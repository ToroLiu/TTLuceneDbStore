using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTLuceneDbStore.Models
{
    /// <summary>
    /// LUCENE自己，有實作一層Lock的機制。
    /// </summary>
    public class DbLuceneLock
    {
        [Key]
        public long Id { get; set; }
        public string LockName { get; set; }
        public Guid LockSource { get; set; }
        public DateTime LockTimeUTC { get; set; }
        public DateTime ExpireTimeUTC { get; set; }

        /// <summary>
        /// Enable optimistic locking mechanisam.
        /// </summary>
        [Timestamp]
        public byte[] db_row_version { get; set; }

        [NotMapped]
        public string RowVersion { 
            get 
            {
                if (db_row_version == null || db_row_version.Any() == false)
                    return string.Empty;

                return Convert.ToBase64String(db_row_version);
            }

            set {
                if (string.IsNullOrEmpty(value))
                    db_row_version = null;

                db_row_version = Convert.FromBase64String(value);
            }
        }

        public DbLuceneLock()
        {
            LockTimeUTC = ExpireTimeUTC = DateTime.UtcNow;
        }
        public DbLuceneLock(string lockName) : this()
        {
            LockName = lockName;
        }
    }
}
