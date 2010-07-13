using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

  

namespace WPFSimpleDCM
{
    [TestFixture]
    class Tester
    {
        private int i;
        [SetUp]
        public void Init()
        {
            i = 3;
        }

        private AutoResetEvent isEnded;
        [Test]
        public void Test1()
        {
            isEnded = new AutoResetEvent(false);
            FolderDiff fld = new FolderDiff("e:\\temp\\b1","e:\\temp\\b2");
            fld.DoCompare(onStatus, onComplete  );
            isEnded.WaitOne();
            string fn = fld.ItemsAll[0].FullName;
        }

        private void onComplete(string status, int percent)
        {
            isEnded.Set();
        }

        private void onStatus(string status, int percent)
        {
            //throw new NotImplementedException();
        }

        [Test]
        public void Test2()
        {
            FolderDiff fld = new FolderDiff("e:\\temp\\b1","e:\\temp\\b2");
System.Diagnostics.Stopwatch st=  System.Diagnostics.Stopwatch.StartNew();
            
            int cnt = fld.GetFolderCount("e:\\temp\\b1");
            st.Stop();
            Console.WriteLine(st.ElapsedMilliseconds);
        }

        [Test]
        public void Test3()
        {
            Assert.AreEqual(1, 1 + 1 - 1);
            Assert.AreEqual(i++, 3);
        }
    }
}
