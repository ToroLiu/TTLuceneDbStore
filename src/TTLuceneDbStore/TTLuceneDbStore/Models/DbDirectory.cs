using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TTLuceneDbStore.Models
{
    public class DbDirectory
    {
        [Key]
        public long Id { get; set; }
        
        [MaxLength(256)]
        public string Name { get; set; }
        public DateTime CreatedTimeUTC { get; set; }
        public DateTime ModifiedTimeUTC { get; set; }
        
        public virtual ICollection<DbFileInfo> FileSet { get; set; }

        public DbDirectory()
        {
            this.FileSet = new HashSet<DbFileInfo>();
            this.CreatedTimeUTC = this.ModifiedTimeUTC = DateTime.UtcNow;
        }
    }
}
