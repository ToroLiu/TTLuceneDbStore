using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTLuceneDbStore.Models
{
    public class DbFileContext : DbContext
    {
        public DbSet<DbDirectory> DirectorySet { get; set; }
        public DbSet<DbFileInfo> FileSet { get; set; }
        public DbSet<DbFileBlock> FileBlockSet { get; set; }
        public DbSet<DbFileLock> FileLockSet { get; set; }
        public DbSet<DbLuceneLock> LuceneLockSet { get; set; }

        public DbFileContext() {}

        public void Detach(object obj)
        {
            if (obj == null)
                return;

            DbEntityEntry entry = this.Entry(obj);
            entry.State = EntityState.Detached;
        }
        
    }
}
