using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TTLuceneDbStore.Store;
using Lucene.Net.Store;
using Lucene.Net.Index;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;

namespace TestLuceneDbStore
{
    /// <summary>
    /// Summary description for UnitTestEFDbLucene
    /// </summary>
    [TestClass]
    public class UnitTestEFDbLucene
    {
        public UnitTestEFDbLucene()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        #region 基本測試 (讀/寫)
        /// <summary>
        /// 測試EFDbIndexOutput, EFDbIndexInput, EFDbDirectory, EFDbLock基本功能。
        /// </summary>
        [TestMethod]
        public void TestEFDb_BasicIO()
        {
            using (EFDbDirectory dir = EFDbDirectory.OpenDir("test")) 
            { 
                // Write into indexes.
                using (IndexWriter writer = new IndexWriter(dir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), true, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    Tuple<string, string>[] testSet = new[] { 
                        Tuple.Create("Kitty", "A cat named with Hello Kitty. It is very cute cat, and it earns more than I do."),
                        Tuple.Create("Snoopy", "A dog named with Snoopy. The dog is very humor, and it earns more than I do too. It never become older. A dog with magic power."),
                        Tuple.Create("TestUpdate", "Nothing"),
                    };

                    foreach (var t in testSet)
                    {
                        Document doc = new Document();
                        doc.Add(new Field("main", t.Item1, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
                        doc.Add(new Field("body", t.Item2, Field.Store.NO, Field.Index.ANALYZED));
                        writer.AddDocument(doc);
                    }
                }

                // Search from indexes.
                using (IndexSearcher searcher = new IndexSearcher(dir)) {
                    QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "body", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));
                    Query q = parser.Parse("dog");
                    TopDocs hits = searcher.Search(q, 2);
                    Assert.IsTrue(hits.TotalHits == 1);
    
                }
            }
        }

        [TestMethod]
        public void TestEFDB_AppendIndex() {
            using (EFDbDirectory dir = EFDbDirectory.OpenDir("test"))
            {
                // Write into indexes.
                using (IndexWriter writer = new IndexWriter(dir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), true, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    Tuple<string, string>[] testSet = new[] { 
                        Tuple.Create("Kitty", "A cat named with Hello Kitty. It is very cute cat, and it earns more than I do."),
                        Tuple.Create("Snoopy", "A dog named with Snoopy. The dog is very humor, and it earns more than I do too. It never become older. A dog with magic power."),
                        Tuple.Create("TestUpdate", "Nothing"),
                    };

                    foreach (var t in testSet)
                    {
                        Document doc = new Document();
                        doc.Add(new Field("main", t.Item1, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
                        doc.Add(new Field("body", t.Item2, Field.Store.NO, Field.Index.ANALYZED));
                        writer.AddDocument(doc);
                    }
                }
                
                // Append new indexes.
                using (IndexWriter writer = new IndexWriter(dir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), false, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    Tuple<string, string>[] testSet = new[] { 
                        Tuple.Create("Kitty", "A cat named with Hello Kitty. It is very cute cat, and it earns more than I do."),
                        Tuple.Create("Snoopy", "A dog named with Snoopy. The dog is very humor, and it earns more than I do too. It never become older. A dog with magic power."),
                        Tuple.Create("TestUpdate", "Nothing"),
                    };

                    foreach (var t in testSet)
                    {
                        Document doc = new Document();
                        doc.Add(new Field("main", t.Item1, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
                        doc.Add(new Field("body", t.Item2, Field.Store.NO, Field.Index.ANALYZED));
                        writer.AddDocument(doc);
                    }
                }

                // Search from indexes.
                using (IndexSearcher searcher = new IndexSearcher(dir))
                {
                    QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "body", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));
                    Query q = parser.Parse("dog");
                    TopDocs hits = searcher.Search(q, 5);
                    Assert.IsTrue(hits.TotalHits == 2);
                }
            }
        }

        /// <summary>
        /// 測試大檔的讀寫，是否正常。
        /// </summary>
        [TestMethod]
        public void TestEFDb_IndexBigFile()
        {
            using (EFDbDirectory dir = EFDbDirectory.OpenDir("file"))
            {
                dir.BufferSize = 1024; // 1KB, to test multiple File Blocks.

                // Write into indexes.
                using (IndexWriter writer = new IndexWriter(dir, new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), true, IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    Tuple<string, string>[] testSet = new[] { 
                        Tuple.Create("IndexReader.cs", Properties.Resources.IndexReader),
                        Tuple.Create("IndexWriter.cs", Properties.Resources.IndexWriter),
                    };

                    foreach (var t in testSet)
                    {
                        Document doc = new Document();
                        doc.Add(new Field("main", t.Item1, Field.Store.YES, Field.Index.NOT_ANALYZED_NO_NORMS));
                        doc.Add(new Field("body", t.Item2, Field.Store.NO, Field.Index.ANALYZED));
                        writer.AddDocument(doc);
                    }
                }

                // Search from indexes.
                using (IndexSearcher searcher = new IndexSearcher(dir))
                {
                    QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "body", new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));
                    Query q = parser.Parse("\"public class IndexWriter\"");
                    TopDocs hits = searcher.Search(q, 2);
                    Assert.IsTrue(hits.TotalHits == 1);
                }
            }

        }

        #endregion

        

    }
}
