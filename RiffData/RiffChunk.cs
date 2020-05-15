using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiffData
{
    /// <summary>
    /// The top level chunk for all RIFF files:
    /// 
    ///     FourCC("RIFF")
    ///     Int32(content-length)
    ///     FourCC(format)
    ///     
    /// For WAV files the format FourCC is "WAVE"
    /// </summary>
    public class RiffChunk : ChunkHeader
    {
        public FourCCPtr format { get; private set; }
        
        public RiffChunk(MappedFile f, string format)
            : base(f, "RIFF")
        {
            this.format = AddPtr<FourCCPtr>(f);

            Format = format;
        }

        /// <summary>
        /// Accessor for the Format FourCC field
        /// </summary>
        public string Format {
            get { return format.Read(); }
            set { format.Write(value); }
        }

    }
}
