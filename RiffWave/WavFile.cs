using RiffData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiffWave
{
    /// <summary>
    /// Sample implementation of the WAV file format using the RiffData library
    /// </summary>
    public class WavFile : RiffFile
    {
        FmtChunk fmt;
        FactChunk fact;
        DataChunk data;

        public WavFile(string fname, int nchan=1, int freq=8000)
            : base(fname, "WAVE")
        {
            fmt = new FmtChunk(fp, FmtChunk.WaveFormat.MULAW);     
            fmt.Channels = nchan;
            fmt.SamplesPerSec = freq;
            fmt.AvgBytesPerSec = freq;
            fmt.BlockAlign = 1;         // 1 byte per sample for 8-bit MULAW
            fmt.BitsPerSample = 8;      // 8 bits per sample 
            fmt.ExtensionSize = 0;      // no extended data
            riff.AddChunk(fmt);

            fact = new FactChunk(fp, 0);
            riff.AddChunk(fact);

            data = new DataChunk(fp, 0, riff);
            riff.AddChunk(data);
        }

        public void WriteAudio(float[] audio)
        {
            data.AddSamples(UlawCodec.Encode(audio));
        }

        //public void AudioData(float[] data)
        //{
        //    var ck = new DataChunk(fp, data.Length);
        //    ck.Samples = UlawCodec.Encode(data);
        //    riff.AddChunk(ck);
        //    fact.SampleLength += data.Length;
        //}

        public void Save()
        {
            fp.Save();
        }

        public string Dump()
        {
            return riff.Dump();
        }
    }
}
