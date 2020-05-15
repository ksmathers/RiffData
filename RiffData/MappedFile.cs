using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace RiffData
{
    public class MappedFile
    {
        string path;
        FileStream fp;
        byte[] memory;
        int pend;

        public MappedFile(string path)
        {
            this.path = path;  
            Extend(1024);
        }

        public void Save()
        {
            fp = new FileStream(path, FileMode.Create);
            fp.Seek(0, SeekOrigin.Begin);
            fp.SetLength(0);
            fp.Write(memory, 0, pend);
            fp.Close();
        }

        public void Extend(int newsize)
        {
            newsize += 1023;
            newsize = newsize - (newsize % 1024);
            byte[] _newmem = new byte[newsize];
            if (memory != null) Array.Copy(memory, 0, _newmem, 0, memory.Length);
            memory = _newmem;
        }

        public int Malloc(int nbytes)
        {
            int p = pend;
            pend += nbytes;
            if (pend > memory.Length) {
                Extend(pend);
            }
            return p;
        }

        public bool Extend(int ptr, int oldsize, int newsize)
        {
            if (ptr+oldsize == pend) {
                Malloc(newsize - oldsize);
                return true;
            }
            return false;
        }

        public void WriteByte(int p, byte b)
        {
            memory[p] = b;
        }

        public byte ReadByte(int p)
        {
            return memory[p];
        }

        public void WriteBytes(int p, byte[] buf, int nbytes)
        {
            Array.Copy(buf, 0, memory, p, nbytes);
        }

        public byte[] ReadBytes(int p, int nbytes)
        {
            var buf = new byte[nbytes];
            Array.Copy(memory, p, buf, 0, nbytes);
            return buf;
        }

        public void WriteInt32(int p, int val)
        {
            var a = (byte)((val >> 0) & 0xff);
            var b = (byte)((val >> 8) & 0xff);
            var c = (byte)((val >> 16) & 0xff);
            var d = (byte)((val >> 24) & 0xff);
            WriteByte(p + 0, a);
            WriteByte(p + 1, b);
            WriteByte(p + 2, c);
            WriteByte(p + 3, d);
        }

        public void WriteInt16(int p, int val)
        {
            var a = (byte)((val >> 0) & 0xff);
            var b = (byte)((val >> 8) & 0xff);
            WriteByte(p + 0, a);
            WriteByte(p + 1, b);
        }

        public Int32 ReadInt32(int p)
        {
            var a = ReadByte(p + 0);
            var b = ReadByte(p + 1);
            var c = ReadByte(p + 2);
            var d = ReadByte(p + 3);
            return (Int32)((d << 24) | (c << 16) | (b << 8) | (a << 0));
        }

        public Int16 ReadInt16(int p)
        {
            var a = ReadByte(p + 0);
            var b = ReadByte(p + 1);
            return (Int16)((b << 8) | (a << 0));
        }
    }

    public abstract class MappedPtr
    {
        protected MappedFile fmap;
        protected int ptr;
        int len;
        public MappedPtr(MappedFile f)
        {
            fmap = f;
        }

        protected void Malloc(int nbytes)
        {
            len = nbytes;
            ptr = fmap.Malloc(nbytes);
        }

        protected bool Extend(int p, int addbytes)
        {
            var ok = fmap.Extend(p, len, len+addbytes);
            if (ok) len += addbytes;
            return ok;
        }

        public virtual string Dump(int indent)
        {
            string margin = new string(' ', indent);
            string result = "[";
            for (int i = 0; i < len; i++) {
                if (i % 20 == 0) {
                    if (i > 0) result += ",";
                    result += "\r\n" + margin;
                } else { 
                    result += ","; 
                }
                var x = fmap.ReadByte(ptr + i);
                result += x.ToString("X2");
            }
            result += " ]";
            return result;
        }

        public int Length { get { return len; } }
    }

    public class Int32Ptr : MappedPtr
    {
        public Int32Ptr(MappedFile f) :
            base(f)
        {
            Malloc(4);
        }

        public Int32 Read()
        {
            return fmap.ReadInt32(ptr);
        }

        public void Write(Int32 val)
        {
            fmap.WriteInt32(ptr, val);
        }

        public override string Dump(int indent)
        {
            string result = Read().ToString();
            return result;
        }
    }

    public class Int16Ptr : MappedPtr
    {
        public Int16Ptr(MappedFile f) :
            base(f)
        {
            
            Malloc(2);
        }

        public Int16 Read()
        {
            return fmap.ReadInt16(ptr);
        }

        public void Write(Int16 val)
        {
            fmap.WriteInt16(ptr, val);
        }
        public override string Dump(int indent = 0)
        {
            string result = Read().ToString();
            return result;
        }
    }

    public class ByteArrayPtr : MappedPtr
    {
        public int nbytes;
        public ByteArrayPtr(MappedFile f, int nbytes)
            : base(f)
        {
            this.nbytes = nbytes;
            Malloc(nbytes);
        }

        public bool Extend(byte[] data)
        {
            var obytes = nbytes;
            var ok = Extend(ptr, data.Length);
            if (ok) {
                fmap.WriteBytes(ptr + obytes, data, data.Length);
                nbytes += data.Length;
            }
            return ok;
        }

        public byte[] Read()
        {
            return fmap.ReadBytes(ptr, nbytes);
        }

        public void Write(byte[] buf)
        {
            fmap.WriteBytes(ptr, buf, nbytes);
        }
    }

    public class FixedStrPtr : ByteArrayPtr
    {
        public FixedStrPtr(MappedFile f, int nbytes)
            : base(f, nbytes) { }

        new public string Read()
        {
            byte[] buf = base.Read();
            return ASCIIEncoding.ASCII.GetString(buf);
        }

        public void Write(string s)
        {
            byte[] buf = ASCIIEncoding.ASCII.GetBytes(s);
            Debug.Assert(buf.Length == nbytes);
            base.Write(buf);
        }

        public override string Dump(int indent = 0)
        {
            string result = "'" + Read() + "'";
            return result;
        }
    }

    public class FourCCPtr : FixedStrPtr
    {
        public FourCCPtr(MappedFile f)
            : base(f, 4) { }
    }
}
