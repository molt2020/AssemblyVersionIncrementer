using System;
using System.Collections.Generic;
using System.Diagnostics;
using static AssemblyVersionIncrementer.IncrementerError;

namespace AssemblyVersionIncrementer
{
    public class ConsoleUtil
    {
        public static void SetConsoleColor(ErrorTypes errorType)
        {
            switch (errorType)
            {
                case IncrementerError.ErrorTypes.Ok:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;

                case IncrementerError.ErrorTypes.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                case IncrementerError.ErrorTypes.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;

                case IncrementerError.ErrorTypes.Info:
                default:
                    Console.ResetColor();
                    break;
            }
        }
        public static void ErrorsToConsole(List<IncrementerError> errors, string category = "", bool quiet = false)
        {
            foreach (var error in errors)
                WriteLn(
                    messageType: error.ErrorType,
                    message: "\t" + error,
                    category: category,
                    quiet: quiet
                    );
        }

        private static string PrepMessage(string message, string category = "")
        {
            bool printCategory = !string.IsNullOrEmpty(category);
            return printCategory ? $"[{category}] {message}" : message;
        }

        public static void WriteLn(IncrementerError.ErrorTypes messageType, string message, string category = "", bool quiet = false)
        {
            if (!quiet)
            {

                SetConsoleColor(messageType);
                Console.WriteLine(PrepMessage(message, category));
#if DEBUG
                Debug.WriteLine(PrepMessage(message, category));
#endif
                Console.ResetColor();
            }
        }

        public static void Write(IncrementerError.ErrorTypes messageType, string message, string category = "", bool quiet = false)
        {
            if (!quiet)
            {
                SetConsoleColor(messageType);
                Console.Write(PrepMessage(message, category));
#if DEBUG
                Debug.Write(PrepMessage(message, category));
#endif
                Console.ResetColor();
            }
        }
    }
}
