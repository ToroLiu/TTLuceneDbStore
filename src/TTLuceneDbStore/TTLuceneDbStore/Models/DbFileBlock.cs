using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTLuceneDbStore.Helper;

namespace TTLuceneDbStore.Models
{
    /// <summary>
    /// The file blocks
    /// </summary>
    public class DbFileBlock
    {
        [Key]
        public long Id { get; set; }
        public long Order { get; set; }
        public byte[] RawData { get; set; }
        public long Length { get; set; }
        public string SHA1 { get; set; }

        public long FileId { get; set; }
        public virtual DbFileInfo File { get; set; }

        public DbFileBlock()
        {
            RawData = null;
            Length = 0;
        }
        public DbFileBlock(long size)
        {
            RawData = new byte[size];
            Length = size;
        }

        internal void ComputeSHA() {
            this.SHA1 = SHAHelper.ToSHA1(RawData);
        }
    }
}
