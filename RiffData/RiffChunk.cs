using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiffData
{
    public class RiffChunk : ChunkHeader
    {
        public FixedStrPtr format { get; private set; }
        
        public RiffChunk(MappedFile f, string format)
            : base(f, "RIFF")
        {
            this.format = AddPtr<FixedStrPtr>(new FixedStrPtr(f, 4));

            Format = format;
        }

        public string Format {
            get { return format.Read(); }
            set { format.Write(value); }
        }

    }
}
