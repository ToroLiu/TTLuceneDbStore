using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LDirectory = Lucene.Net.Store.Directory;

using TTLuceneDbStore.Models;
using TTLuceneDbStore.Helper;
using System.Diagnostics.Contracts;
using System.Diagnostics;

namespace TTLuceneDbStore.Store
{
    public class EFDbDirectory : LDirectory, IDisposable
    {
        #region Class Members
        private Guid _dbGuid = Guid.Empty;

        private DbFileContext _db = null;
        private DbDirectory _dir = null;

        #endregion

        protected EFDbDirectory() {
            _db = new DbFileContext();
            _dbGuid = Guid.NewGuid();
        }

        protected bool Open(string dirPath)
        {
            Contract.Requires(_db != null);

            _dir = _db.DirectorySet.SingleOrDefault(obj => obj.Name.Equals(dirPath));
            if (_dir == null)
            {
                _dir = new DbDirectory();
                _dir.Name = dirPath;

                _db.DirectorySet.Add(_dir);
                _db.SaveChanges();
            }

            interalLockFactory = new EFDbLockFactory(_db, _dbGuid, dirPath);
            isOpen = (_dir != null);
            return isOpen;
        }

        private bool _disposed = false;
        protected override void Dispose(bool disposing)
        {
            // 清掉所有這個Directory所產生的Lock
            if (_disposed) return;
            if (disposing)
            {
                isOpen = false;

                _db.Dispose();
                _db = null;
            }

            interalLockFactory = null;
            _db = null;
            _dir = null;

            _disposed = true;
        }

        /// <summary>
        /// Open or Create a directory.
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        public static EFDbDirectory OpenDir(string dirPath)
        {
            EFDbDirectory efDir = new EFDbDirectory();
            bool done = efDir.Open(dirPath);
            if (!done)
            {
                throw new NoSuchDirectoryException("Failed to open the directory.");
            }
            return efDir;
        }

        #region File Operations
        public override IndexInput OpenInput(string name)
        {
            EFDbIndexInput file = new EFDbIndexInput(_db, _dir);
            bool done = file.Open(name);
            if (!done)
                return null;

            return file;
        }
        
        protected long _bufferSize = 64 * 1024; //64 KB
        public long BufferSize
        {
            get { return _bufferSize; }
            set
            {
                long val = Math.Max(1024, value);
                _bufferSize = val;
            }
        }

        public override IndexOutput CreateOutput(string name)
        {
            EFDbIndexOutput file = new EFDbIndexOutput(_db, _dir);
            file.OpenOrCreate(name, _bufferSize);

            return file;
        }
        
        public override bool FileExists(string name)
        {
            bool exist = _dir.FileSet.Any(obj => obj.FileName.Equals(name));
            return exist;
        }

        public override void DeleteFile(string name)
        {
            DbFileInfo file = _dir.FileSet.SingleOrDefault(obj => obj.FileName.Equals(name));
            if (file != null)
            {
                _dir.FileSet.Remove(file);
                _db.FileSet.Remove(file);
                _db.SaveChanges();
            }
        }

        public override long FileLength(string name)
        {
            /// <summary>Returns the length in bytes of a file in the directory. </summary>
            DbFileInfo file = _dir.FileSet.SingleOrDefault(obj => obj.FileName.Equals(name));
            if (file == null)
                return 0;
            
            return file.Length;
        }
        public override long FileModified(string name)
        {
            DbFileInfo file = _dir.FileSet.Single(obj => obj.FileName.Equals(name));
            Debug.Assert(file != null);

            return file.ModifiedTimeUTC.ToLongInt();
        }
        public override string[] ListAll()
        {
            string[] allFile =
                (from f in _dir.FileSet
                 select f.FileName).ToArray();

            return allFile;
        }

        public override void TouchFile(string name)
        {
            DbFileInfo file = _dir.FileSet.SingleOrDefault(obj => obj.FileName.Equals(name));
            Debug.Assert(file != null);

            file.ModifiedTimeUTC = DateTime.UtcNow;
            _db.SaveChanges();
        }

        public override string GetLockId()
        {
            string name = "efdbdir_" + _dir.Name;
            return name;
        }

        #endregion
    }
}
