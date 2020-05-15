using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace RiffData
{
    /// <summary>
    /// The base class for all Chunk types.  RiffChunk is one example of a specialization.
    /// Other chunk types are specific to the type of data they store, samples for which can 
    /// be found in RiffWave, including DataChunk, FactChunk, and FmtChunk.
    /// </summary>
    public abstract class ChunkHeader
    {
        public const int HEADER_LEN = 8;

        /// <summary>
        /// The first four bytes of any chunk is the chunk ID, expressed as a FourCC string.
        /// </summary>
        public FourCCPtr id { get; private set; }
        /// <summary>
        /// The second set of four bytes give the total length of the data (not including the
        /// first 8 bytes of ID and BLEN fields)
        /// </summary>
        public Int32Ptr blen { get; private set; }

        /// <summary>
        /// Keeps track of allocated MappedPtr data AddPtr()'d to this chunk.
        /// </summary>
        public List<MappedPtr> body { get; private set; }

        /// <summary>
        /// Keeps track of other chunks that have been created and are contained by this chunk.
        /// </summary>
        List<ChunkHeader> chunks;

        public ChunkHeader(MappedFile f, string _id)
        {
            id = new FourCCPtr(f);
            id.Write(_id);
            blen = new Int32Ptr(f);
            blen.Write(0);
            body = new List<MappedPtr>();
            chunks = new List<ChunkHeader>();
        }

        /// <summary>
        /// Adds an allocated subchunk to a chunk
        /// </summary>
        /// <param name="ck"></param>
        public void AddChunk(ChunkHeader ck)
        {
            chunks.Add(ck); 
            PlusLen(HEADER_LEN + ck.Length);
        }

        /// <summary>
        /// Adds allocated data to a chunk
        /// </summary>
        /// <typeparam name="T">A typed subclass of MappedPtr</typeparam>
        /// <param name="f">The mapped file in which to allocate the pointer</param>
        /// <returns>The newly allocated pointer</returns>
        public T AddPtr<T>(MappedFile f) where T : MappedPtr
        {
            T ptr = (T)Activator.CreateInstance(typeof(T), (MappedFile)f);
            return AddPtr<T>(ptr);
        }

        /// <summary>
        /// Adds a pre-allocated pointer to the body index for this chunk
        /// </summary>
        /// <typeparam name="T">The specific subclass of MappedPtr to return</typeparam>
        /// <param name="p">the pointer to index</param>
        /// <returns>the same pointer given in</returns>
        public T AddPtr<T>(T p) where T : MappedPtr
        {
            body.Add(p);
            PlusLen(p.Length);
            return (T)p;
        }

        /// <summary>
        /// Adds bytes to the chunk length header element
        /// </summary>
        /// <param name="nbytes"></param>
        public void PlusLen(int nbytes)
        {
            blen.Write(Length + nbytes);
        }

        /// <summary>
        /// Accessor for reading the chunk Id FourCC string
        /// </summary>
        public string Id {
            get { return id.Read(); }
        }

        /// <summary>
        /// Accessor for reading the length of the chunk
        /// </summary>
        public int Length {
            get { return blen.Read(); }
        }

        public string Dump(int indent = 0)
        {
            string result;
            string margin = new string(' ', indent);
            result = margin + "{'" + Id + "', " + blen.Read();
            foreach (var p in body) {
                //result += ", " + p.GetType().Name;
                result += ", " + p.Dump(indent + 4);
            }
            foreach (var ck in chunks) {
                result += ",\r\n";
                result += ck.Dump(indent + 4);
            }
            result += "}";
            return result;
        }

    }
}
