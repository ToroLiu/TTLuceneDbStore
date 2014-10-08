using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTLuceneDbStore.Models;
using LLock = Lucene.Net.Store.Lock;

namespace TTLuceneDbStore.Store
{
    public class EFDbLock : LLock
    {
        private DbFileContext _db = null;
        private string _lockName = string.Empty;
        private Guid _srcGuid = Guid.Empty;
        private readonly static TimeSpan _expireOffset = new TimeSpan(0, 20, 0);

        public EFDbLock(DbFileContext db, string lockName, Guid srcGuid)
        {
            _db = db;
            _srcGuid = srcGuid;
            _lockName = lockName;
        }

        private bool CheckTimeout(DbLuceneLock dbLock) {
            DateTime now = DateTime.UtcNow;
            if (dbLock.ExpireTimeUTC < now)
            {
                return true;
            }
            return false;
        }

        public override bool IsLocked()
        {
            // Check lockName only. The lock may be from another source.
            DbLuceneLock dbLock = _db.LuceneLockSet.SingleOrDefault(obj => obj.LockName.Equals(_lockName));
            if (dbLock == null)
                return false;

            // If the lock is timeout. Just release it.
            bool timeout = CheckTimeout(dbLock);
            if (timeout)
                return false;

            return true;
        }
        
        /// <summary>
        /// DbLuceneLock啟用Optimistic Locking的機制。如果該lock object有多個人修改，會造成Exception。以此處理concurrency的問題。
        /// </summary>
        /// <returns></returns>
        public override bool Obtain()
        {
            DbLuceneLock dbLock = _db.LuceneLockSet.SingleOrDefault(obj => obj.LockName.Equals(_lockName));
            if (dbLock != null && CheckTimeout(dbLock) == false)
                return false;

            if (dbLock == null)
            {
                dbLock = new DbLuceneLock(_lockName);
                dbLock.LockSource = _srcGuid;
                dbLock.ExpireTimeUTC = DateTime.UtcNow.Add(_expireOffset);
                _db.LuceneLockSet.Add(dbLock);
                _db.SaveChanges();
                return true;
            }

            dbLock.LockSource = _srcGuid;
            dbLock.ExpireTimeUTC = DateTime.UtcNow.Add(_expireOffset);
            _db.SaveChanges();
            return true;
        }

        public override void Release()
        {
            DbLuceneLock dbLock = _db.LuceneLockSet.SingleOrDefault(obj => obj.LockName.Equals(_lockName) && obj.LockSource.Equals(_srcGuid));
            if (dbLock == null)
                return;

            _db.LuceneLockSet.Remove(dbLock);
            _db.SaveChanges();
        }
    }

    public class EFDbLockFactory : LockFactory
    {
        private DbFileContext _db = null;
        private string _dirPath = string.Empty;
        private Guid _srcGuid = Guid.Empty;

        public EFDbLockFactory(DbFileContext db, Guid srcGuid, string dir)
        {
            _db = db;
            _srcGuid = srcGuid;
            internalLockPrefix = dir;
        }

        public override LLock MakeLock(string lockName)
        {
            if (string.IsNullOrEmpty(internalLockPrefix) != true)
            {
                lockName = internalLockPrefix + "-" + lockName;
            }
            return new EFDbLock(_db, lockName, _srcGuid);
        }

        public override void ClearLock(string lockName)
        {
            DbLuceneLock dbLock = _db.LuceneLockSet.SingleOrDefault(obj => obj.LockName.Equals(lockName) && obj.LockSource.Equals(_srcGuid));
            if (dbLock == null)
                return;

            _db.LuceneLockSet.Remove(dbLock);
            _db.SaveChanges();
        }
    }
}
