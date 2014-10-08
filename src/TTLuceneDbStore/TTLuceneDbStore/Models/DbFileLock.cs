using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTLuceneDbStore.Models
{
    public enum LockType
    {
        /// <summary>
        /// Exclusive lock, or write lock...
        /// </summary>
        Exclusive = 0,

        Reader = 1,
    }

    /// <summary>
    /// 讀寫檔案時，需要做一層Lock。
    /// 這邊，需要做read和write的Lock機制！？
    /// </summary>
    public class DbFileLock
    {
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// 誰設了Lock。
        /// </summary>
        public Guid LockOwner { get; set; }
        public long FileId { get; set; }
        public DateTime LockedTimeUTC { get; set; }
        public DateTime ExpiredTimeUTC { get; set; }

        [Timestamp]
        public byte[] db_row_version { get; set; }

        [NotMapped]
        public string RowVersion
        {
            get
            {
                if (db_row_version == null || db_row_version.Any() == false)
                    return string.Empty;

                return Convert.ToBase64String(db_row_version);
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    db_row_version = null;
                }
                else
                {
                    db_row_version = Convert.FromBase64String(value);
                }
            }
        }
        public LockType CurLockType { get; set; }

        public DbFileLock()
        {
            this.LockedTimeUTC = this.ExpiredTimeUTC = DateTime.UtcNow;
            this.CurLockType = LockType.Exclusive;
        }
    }
}
