using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RiffData
{
    /// <summary>
    /// A mostly transparent LRU page loader for treating a file as a memory mapped block buffer.   This implementation
    /// doesn't require any operating system support for memory mapped files, it just simulates paging by keeping track
    /// of accesses and using random File I/O to write out dirty blocks.
    /// </summary>
    public class BlockMem
    {
        public const int PAGESIZE = 1024;
        private readonly int HIGHWATER = 128;
        private readonly int LOWWATER = 96;
        int usecount = 0;

        // Access age counter
        int UseCount {
            get { return usecount++; }
        }

        // Total Length in bytes of the allocated pages
        public int Length {
            get { return blocks.Count * PAGESIZE; }
        }

        /// <summary>
        /// Represents a memory region that is backed by filesystem storage
        /// </summary>
        private class Block
        {
            BlockMem blk;
            FileStream io;
            int address0;
            int last_used;
            byte[] mem;
            bool loaded;
            bool dirty;

            public event EventHandler Loaded;
            public event EventHandler Unloaded;

            public Block(BlockMem blk, int address, FileStream io)
            {
                this.blk = blk;
                this.io = io;
                this.address0 = address;
                mem = null;
                loaded = false;
            }

            /// <summary>
            /// Free up the memory in this block, writing out to disk if dirty
            /// </summary>
            public void Unload()
            {
                if (!loaded) return;
                if (dirty) {
                    if (address0 > io.Length) io.SetLength(address0);
                    if (address0 != io.Position) io.Seek(address0, SeekOrigin.Begin);
                    io.Write(mem, 0, PAGESIZE);
                }
                mem = null;
                loaded = false;
                last_used = 0;
                Unloaded?.Invoke(this, null);
            }

            /// <summary>
            /// Restore this block from disk to memory.   If the address is beyond the end of the
            /// current file length then the memory is just cleared.
            /// </summary>
            public void Load()
            {
                mem = new byte[PAGESIZE];
                if (address0 < io.Length) {
                    io.Seek(address0, SeekOrigin.Begin);
                    io.Read(mem, 0, PAGESIZE);
                }
                Loaded?.Invoke(this, null);
                loaded = true;
                last_used = blk.UseCount;
            }

            /// <summary>
            /// The last time the block memory was read or written
            /// </summary>
            public int LastUse {
                get { return last_used; }
            }

            /// <summary>
            /// Reads a byte of memory
            /// </summary>
            /// <param name="addr">memory address</param>
            /// <returns></returns>
            public byte ReadByte(int addr)
            {
                if (!loaded) Load();
                last_used = blk.UseCount;
                return mem[addr - address0];
            }

            /// <summary>
            /// The base address of this memory block
            /// </summary>
            public int BaseAddress {
                get { return address0; }
            }

            /// <summary>
            /// Writes a byte of memory
            /// </summary>
            /// <param name="addr">memory address</param>
            /// <param name="val">byte value</param>
            public void WriteByte(int addr, byte val)
            {
                if (!loaded) Load();
                last_used = blk.UseCount;
                mem[addr - address0] = val;
                dirty = true;
            }
        }

        private FileStream io;
        List<Block> blocks;
        int loadedBlockCount = 0;

        /// <summary>
        /// Creates a file backed paged memory structure
        /// </summary>
        /// <param name="io"></param>
        public BlockMem(FileStream io)
        {
            this.io = io;
            blocks = new List<Block>();
        }


        private void AddBlock(int n)
        {
            var block = new Block(this, n * PAGESIZE, io);
            block.Loaded += Block_Loaded;
            block.Unloaded += Block_Unloaded;
            blocks.Add(block);
        }

        private void Block_Unloaded(object sender, EventArgs e)
        {
            loadedBlockCount--;
        }

        /// <summary>
        /// Evicts one block that hasn't been used in a while
        /// </summary>
        private void EvictOldestBlock()
        {
            Block oldest = null;
            foreach (var b in blocks) {
                if (b.LastUse>0) {
                    if (oldest == null || oldest.LastUse > b.LastUse) {
                        oldest = b;
                    }
                }
            }
            if (oldest != null) {
                Console.WriteLine($"Evicting {oldest.BaseAddress}");
                oldest.Unload();
                loadedBlockCount--;
            }
        }

        /// <summary>
        /// Writes out all of the dirty blocks
        /// </summary>
        /// <param name="length"></param>
        public void Save(int length)
        {
            foreach (var b in blocks) {
                b.Unload();
            }
            io.SetLength(length);
            io.Close();
        }

        private void Block_Loaded(object sender, EventArgs e)
        {
            // Adjust loaded block count when a new block is loaded, and evict blocks
            // if there are too many in memory at once
            loadedBlockCount++;
            if (loadedBlockCount >= HIGHWATER) {
                while (loadedBlockCount > LOWWATER) EvictOldestBlock();
            }
        }

        /// <summary>
        /// Adds blocks to the BlockMem structure
        /// </summary>
        /// <param name="n">new block total</param>
        void SetBlockCount(int n)
        {
            for (int i = blocks.Count; i < n; i++) {
                AddBlock(i);
            }
        }

        /// <summary>
        /// Increases the total addressable memory size by allocating new blocks
        /// </summary>
        /// <param name="newsize">in bytes</param>
        public void Extend(int newsize)
        {
            int blocks = (newsize + PAGESIZE - 1) / PAGESIZE;
            SetBlockCount(blocks);
        }

        /// <summary>
        /// Reads a byte of memory
        /// </summary>
        /// <param name="addr">memory address</param>
        /// <returns></returns>
        public byte ReadByte(int addr)
        {
            int iblock = addr / PAGESIZE;
            var block = blocks[iblock];
            return block.ReadByte(addr);
        }

        /// <summary>
        /// Writes a byte of memory
        /// </summary>
        /// <param name="addr">memory address</param>
        /// <param name="value">byte</param>
        public void WriteByte(int addr, byte value)
        {
            int idx = addr / PAGESIZE;
            var block = blocks[idx];
            block.WriteByte(addr, value);
        }

        /// <summary>
        /// Reads a memory buffer from memory
        /// </summary>
        /// <param name="addr">memory address</param>
        /// <param name="len">length in bytes</param>
        /// <returns>a new byte[] array</returns>
        public byte[] ReadBytes(int addr, int len)
        {
            byte[] buf = new byte[len];
            for (int i = 0; i < len; i++) buf[i] = ReadByte(addr + i);
            return buf;
        }

        /// <summary>
        /// Writes a memory buffer into memory
        /// </summary>
        /// <param name="addr">memory address</param>
        /// <param name="buf">data to write</param>
        /// <param name="nbytes">number of bytes to store</param>
        public void WriteBytes(int addr, byte[] buf, int nbytes)
        {
            for (int i = 0; i < nbytes; i++) WriteByte(addr + i, buf[i]);
        }
    }
}
