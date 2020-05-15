using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RiffData;

namespace RiffWave
{
    public class DataChunk : ChunkHeader
    {
        RiffChunk riff;
        ByteArrayPtr bSamples;

        /// <summary>
        /// Allocates a new Data chunk from the Riff file.  For a WAV file this can be
        /// the last chunk in the file in which case the chunk can be extended with Extend()
        /// to allow more data to be written
        /// </summary>
        /// <param name="f">MappedFile reference</param>
        /// <param name="nbytes">data bytes to preallocate, or 0 if all data will be added using AddSamples()</param>
        /// <param name="riff">top level RIFF chunk</param>
        public DataChunk(MappedFile f, int nbytes, RiffChunk riff)
            : base(f, "data")
        {
            this.riff = riff;
            bSamples = AddPtr<ByteArrayPtr>(new ByteArrayPtr(f, nbytes));
        }

        /// <summary>
        /// The data samples buffer.   Returns a local copy, assign changes back to Samples
        /// to update.
        /// </summary>
        public byte[] Samples {
            get { return bSamples.Read(); }
            set { bSamples.Write(value); }
        }

        /// <summary>
        /// Appends new samples to the existing buffer if this DataChunk it the last one in the WAV 
        /// file.
        /// </summary>
        /// <param name="arr">data to be added</param>
        public void AddSamples(byte[] arr)
        {
            bool ok = bSamples.Extend(arr);
            if (ok) {
                // Update byte totals for the DataChunk and the overall RiffChunk
                PlusLen(arr.Length);
                riff.PlusLen(arr.Length);
            } else {
                throw new NotImplementedException("Unable to extend chunk, not at end");
            }
        }
    }
}
