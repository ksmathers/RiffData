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

        public DataChunk(MappedFile f, int nbytes, RiffChunk riff)
            : base(f, "data")
        {
            this.riff = riff;
            bSamples = AddPtr<ByteArrayPtr>(new ByteArrayPtr(f, nbytes));
        }

        public byte[] Samples {
            get { return bSamples.Read(); }
            set { bSamples.Write(value); }
        }

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
