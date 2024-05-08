using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;
using static AssemblyVersionIncrementer.IncrementerError;

namespace AssemblyVersionIncrementer
{
    public class IncrementerError : Exception
    {
        public enum ErrorTypes { Ok, Info, Warning, Error };
        public ErrorTypes ErrorType = ErrorTypes.Error;

        public IncrementerError(string message) : base(message) { }

        public IncrementerError(ErrorTypes errorType, string message) : base(message)
        {
            ErrorType = errorType;
        }

        public IncrementerError(string message, Exception inner) : base(message, inner) { }

        public static IncrementerError Ok(string message)
            => new IncrementerError(ErrorTypes.Ok, message);

        public static IncrementerError Info(string message)
            => new IncrementerError(ErrorTypes.Info, message);

        public static IncrementerError Warning(string message)
            => new IncrementerError(ErrorTypes.Warning, message);

        public static IncrementerError Error(string message)
            => new IncrementerError(ErrorTypes.Error, message);
    }
}
