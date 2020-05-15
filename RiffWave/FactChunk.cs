using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RiffData;

namespace RiffWave
{
    /// <summary>
    /// Indicates the total recording duration of the WAV file.   Required for WAV formats other than PCM.
    /// </summary>
    public class FactChunk : ChunkHeader
    {
        Int32Ptr dwSampleLength;

        public FactChunk(MappedFile f, int sampleLength) :
            base(f, "fact")
        {
            dwSampleLength = AddPtr<Int32Ptr>(f);

            SampleLength = sampleLength;
        }

        /// <summary>
        /// The total run length of the WAV recording counted in samples.  Multiply by FmtChunk.SamplesPerSec to get run length in seconds.
        /// </summary>
        public int SampleLength {
            get { return dwSampleLength.Read(); }
            set { dwSampleLength.Write(value); }
        }
    }
}
