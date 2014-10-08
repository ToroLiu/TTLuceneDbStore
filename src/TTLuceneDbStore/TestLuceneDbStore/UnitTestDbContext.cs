using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TTLuceneDbStore.Models;
using System.Text;

namespace TestLuceneDbStore
{
    [TestClass]
    public class UnitTestDbContext
    {
        [TestMethod]
        public void TestDBSchema()
        {
            Guid guid = Guid.NewGuid();

            using (DbFileContext db = new DbFileContext()) {
                DbDirectory dir = new DbDirectory()
                {
                    Name = "a_dir_" + guid.ToString(),
                };
                db.DirectorySet.Add(dir);

                DbFileInfo file = new DbFileInfo("a_file");
                dir.FileSet.Add(file);

                DbFileBlock block = new DbFileBlock(100);
                byte[] bytes = Encoding.UTF8.GetBytes("Hello World");
                Array.Copy(bytes, block.RawData, bytes.Length);
                file.BlockSet.Add(block);

                db.SaveChanges();
            }
        }
    }
}
