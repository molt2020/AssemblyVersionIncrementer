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
    public class ConsoleUtilTests
    {
        [TestMethod()]
        public void ErrorsToConsoleTest()
        {
            List<IncrementerError> errors = new List<IncrementerError>()
            {
                IncrementerError.Error("error"),
                IncrementerError.Ok("ok"),
                IncrementerError.Info("info"),
                IncrementerError.Warning("warning")
            };
            ConsoleUtil.ErrorsToConsole(errors, "category");
        }
    }
}