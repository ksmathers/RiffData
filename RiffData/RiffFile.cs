using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiffData
{
    /// <summary>
    /// Binds a top level RiffChunk to a MappedFile where the chunk data will be stored.
    /// </summary>
    public class RiffFile
    {
        protected RiffChunk riff;
        protected MappedFile fp;

        /// <summary>
        /// Binds a RiffChunk to a MappedFile.   This is the top level object to
        /// use for writing RIFF data.
        /// </summary>
        /// <param name="path">The file to write to</param>
        /// <param name="type">The type field ('WAVE' for audio data)</param>
        public RiffFile(string path, string type)
            : base()
        {
            fp = new MappedFile(path);
            var data = new byte[4];
            riff = new RiffChunk(fp, type);
        }
    }
}
