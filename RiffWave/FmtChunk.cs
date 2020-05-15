using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using RiffData;

namespace RiffWave
{
    // http://www-mmsp.ece.mcgill.ca/Documents/AudioFormats/WAVE/WAVE.html
    public class FmtChunk : ChunkHeader
    {
        public enum WaveFormat
        {
            PCM = 0x1,
            IEEE_FLOAT = 0x2,
            ALAW = 0x6,             // 8-bit ITU-T G.711 A-law
            MULAW = 0x7,            // 8-bit ITU-T G.711 mu-law
            EXTENSIBLE = 0xfffe
        }

        Int16Ptr wFormatTag;
        Int16Ptr nChannels;
        Int32Ptr nSamplesPerSec;
        Int32Ptr nAvgBytesPerSec;
        Int16Ptr nBlockAlign;
        Int16Ptr wBitsPerSample;
        Int16Ptr cbSize;
        Int16Ptr wValidBitsPerSample;
        Int32Ptr dwChannelMask;
        ByteArrayPtr gidSubFormat;

        public FmtChunk(MappedFile f, WaveFormat _audioFormat)
            : base(f, "fmt ")
        {
            // Structure
            wFormatTag = AddPtr<Int16Ptr>(f);
            nChannels = AddPtr<Int16Ptr>(f);
            nSamplesPerSec = AddPtr<Int32Ptr>(f);
            nAvgBytesPerSec = AddPtr<Int32Ptr>(f);
            nBlockAlign = AddPtr<Int16Ptr>(f);
            wBitsPerSample = AddPtr<Int16Ptr>(f);
            if (_audioFormat != WaveFormat.PCM) {
                cbSize = AddPtr<Int16Ptr>(f);
                if (_audioFormat == WaveFormat.EXTENSIBLE) {
                    wValidBitsPerSample = AddPtr<Int16Ptr>(f);
                    dwChannelMask = AddPtr<Int32Ptr>(f);
                    gidSubFormat = AddPtr<ByteArrayPtr>(new ByteArrayPtr(f, 16));
                    cbSize = new Int16Ptr(f);
                }
            }

            // Initialization
            FormatTag = _audioFormat;
        }

        public WaveFormat FormatTag {
            get { return (WaveFormat)(ushort)wFormatTag.Read(); }
            set { wFormatTag.Write((short)value); }
        }

        public int Channels {
            get { return nChannels.Read(); }
            set { nChannels.Write((short)value); }
        }

        public int SamplesPerSec {
            get { return nSamplesPerSec.Read(); }
            set { nSamplesPerSec.Write(value); }
        }

        public int AvgBytesPerSec {
            get { return nAvgBytesPerSec.Read(); }
            set { nAvgBytesPerSec.Write(value); }
        }

        public int BlockAlign {
            get { return nBlockAlign.Read(); }
            set { nBlockAlign.Write((short)value); }
        }

        public int BitsPerSample {
            get { return (ushort)wBitsPerSample.Read(); }
            set { wBitsPerSample.Write((short)value); }
        }

        public int ExtensionSize {
            get { return cbSize.Read(); }
            set { cbSize.Write((short)value); }
        }

        public int ValidBitsPerSample {
            get { return (ushort)wValidBitsPerSample.Read(); }
            set { wValidBitsPerSample.Write((short)value); }
        }

        public uint ChannelMask {
            get { return (uint)dwChannelMask.Read(); }
            set { dwChannelMask.Write((int)value); }
        }

        public Guid SubFormat {
            get { return new Guid(gidSubFormat.Read()); }
            set { gidSubFormat.Write(value.ToByteArray()); }
        }
    }
}
