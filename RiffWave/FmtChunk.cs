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
            PCM = 0x1,              // 16-bit or 32-bit PCM
            IEEE_FLOAT = 0x2,       // 32-bit or 64-bit IEEE float
            ALAW = 0x6,             // 8-bit ITU-T G.711 A-law
            MULAW = 0x7,            // 8-bit ITU-T G.711 mu-law
            EXTENSIBLE = 0xfffe
        }

        // Data fields
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

        /// <summary>
        /// Any of several compressed or uncompressed basic audio formats, or use
        /// WaveFormat.EXTENSIBLE for plugin compression schemes identified on Windows
        /// by UUID
        /// </summary>
        public WaveFormat FormatTag {
            get { return (WaveFormat)(ushort)wFormatTag.Read(); }
            set { wFormatTag.Write((short)value); }
        }

        /// <summary>
        /// Number of channels represented in the data.  
        /// </summary>
        public int Channels {
            get { return nChannels.Read(); }
            set { nChannels.Write((short)value); }
        }

        /// <summary>
        /// Samples per second.  Common rates include: 44100 (AudioCD), 48000 (DAT), 
        /// 8000 (G.711)
        /// </summary>
        public int SamplesPerSec {
            get { return nSamplesPerSec.Read(); }
            set { nSamplesPerSec.Write(value); }
        }

        /// <summary>
        /// Used to determine the total runtime of the recording
        /// AvgBytesPerSec = (SamplesPerSec * BitsPerSample/8 * Channels)
        /// </summary>
        public int AvgBytesPerSec {
            get { return nAvgBytesPerSec.Read(); }
            set { nAvgBytesPerSec.Write(value); }
        }

        /// <summary>
        /// BlockAlign is the number of bytes in a single sample row across all channels
        /// BlockAlign = (BitsPerSample/8 * Channels)
        /// </summary>
        public int BlockAlign {
            get { return nBlockAlign.Read(); }
            set { nBlockAlign.Write((short)value); }
        }

        /// <summary>
        /// Typically 8 (8-bit formats), 16 (16-bit formats), 32 (32-bit formats), or 64 (64-bit float)
        /// </summary>
        public int BitsPerSample {
            get { return (ushort)wBitsPerSample.Read(); }
            set { wBitsPerSample.Write((short)value); }
        }

        /// <summary>
        /// Not used for PCM format.
        /// Otherwise this is the size of the extended format information. Usually 0 for
        /// formats other than WaveFormat.EXTENSIBLE, although the ChannelMask may be used
        /// to map channels to specific surround sound speakers.
        /// </summary>
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
