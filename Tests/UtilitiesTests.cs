using Microsoft.VisualStudio.TestTools.UnitTesting;
using AssemblyVersionIncrementer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssemblyVersionIncrementer.Tests
{
    [TestClass()]
    public class UtilitiesTests
    {
        [TestMethod()]
        public void GetBackupNameTest()
        {
            var fileName = "test.txt";
            var backupName = Utilities.GetBackupName(fileName);
            Assert.IsTrue(!string.IsNullOrEmpty(backupName));
            Assert.IsFalse(backupName.Equals(fileName));
            Assert.IsTrue(backupName.Contains(".bak"));
            Assert.IsTrue(backupName.Length > fileName.Length);
        }

        [TestMethod()]
        public void BackupFileTest()
        {
            string fileName = "backup-test.txt";
            string contents = "test test";

            var dir = new DirectoryInfo(Utilities.GetCurrentFolder());

            foreach (var file in dir.EnumerateFiles("backup-test*"))
            {
                file.Delete();
            }

            File.WriteAllText(fileName, contents);

            var backupName = Utilities.BackupFile(fileName);
            Assert.IsTrue(!string.IsNullOrEmpty(backupName));
            Assert.IsTrue(backupName.Length > fileName.Length);

            string backupContent = File.ReadAllText(backupName);
            Assert.IsTrue(backupContent.Length > 0);
            Assert.IsTrue(backupContent == contents);
        }
    }
}