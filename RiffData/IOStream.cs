using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiffData
{
    public class IOStream
    {
        public enum IODirection { In, Out };
        public IODirection iodir;
        Stream iofile;


        public IOStream(Stream s, IODirection iodir)
        {
            this.iodir = iodir;
            this.iofile = s;
            if (iodir == IODirection.In && !s.CanRead) throw new ArgumentException("stream must be readable");
            if (iodir == IODirection.Out && !s.CanWrite) throw new ArgumentException("stream must be writeable");
        }

        public void XdrStringFixed(ref string fstr, int flen)
        {
            var buf = new byte[flen];
            if (iodir == IODirection.Out)
            {
                Array.Copy(ASCIIEncoding.ASCII.GetBytes(fstr), buf, buf.Length);
            }
            XdrByteArray(ref buf);
            if (iodir == IODirection.In)
            {
                fstr = ASCIIEncoding.ASCII.GetString(buf);
            }
        }

        public void XdrByteArray(ref byte[] buf)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                XdrByte(ref buf[i]);
            }
        }

        public void XdrInt32(ref Int32 p)
        {
            if (iodir == IODirection.In)
            {
                p = ReadInt32();
            } else
            {
                WriteInt32(p);
            }
        }

        public void XdrInt16(ref Int16 p)
        {
            if (iodir == IODirection.In)
            {
                p = ReadInt16();
            }
            else
            {
                WriteInt16(p);
            }
        }

        public void XdrByte(ref byte p)
        {
            if (iodir == IODirection.In)
            {
                p = ReadByte();
            }
            else
            {
                WriteByte(p);
            }
        }

        private void WriteInt32(int val)
        {
            var a = (byte)((val >> 0) & 0xff);
            var b = (byte)((val >> 8) & 0xff);
            var c = (byte)((val >> 16) & 0xff);
            var d = (byte)((val >> 24) & 0xff);
            iofile.WriteByte(a);
            iofile.WriteByte(b);
            iofile.WriteByte(c);
            iofile.WriteByte(d);
        }

        private void WriteInt16(int val)
        {
            var a = (byte)((val >> 0) & 0xff);
            var b = (byte)((val >> 8) & 0xff);
            iofile.WriteByte(a);
            iofile.WriteByte(b);
        }

        private void WriteByte(int val)
        {
            var a = (byte)((val >> 0) & 0xff);
            iofile.WriteByte(a);
        }

        private Int32 ReadInt32()
        {
            var a = iofile.ReadByte();
            var b = iofile.ReadByte();
            var c = iofile.ReadByte();
            var d = iofile.ReadByte();
            return (d << 24) | (c << 16) | (b << 8) | (a << 0);
        }

        private Int16 ReadInt16()
        {
            var a = iofile.ReadByte();
            var b = iofile.ReadByte();
            return (Int16)((b << 8) | (a << 0));
        }

        private byte ReadByte()
        {
            return (byte)iofile.ReadByte();
        }
    }
}
