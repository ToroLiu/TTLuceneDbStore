using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DemoContactSearch.Models;

namespace TestDemoContactSearch
{
    [TestClass]
    public class UnitTestDemoDbContact
    {
        [TestMethod]
        public void TestContactFields()
        {
            using (DemoDbContext db = new DemoDbContext())
            {

            }
        }
    }
}
