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

    /// <summary>
    /// Basic implementation of memory mapped file semantics.   
    /// </summary>
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

        /// <summary>
        /// Write any dirty buffers and close the file.
        /// 
        /// In this implementation the entire memory buffer is kept in memory
        /// and written at once.
        /// </summary>
        public void Save()
        {
            fp = new FileStream(path, FileMode.Create);
            fp.Seek(0, SeekOrigin.Begin);
            fp.SetLength(0);
            fp.Write(memory, 0, pend);
            fp.Close();
        }

        /// <summary>
        /// Extends the total size of the memory mapped file.   Quantized to 1kB boundaries
        /// when extended, but truncated to the exact file length on close.
        /// </summary>
        /// <param name="newsize"></param>
        public void Extend(int newsize)
        {
            newsize += 1023;
            newsize = newsize - (newsize % 1024);
            byte[] _newmem = new byte[newsize];
            if (memory != null) Array.Copy(memory, 0, _newmem, 0, memory.Length);
            memory = _newmem;
        }

        /// <summary>
        /// Allocates bytes from the file.   Returns the file position as an Int.
        /// </summary>
        /// <param name="nbytes"></param>
        /// <returns></returns>
        public int Malloc(int nbytes)
        {
            int p = pend;
            pend += nbytes;
            if (pend > memory.Length) {
                Extend(pend);
            }
            return p;
        }

        /// <summary>
        /// Extends a previous allocation if it is at the end of the currently used
        /// memory.
        /// </summary>
        /// <param name="ptr">the start position of the allocation</param>
        /// <param name="oldsize">the old allocated size</param>
        /// <param name="newsize">the new size</param>
        /// <returns>true if successful, false if the pointer is not at the end of the buffer</returns>
        public bool Extend(int ptr, int oldsize, int newsize)
        {
            if (ptr+oldsize == pend) {
                Malloc(newsize - oldsize);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Write a byte to memory
        /// </summary>
        /// <param name="p">pointer location</param>
        /// <param name="b">byte value</param>
        public void WriteByte(int p, byte b)
        {
            memory[p] = b;
        }

        /// <summary>
        /// Read a byte from memory
        /// </summary>
        /// <param name="p">pointer location</param>
        /// <returns></returns>
        public byte ReadByte(int p)
        {
            return memory[p];
        }

        /// <summary>
        /// Writes an array of bytes to memory
        /// </summary>
        /// <param name="p">memory pointer</param>
        /// <param name="buf">the bytes to be written</param>
        /// <param name="nbytes">byte count to write</param>
        public void WriteBytes(int p, byte[] buf, int nbytes)
        {
            Array.Copy(buf, 0, memory, p, nbytes);
        }

        /// <summary>
        /// Read an array of bytes from memory
        /// </summary>
        /// <param name="p">memory pointer</param>
        /// <param name="nbytes">bytes to read</param>
        /// <returns>a new byte buffer</returns>
        public byte[] ReadBytes(int p, int nbytes)
        {
            var buf = new byte[nbytes];
            Array.Copy(memory, p, buf, 0, nbytes);
            return buf;
        }

        /// <summary>
        /// Writes a 32-bit integer to memory
        /// </summary>
        /// <param name="p">memory pointer</param>
        /// <param name="val">value</param>
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

        /// <summary>
        /// Writes a 16 bit value to memory
        /// </summary>
        /// <param name="p">memory pointer</param>
        /// <param name="val">value to be truncated and written</param>
        public void WriteInt16(int p, int val)
        {
            var a = (byte)((val >> 0) & 0xff);
            var b = (byte)((val >> 8) & 0xff);
            WriteByte(p + 0, a);
            WriteByte(p + 1, b);
        }

        /// <summary>
        /// Reads a 32 bit integer from memory.   Cast to uint before assigning to a larger
        /// integer type if you don't want sign extended values.
        /// </summary>
        /// <param name="p">memory pointer</param>
        /// <returns>value</returns>
        public Int32 ReadInt32(int p)
        {
            var a = ReadByte(p + 0);
            var b = ReadByte(p + 1);
            var c = ReadByte(p + 2);
            var d = ReadByte(p + 3);
            return (Int32)((d << 24) | (c << 16) | (b << 8) | (a << 0));
        }

        /// <summary>
        /// Reads a 16 bit integer from memory.   Cast ushort before assigning to a larger
        /// integer type if you don't want sign extended values.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public Int16 ReadInt16(int p)
        {
            var a = ReadByte(p + 0);
            var b = ReadByte(p + 1);
            return (Int16)((b << 8) | (a << 0));
        }
    }

    /// <summary>
    /// Base class for all typed pointers to MappedFile memory locations
    /// </summary>
    public abstract class MappedPtr
    {
        protected MappedFile fmap;
        protected int ptr;
        int len;

        /// <summary>
        /// The constructor only binds the pointer to a MappedFile.  Call Malloc() or 
        /// Extend() before using.
        /// </summary>
        /// <param name="f"></param>
        public MappedPtr(MappedFile f)
        {
            fmap = f;
        }

        /// <summary>
        /// Allocates bytes at the end of the MappedFile.  
        /// </summary>
        /// <param name="nbytes"></param>
        protected void Malloc(int nbytes)
        {
            len = nbytes;
            ptr = fmap.Malloc(nbytes);
        }

        /// <summary>
        /// Extends the number of bytes previously allocated with Malloc
        /// </summary>
        /// <param name="p"></param>
        /// <param name="addbytes"></param>
        /// <returns></returns>
        protected bool Extend(int p, int addbytes)
        {
            var ok = fmap.Extend(p, len, len+addbytes);
            if (ok) len += addbytes;
            return ok;
        }

        /// <summary>
        /// Stringify the contents of the pointer.  
        /// </summary>
        /// <param name="indent"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the Malloc() length of the pointer
        /// </summary>
        public int Length { get { return len; } }
    }

    /// <summary>
    /// A MappedFile pointer that accesses 32-bit integers
    /// </summary>
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

    /// <summary>
    /// A MappedFile pointer that accesses 16-bit integers
    /// </summary>
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

    /// <summary>
    /// A MappedFile pointer that accesses byte arrays of specified length
    /// </summary>
    public class ByteArrayPtr : MappedPtr
    {
        public int nbytes;

        /// <summary>
        /// Constructs an accessor for byte arrays of specified length
        /// </summary>
        /// <param name="f">MappedFile reference</param>
        /// <param name="nbytes">the length of data to allocate</param>
        public ByteArrayPtr(MappedFile f, int nbytes)
            : base(f)
        {
            this.nbytes = nbytes;
            Malloc(nbytes);
        }

        /// <summary>
        /// Extends the allocation for a byte array if it is at the end of the MappedFile, 
        /// and copies the provided data buffer into that location
        /// </summary>
        /// <param name="data">new data to be added to the ByteArray</param>
        /// <returns>true on success, false on failure</returns>
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

        /// <summary>
        /// Reads the full content of the ByteArray (as a local copy)
        /// </summary>
        /// <returns></returns>
        public byte[] Read()
        {
            return fmap.ReadBytes(ptr, nbytes);
        }

        /// <summary>
        /// Writes the full content of the ByteArray
        /// </summary>
        /// <param name="buf"></param>
        public void Write(byte[] buf)
        {
            fmap.WriteBytes(ptr, buf, nbytes);
        }
    }

    /// <summary>
    /// A MappedFile pointer to a fixed length string
    /// </summary>
    public class FixedStrPtr : ByteArrayPtr
    {
        /// <summary>
        /// Creates a pointer to a string of specified length
        /// </summary>
        /// <param name="f">the MappedFile being referenced</param>
        /// <param name="nbytes">number of bytes to allocate to the string</param>
        public FixedStrPtr(MappedFile f, int nbytes)
            : base(f, nbytes) { }

        /// <summary>
        /// Reads a fixed length decoded ASCII string
        /// </summary>
        /// <returns>an ASCII string</returns>
        new public string Read()
        {
            byte[] buf = base.Read();
            return ASCIIEncoding.ASCII.GetString(buf);
        }

        /// <summary>
        /// Writes a fixed length ASCII string.   The string provided must match the expected
        /// string length and should contain valid ASCII codepoints.  Characters greater than
        /// 0x7f will be encoded as an ASCII question mark ('?').
        /// </summary>
        /// <param name="s">an ASCII string</param>
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

    /// <summary>
    /// A RIFF four character chunk ID code.  Originally this was written as a long character 
    /// type containing 4-bytes, each containing one letter of the long character type.  Here the 
    /// value is instead represented as a 4-character string at extraction, and is converted 
    /// to a FourCC when written to the MappedFile memory.
    /// </summary>
    public class FourCCPtr : FixedStrPtr
    {
        public FourCCPtr(MappedFile f)
            : base(f, 4) { }
    }
}
