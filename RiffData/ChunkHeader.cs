using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace RiffData
{
    public abstract class ChunkHeader
    {
        public const int HEADER_LEN = 8;

        public FixedStrPtr id { get; private set; }
        public Int32Ptr blen { get; private set; }

        public List<MappedPtr> body { get; private set; }
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
        /// <param name="ptr"></param>
        public T AddPtr<T>(MappedFile f) where T : MappedPtr
        {
            T ptr = (T)Activator.CreateInstance(typeof(T), (MappedFile)f);
            return AddPtr<T>(ptr);
        }

        public T AddPtr<T>(T p) where T : MappedPtr
        {
            body.Add(p);
            PlusLen(p.Length);
            return (T)p;
        }

        public void PlusLen(int nbytes)
        {
            blen.Write(Length + nbytes);
        }

        public string Id {
            get { return id.Read(); }
        }

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
