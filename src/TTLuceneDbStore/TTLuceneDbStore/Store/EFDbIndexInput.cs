using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTLuceneDbStore.Models;
using TTLuceneDbStore.Helper;
using System.Diagnostics.Contracts;

namespace TTLuceneDbStore.Store
{
    public class EFDbIndexInput : IndexInput
    {
        private DbFileContext _db = null;
        private DbDirectory _dir = null;
        private DbFileInfo _file = null;
        private DbFileBlock _file_block = null;

        private long _buffer_start = 0;
        private long _buffer_position = 0;
        private long _buffer_length = 0;
        private long _block_index = -1;

        public EFDbIndexInput(DbFileContext db, DbDirectory dir)
        {
            Contract.Requires(_db != null && dir != null);

            _db = db;
            _dir = dir;
        }

        public bool Open(string fileName)
        {
            //! Open existed file
            _file = _dir.FileSet.SingleOrDefault(obj => obj.FileName.Equals(fileName));
            return (_file != null);
        }

        protected override void Dispose(bool disposing)
        {
            _db = null;
            _dir = null;
            _file = null;
            _file_block = null;
        }
        
        public override long FilePointer
        {
            get {
                if (_block_index < 0)
                    return 0;

                long curPos = _buffer_start + _buffer_position;
                return curPos; 
            }
        }
        public override long Length()
        {
            return _file.Length;
        }

        private void SwitchBlock(bool enforceEOF)
        {
            DbFileBlock oldBlock = null;
            if (_file_block != null)
            {
                oldBlock = _file_block;
            }

            long maxBlock = _file.BlockSet.Count();
            if (_block_index >= maxBlock)
            {
                if (enforceEOF)
                {
                    throw new System.IO.IOException("Read past EOF");
                }
                else
                {
                    _block_index -= 1;
                    _buffer_length = _file.BufferSize;

                }
            }
            else
            {
                _file_block = _file.BlockSet.SingleOrDefault(obj => obj.Order == _block_index);
                _buffer_start = _block_index * _file.BufferSize;
                _buffer_position = 0;

                long bufLen = _file.Length - _buffer_start;
                _buffer_length = (bufLen > _file.BufferSize) ? _file.BufferSize : bufLen;
            }

            if (oldBlock != _file_block && oldBlock != null)
            {
                //_db.Detach(oldBlock);
                oldBlock = null;
            }
        }

        public override void Seek(long pos)
        {
            if (pos < _buffer_start || (_buffer_start + _buffer_position) < pos)
            {
                // switch buffer
                _block_index = pos / _file.BufferSize;
                SwitchBlock(false);    
            }

            _buffer_position = pos % _file.BufferSize;
        }
        public override byte ReadByte()
        {
            if (_buffer_position >= _buffer_length)
            {
                _block_index += 1;
                SwitchBlock(true);
            }

            byte b = _file_block.RawData[_buffer_position];
            _buffer_position += 1;
            return b;
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            long curLen = (long)len;
            long curOffset = (long)offset;

            while (curLen > 0)
            {
                if (_buffer_position >= _buffer_length)
                {
                    _block_index += 1;
                    SwitchBlock(true);
                }

                long remainInBuffer = _buffer_length - _buffer_position;
                long bytesToCopy = Math.Min(curLen, remainInBuffer);
                Array.Copy(_file_block.RawData, _buffer_position, b, curOffset, bytesToCopy);

                _buffer_position += bytesToCopy;
                curOffset += bytesToCopy;
                curLen -= bytesToCopy;
            }
        }
    }
}
