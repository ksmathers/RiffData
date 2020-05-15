using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiffData
{
    public class RiffFile
    {
        protected RiffChunk riff;
        protected MappedFile fp;

        public RiffFile(string path, string type)
            : base()
        {
            fp = new MappedFile(path);
            var data = new byte[4];
            riff = new RiffChunk(fp, type);
            //riff.WriteFixedStr("WAVE", 4);
        }
    }
}
