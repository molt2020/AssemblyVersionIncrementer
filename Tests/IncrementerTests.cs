using Microsoft.VisualStudio.TestTools.UnitTesting;
using AssemblyVersionIncrementer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tests;
using System.Security.Cryptography.X509Certificates;

namespace AssemblyVersionIncrementer.Tests
{
    [TestClass()]
    public class IncrementerTests
    {
        [TestMethod()]
        public void IncrementVersionTest()
        {
            List<IncrementParamsTest> tests = new List<IncrementParamsTest>()
            {
                new IncrementParamsTest
                {
                    CurrentVersion = "1",
                    ExpectedVersion = "2",
                    IncrementPosition = 0,
                    IncrementBy = 1
                },

                new IncrementParamsTest
                {
                    CurrentVersion = "1.1",
                    ExpectedVersion = "1.3",
                    IncrementPosition = 1,
                    IncrementBy = 2
                },

                new IncrementParamsTest
                {
                    CurrentVersion = "1.1.1",
                    ExpectedVersion = "1.1.2",
                    IncrementPosition = 2,
                    IncrementBy = 1
                },

                new IncrementParamsTest
                {
                    CurrentVersion = "1.1.1.1",
                    ExpectedVersion = "1.1.1.2",
                    IncrementPosition = 3,
                    IncrementBy = 1
                },

                new IncrementParamsTest
                {
                    CurrentVersion =  "1.1.1.1.1.1.1.1",
                    ExpectedVersion = "1.1.1.2.1.1.1.1",
                    IncrementPosition = 3,
                    IncrementBy = 1
                },

                new IncrementParamsTest
                {
                    CurrentVersion = "",
                    ExpectedVersion = "1",
                    IncrementPosition = 0,
                    IncrementBy = 1
                },

            };

            bool success = false;
            string incremented = string.Empty;

            foreach (var test in tests)
            {
                (success, incremented) = Incrementer.IncrementVersion(
                    test.CurrentVersion,
                    test.IncrementPosition,
                    test.IncrementBy
                    );
                Assert.IsTrue(success, $"Test reported failed conversion " +
                    $"(success=false, version='{test.CurrentVersion}')");
                Assert.IsTrue(incremented == test.ExpectedVersion, $"{incremented} != {test.ExpectedVersion}");
            }


            // now let's test setVersion parameter - it should override anything

            (success, incremented) = Incrementer.IncrementVersion("1.2.3", 0, 1, "9.9.9.9");
            Assert.IsTrue(success);
            Assert.IsTrue(incremented == "9.9.9.9");
        }


        [TestMethod()]
        public void IncrementVersionBadEntriesTest()
        {
            List<IncrementParamsTest> badTests = new List<IncrementParamsTest>()
            {
                new IncrementParamsTest
                {
                    CurrentVersion = "1.1.1",
                    ExpectedVersion = "1.1.1",
                    IncrementPosition = 3, // out of range
                    IncrementBy = 1
                },
                new IncrementParamsTest
                {
                    CurrentVersion = "1.a.1",
                    ExpectedVersion = "1.b.1",
                    IncrementPosition = 1,
                    IncrementBy = 1
                },
            };

            int expectedFailures = badTests.Count();
            int failedCount = 0;

            foreach (var test in badTests)
            {
                try
                {
                    (bool success, string incremented) = Incrementer.IncrementVersion(
                           test.CurrentVersion,
                           test.IncrementPosition,
                           test.IncrementBy
                           );
                    if (!success) failedCount++;
                    Assert.IsFalse(success, $"Test reported successful increment where " +
                        $"we expected failure '{test.CurrentVersion}'");
                    Assert.IsTrue(incremented == test.CurrentVersion, $"Return value on failure " +
                        $"should be the same as input value {test.CurrentVersion}");
                }
                catch (Exception)
                {
                    failedCount++;
                }
            }
            Assert.IsTrue(failedCount == expectedFailures,
                $"Failed less times than expected failedCount={failedCount} < {expectedFailures}");
        }

        [TestMethod()]
        public void ProcessSDKFileTest()
        {
            string testFile1 = @"TestFiles\in_sdk_style_test_2.csproj";
            (var errors, var xmlDoc) = Incrementer.ProcessSDKFile(testFile1, 2, 1); // increment 1.1.1 to 1.1.2
            int errorCount = errors.Where(e =>
                e.ErrorType == IncrementerError.ErrorTypes.Error).Count();

            string errorsMessage = string.Join(Environment.NewLine,
                errors.Where(e => e.ErrorType == IncrementerError.ErrorTypes.Error)
                );

            Assert.IsTrue(errorCount == 0, $"Errors processing SDK file {testFile1}" +
                $"\r\n{errorsMessage}");

            string outFile = @"out_sdk_style_test_2.csproj";
            xmlDoc.Save(outFile);
            string expectedFile1 = @"TestFiles\expected_sdk_style_test_2.csproj";

            var checksumExpected = Utilities.CalculateMD5(expectedFile1);
            var checksumOut = Utilities.CalculateMD5(outFile);

            Assert.IsTrue(checksumExpected == checksumOut, $"Checksums between '{outFile}' and expected '{expectedFile1}' do not match");
        }

        [TestMethod()]
        public void GetVersionFromAssemblyFileTest()
        {
            List<string> testLines = new List<string>()
            {
                "using System.Reflection;",
                "\t[assembly: AssemblyVersion(\"1.23.4.5\")]",
                "\t[assembly: AssemblyFileVersion(\"2.34.5.6\")]",
                "\r\n",
                "",
                "   "
            };

            List<string> expectedOutcomeAssemblyVersion = new List<string>()
            {
                "",
                "1.23.4.5",
                "",
                "",
                "",
                ""
            };

            List<string> expectedOutcomeAssemblyFileVersion = new List<string>()
            {
                "",
                "",
                "2.34.5.6",
                "",
                "",
                ""
            };

            for (int i = 0; i < testLines.Count; i++)
            {
                string line = testLines[i];
                (bool success, string newVersion) = Incrementer.GetVersionFromAssemblyFile("AssemblyVersion", line);
                Assert.IsTrue(newVersion == expectedOutcomeAssemblyVersion[i], $"AssemblyVersion {i}: {newVersion} != {expectedOutcomeAssemblyVersion[i]}");
            }

            for (int i = 0; i < testLines.Count; i++)
            {
                string line = testLines[i];
                (bool success, string newVersion) = Incrementer.GetVersionFromAssemblyFile("AssemblyFileVersion", line);
                Assert.IsTrue(newVersion == expectedOutcomeAssemblyFileVersion[i], $"AssemblyFileVersion {i}: {newVersion} != {expectedOutcomeAssemblyVersion[i]}");
            }
        }

        [TestMethod()]
        public void ProcessAssemblyFileTest()
        {
            string testFile1 = @"TestFiles\in_old_style_AssemblyFileVersion_test3.cs";
            (var errors, List<string> newLines) = Incrementer.ProcessAssemblyFile(testFile1, 2, 1); // increment 1.1.1 to 1.1.2
            int errorCount = errors.Where(e =>
                e.ErrorType == IncrementerError.ErrorTypes.Error).Count();

            string errorsMessage = string.Join(Environment.NewLine,
                errors.Where(e => e.ErrorType == IncrementerError.ErrorTypes.Error)
                );

            Assert.IsTrue(errorCount == 0, $"Errors processing AssemblyFileVersion.cs file {testFile1}" +
                $"\r\n{errorsMessage}");


            string outFile = @"out_old_style_AssemblyFileVersion_test3.cs";
            File.WriteAllLines(outFile, newLines);

            string expectedFile1 = @"TestFiles\expected_old_style_AssemblyFileVersion_test3.cs";

            var checksumExpected = Utilities.CalculateMD5(expectedFile1);
            var checksumOut = Utilities.CalculateMD5(outFile);

            Assert.IsTrue(checksumExpected == checksumOut, $"Checksums between '{outFile}' and expected '{expectedFile1}' do not match");
        }

        [TestMethod()]
        public void GetFileTypeTest()
        {
            Assert.IsTrue(
                Incrementer.GetFileType(@"TestFiles\expected_sdk_style_test_2.csproj")
                == Incrementer.VersionFileType.SDK
                );

            Assert.IsTrue(
                Incrementer.GetFileType(@"TestFiles\in_old_style_AssemblyFileVersion_test3.cs")
                == Incrementer.VersionFileType.AssemblyInfo
                );

            Assert.IsTrue(
              Incrementer.GetFileType(@"TestFiles\in_old_style_AssemblyFileVersion_test3.xml")
              == Incrementer.VersionFileType.Unknown
              );

            Assert.IsTrue(
               Incrementer.GetFileType(@"c:\TestFiles\in_old_style_AssemblyFileVersion_test3.csproj")
               == Incrementer.VersionFileType.SDK
               );

            Assert.IsTrue(
               Incrementer.GetFileType(@"\\shared-folder\TestFiles\in_old_style_AssemblyFileVersion_test3.csproj")
               == Incrementer.VersionFileType.SDK
               );

            Assert.IsTrue(
             Incrementer.GetFileType(@"/we/use/unix/style/too/in_old_style_AssemblyFileVersion_test3.csproj")
             == Incrementer.VersionFileType.SDK
             );

        }
    }
}