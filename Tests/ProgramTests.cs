using Microsoft.VisualStudio.TestTools.UnitTesting;
using AssemblyVersionIncrementer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace AssemblyVersionIncrementer.Tests
{


    [TestClass()]
    public class ProgramTests
    {
        [TestMethod()]
        public void ProcessFileTest()
        {
            string testFile1 = @"TestFiles\in_old_style_AssemblyFileVersion_test3.cs";
            string testFile2 = @"TestFiles\in_sdk_style_test_2.csproj";

            // bad files
            string testFile3 = @"TestFiles\best.txt"; // non existent file, bad extension
            string testFile4 = @"TestFiles\best.cs"; // good extension but non-existent file

            // use a copy of the file and you need to change extension of the file
            // from .bak to .cs in this case so the method will recognise the file as .cs
            var backup1 = Utilities.BackupFile(testFile1, backupExtension: ".cs");

            var result1 = Program.ProcessFile(
                appName: "Test",
                backup1,
                doBackup: true,
                beQuiet: false,
                forceParams: false,
                setVersion: "",
                incrementBy: 1,
                incrementPosition: 2
                );

            Assert.IsTrue(result1 == 0); // straight forward file, no errors, no warnings

            var backup2 = Utilities.BackupFile(testFile2, backupExtension: ".csproj");
            var result2 = Program.ProcessFile(
                 appName: "Test",
                backup2,
              doBackup: true,
              beQuiet: false,
              forceParams: false,
              setVersion: "",
              incrementBy: 1,
              incrementPosition: 2
            );
            Assert.IsTrue(result2 == 78); // no errors but will have warnings!


            var result3 = Program.ProcessFile(
                 appName: "Test",
                testFile3,
                doBackup: true,
                beQuiet: false,
                forceParams: false,
                setVersion: "",
                incrementBy: 1,
                incrementPosition: 2
              );
            Assert.IsTrue(result3 == 1);


            var result4 = Program.ProcessFile(
                 appName: "Test",
                    testFile4,
              doBackup: true,
              beQuiet: false,
              forceParams: false,
              setVersion: "",
              incrementBy: 1,
              incrementPosition: 2
            );
            Assert.IsTrue(result4 == 1);
        }
    }
}