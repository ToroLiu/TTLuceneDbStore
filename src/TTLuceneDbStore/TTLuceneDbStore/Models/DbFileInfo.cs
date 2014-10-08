using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTLuceneDbStore.Helper;

namespace TTLuceneDbStore.Models
{
    public class DbFileInfo
    {
        [Key]
        public long Id { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedTimeUTC { get; set; }
        public DateTime ModifiedTimeUTC { get; set; }
        public string SHA1 { get; set; }
        public bool IsDeleted { get; set; }
        public long Length { get; set; }

        /// <summary>
        /// 檔案的BufferSize大小。這檔案的所有DbFileBlock會以這個值為基準，分成各個Blocks。
        /// 不同size的FileBlock是不允許的，所造成的行為目前未知。
        /// </summary>
        public long BufferSize { get; set; }
        
        [ForeignKey("Directory")]
        public long DirectoryId { get; set; }
        public virtual DbDirectory Directory { get; set; }

        public virtual ICollection<DbFileBlock> BlockSet { get; set; }

        public const long kDefaultBlockSize = 4096; //4 KB

        public DbFileInfo()
        {
            this.CreatedTimeUTC = this.ModifiedTimeUTC = DateTime.UtcNow;
            this.Length = 0;
            this.IsDeleted = false;
            this.FileName = string.Empty;

            this.BufferSize = kDefaultBlockSize;
            this.BlockSet = new HashSet<DbFileBlock>();
        }
        public DbFileInfo(string fileName) : this()
        {
            this.FileName = fileName;
        }

        internal void ComputeSHA() { 
            // Compute all file block sha1.
            var allSha =
                (from b in this.BlockSet
                 select b.SHA1).ToArray();

            string longSha = string.Join("", allSha);
            this.SHA1 = SHAHelper.ToSHA1(longSha);
        }
    }
}
