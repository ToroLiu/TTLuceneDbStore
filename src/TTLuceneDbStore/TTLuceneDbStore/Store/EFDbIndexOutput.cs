using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTLuceneDbStore.Models;
using TTLuceneDbStore.Helper;
using System.Diagnostics.Contracts;

namespace TTLuceneDbStore.Store
{
    public class EFDbIndexOutput : IndexOutput
    {
        private DbFileContext _db = null;
        private DbDirectory _dir = null;
        private DbFileInfo _file = null;
        private DbFileBlock _file_block = null;

        private long _buffer_start = 0;
        private long _buffer_position = 0;
        private long _buffer_length = 0;
        private long _block_index = -1;

        private void EnsureOpen() 
        {
            Debug.Assert(_db != null && _file != null);
            if (_db == null || _file == null)
                throw new ArgumentNullException("The file SHOULD BE opend.");
        }

        public EFDbIndexOutput(DbFileContext db, DbDirectory dir)
        {
            Contract.Requires(db != null && dir != null);
            _db = db;
            _dir = dir;
        }

        private const long kDefaultBufferSize = 4096;

        public void OpenOrCreate(string fileName, long buffer_size = kDefaultBufferSize)
        {
            var file = _dir.FileSet.SingleOrDefault(obj => obj.FileName.Equals(fileName));
            if (file == null)
            {
                file = new DbFileInfo();
                file.FileName = fileName;
                file.DirectoryId = _dir.Id;
                file.BufferSize = buffer_size;

                _dir.FileSet.Add(file);
                _db.FileSet.Add(file);
                _db.SaveChanges();    

            }
            
            _file = file;
            ResetProperties();
        }

        public void Create(string fileName, long buffer_size = kDefaultBufferSize)
        {
            // Create a new file
            var file = _dir.FileSet.SingleOrDefault(obj => obj.FileName.Equals(fileName));
            if (file != null)
            {
                TruncateBlocks(_db, file.Id);
                _file.Length = 0;
                _file = file;
            }
            else
            {
                file = new DbFileInfo();
                file.FileName = fileName;
                file.DirectoryId = _dir.Id;
                file.BufferSize = buffer_size;

                _dir.FileSet.Add(file);
                _db.FileSet.Add(file);
                _db.SaveChanges();

                _file = file;
            }
            
            ResetProperties();
        }

        public bool Open(string fileName)
        {
            // open an existed file.
            _file = _dir.FileSet.SingleOrDefault(obj => obj.FileName.Equals(fileName));
            return (_file != null);
        }

        private bool _isDisposed = false;
        protected override void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                Flush();

                _db = null;
                _dir = null;
                _file = null;
                _file_block = null;
            }
            _isDisposed = true;
        }

        #region buffer control functions
        private void SwitchBlock() 
        {
            // Keep oldBlock 
            DbFileBlock oldBlock = null;
            if (_file_block != null)
            {
                _file_block.ComputeSHA();
                oldBlock = _file_block;
            }

            long maxBlock = _file.BlockSet.Count();
            if (_block_index == maxBlock)
            {
                // Add a new block.
                DbFileBlock block = new DbFileBlock(_file.BufferSize);
                block.Order = _block_index;
                _file.BlockSet.Add(block);
                _file_block = block;
                _db.SaveChanges();
            }
            else if (_block_index >= 0 && _block_index < maxBlock)
            {
                // Switch to existed block.
                _file_block = _file.BlockSet.SingleOrDefault(obj => obj.Order == _block_index);
                Debug.Assert(_file_block != null);
            }
            else
            {
                //
                throw new System.IO.IOException("Failed to write due to invalid file block index.");
            }
            
            // Reset buffer positions, buffer length, ...
            _buffer_start = _block_index * _file.BufferSize;
            _buffer_position = 0;
            _buffer_length = _file_block.Length;

            if (_file_block != oldBlock && oldBlock != null)
            {
                // Let GC have chance to free the memory space of oldBlock.
                //_db.Detach(oldBlock);
                oldBlock = null;
            }
        }
        #endregion
        
        public override long FilePointer
        {
            get {
                if (_block_index < 0)
                    return 0;

                long curPos = _buffer_start + _buffer_position;
                return curPos; 
            }
        }
        public override long Length
        {
            get { return _file.Length; }
        }
        
        public override void Flush()
        {
            SetFileLength();

            // write all buffered data to db.
            _file.ModifiedTimeUTC = DateTime.UtcNow;
            _file_block.ComputeSHA();
            _file.ComputeSHA();
            
            _db.SaveChanges();
        }

        public virtual void SetFileLength() 
        {
            long curPos = _buffer_start + _buffer_position;
            if (curPos > _file.Length)
            {
                _file.Length = curPos;
            }
        }

        public override void Seek(long pos)
        {
            SetFileLength();
            if ( pos < _buffer_start || (_buffer_start + _buffer_length) < pos)
            {
                // decide current block index.
                _block_index = pos / _file.BufferSize;
                SwitchBlock();
            }

            // decide current buffer position.
            _buffer_position = pos % _file.BufferSize;
        }

        public override void WriteByte(byte b)
        {
            if (_buffer_position == _buffer_length)
            {
                _block_index++;
                SwitchBlock();
            }

            _file_block.RawData[_buffer_position] = b;
            _buffer_position++;
        }
        public override void WriteBytes(byte[] b, int offset, int length)
        {
            long curLen = length;
            long curOffset = offset;

            while (curLen > 0)
            {
                if (_buffer_position == _buffer_length)
                {
                    _block_index++;
                    SwitchBlock();
                }

                long remainInBuffer = _file_block.Length - _buffer_position;
                long bytesToCopy = Math.Min(curLen, remainInBuffer);
                Array.Copy(b, curOffset, _file_block.RawData, _buffer_position, bytesToCopy);

                // shift position
                curOffset += bytesToCopy;
                curLen -= bytesToCopy;
                _buffer_position += bytesToCopy;
            }
        }

        private void ResetProperties() {
            // Reset properties.
            _buffer_position = 0;
            _buffer_start = 0;
            _buffer_length = 0;
            _block_index = -1;
            _file_block = null;
        }

        // Remove all block for this file.
        internal static void TruncateBlocks(DbFileContext db, long fileId) {
            // Detach blocks in memory.
            var memBlocks = db.FileBlockSet.Local.Where(o => o.FileId == fileId).ToList();
            memBlocks.ForEach(obj => db.Detach(obj));

            // Remove all blocks.            
            string colName = DbHelper.ColumnName<DbFileBlock>(o => o.FileId);
            string cond = string.Format("where {0} = {1}", colName, fileId);
            db.TTDelete(typeof(DbFileBlock), cond);
        }

        internal static void TruncateBlocks(DbFileContext db, long fileId, long minBlockOrder)
        {
            // Detach blocks in memory.
            var memBlocks = db.FileBlockSet.Local.Where(o => o.FileId == fileId && o.Order > minBlockOrder).ToList();
            memBlocks.ForEach(obj => db.Detach(obj));

            // Remove blocks.
            string colName = DbHelper.ColumnName<DbFileBlock>(o => o.FileId);
            string colOrder = DbHelper.ColumnName<DbFileBlock>(o => o.Order);
            string cond = string.Format("where {0} = {1} and {2} > {3}", colName, fileId, colOrder, minBlockOrder);
            db.TTDelete(typeof(DbFileBlock), cond);
        }

        public virtual long SizeInBytes()
        {
            long count = (long)_file.BlockSet.Count();
            return count * _file.BufferSize;
        }
    }
}
