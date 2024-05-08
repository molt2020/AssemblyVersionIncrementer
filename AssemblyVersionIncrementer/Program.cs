using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Xml;
using static AssemblyVersionIncrementer.IncrementerError;

namespace AssemblyVersionIncrementer
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            string appName = System.AppDomain.CurrentDomain.FriendlyName;
            string myVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

            //ConsoleUtil.WriteLn(ErrorTypes.Ok, category: appName, message: $"{appName} v{myVersion}");

            #region Command Line Options
            var fileOption = new Option<string>(
             name: "--file",
             description: "The file to process.")
            {
                IsRequired = true,
            };
            fileOption.AddAlias("-f");

            var incrementPosition = new Option<int>(
              name: "--set-increment-position",
              description: "Zero-based index of position within the version string to increment, " +
              "e.g. if version string is '1.2.3' and increment position is set to 1, this will " +
              "increment number '2' from the version string)"
              )
            { IsRequired = true };
            incrementPosition.AddAlias("-sip");

            var quietOption = new Option<bool>(
                name: "--quiet",
                description: "Quiet operation - do not print messages",
                getDefaultValue: () => false
                );
            quietOption.AddAlias("-q");

            var forceOption = new Option<bool>(
              name: "--force",
              description: "Add version information if not present in the file",
              getDefaultValue: () => false);

            var backupOption = new Option<bool>(
               name: "--backup",
               description: "Keep old version of file as backup",
               getDefaultValue: () => true
               );
            backupOption.AddAlias("-b");

            /// Version specific options
            var setVersionOption = new Option<string>(
               name: "--set-version",
               description: "Set specific version instead of incrementing (e.g. '1.2.3')",
               getDefaultValue: () => ""
               );
            setVersionOption.AddAlias("-sv");



            var incrementBy = new Option<int>(
              name: "--set-increment-by",
              description: "Sets the value for the increment (i.e. by how much the version string should be incremented)",
              getDefaultValue: () => 1
              );
            incrementBy.AddAlias("-sib");

            var rootCommand = new RootCommand(
                "Increment Product, Assembly, and File versions for specified C# project. " +
                "This utility can be used for both SDK and .net formats at the same time. " +
                "Exit codes: 0 = success, 1 = errors, 78 = warnings")

            //var processCommand = new Command("Options")
            {
                fileOption,
                backupOption,
                quietOption,
                forceOption,
                setVersionOption,
                incrementBy,
                incrementPosition
            };

            //rootCommand.AddCommand(processCommand);

            rootCommand.SetHandler((
                file,
                backup,
                quiet,
                force,
                setVersion,
                incrementBy,
                incrementPosition
                ) =>
                {
                    ProcessFile(appName, myVersion, file!, backup, quiet, force, setVersion, incrementBy, incrementPosition);
                },
                fileOption, backupOption, quietOption, forceOption, setVersionOption, incrementBy, incrementPosition);
            #endregion
            //return await rootCommand.InvokeAsync(args);
            return rootCommand.Invoke(args);
        }


        /// <summary>
        /// This is the real entry point for processing the file
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="fileName"></param>
        /// <param name="doBackup"></param>
        /// <param name="beQuiet"></param>
        /// <param name="forceParams"></param>
        /// <param name="setVersion"></param>
        /// <param name="incrementBy"></param>
        /// <param name="incrementPosition"></param>
        /// <returns></returns>
        public static int ProcessFile(
            string appName,
            string myVersion,
            string fileName,
            bool doBackup,
            bool beQuiet,
            bool forceParams,
            string setVersion,
            int incrementBy,
            int incrementPosition
            )
        {
            List<IncrementerError> errors = new List<IncrementerError>();
            ConsoleUtil.WriteLn(ErrorTypes.Info, $"{appName} [{myVersion}]: " + fileName, category: appName, quiet: beQuiet);
#if DEBUG
            Debug.WriteLine($"Debug: Start processing file {fileName}");
#endif
            try
            {
                if (doBackup)
                {
                    ConsoleUtil.Write(ErrorTypes.Info, "Backing up... ", category: appName, quiet: beQuiet);
                    string backupFileName = Utilities.BackupFile(fileName);
                    ConsoleUtil.WriteLn(ErrorTypes.Info, $"to file {backupFileName} completed.", quiet: beQuiet);
                }


                switch (Incrementer.GetFileType(fileName))
                {
                    /// Old-Style AssemblyInfo.cs
                    case (Incrementer.VersionFileType.AssemblyInfo):
                        List<string> lines = new List<string>();
                        (errors, lines) = Incrementer.ProcessAssemblyFile(fileName,
                            positionToIncrement: incrementPosition,
                            incrementBy: incrementBy,
                            setVersion: setVersion);
                        // ConsoleUtil.ErrorsToConsole(errors: errors, category: $"{appName}-AssemblyFileInfo", quiet: beQuiet);
                        if (Incrementer.HasErrors(errors) == 0)
                        {
                            ConsoleUtil.Write(ErrorTypes.Ok, $"Writing new version file {fileName}...", category: appName, quiet: beQuiet);
                            File.WriteAllLines(fileName, lines);
                            ConsoleUtil.WriteLn(ErrorTypes.Ok, $" completed.", quiet: beQuiet);
                        }
                        else
                            ConsoleUtil.WriteLn(ErrorTypes.Error, $"Not writing file (unresolved errors)", category: appName, quiet: beQuiet);
                        break;

                    /// SDK FILE
                    case (Incrementer.VersionFileType.SDK):
                        XmlDocument xmlDoc = new XmlDocument();
                        (errors, xmlDoc) = Incrementer.ProcessSDKFile(fileName,
                            positionToIncrement: incrementPosition,
                            incrementBy: incrementBy,
                            setVersion: setVersion);
                        // ConsoleUtil.ErrorsToConsole(errors: errors, category: $"{appName}-SDK", quiet: beQuiet);
                        if (Incrementer.HasErrors(errors) == 0)
                        {
                            ConsoleUtil.Write(ErrorTypes.Ok, $"Writing new version file {fileName}...", category: appName, quiet: beQuiet);
                            xmlDoc?.Save(fileName);
                            ConsoleUtil.WriteLn(ErrorTypes.Ok, $" completed.", quiet: beQuiet);
                        }
                        else
                            ConsoleUtil.WriteLn(ErrorTypes.Error, $"Not writing file (unresolved errors)", category: appName, quiet: beQuiet);
                        break;

                    default:
                        errors.Add(IncrementerError.Error($"Unknown file type '{fileName}'"));
                        break;
                }
            }
            catch (Exception ex)
            {
                errors.Add(IncrementerError.Error("Failed: " + ex.Message));
            }

            ConsoleUtil.ErrorsToConsole(errors: errors, category: appName, quiet: beQuiet);

            int errorCount = Incrementer.HasErrors(errors);
            int warningCount = Incrementer.HasWarnings(errors);
            if (errorCount > 0)
                ConsoleUtil.WriteLn(
                messageType: ErrorTypes.Error,
                category: appName,
                message: $"Failed with errors ({errorCount})",
                quiet: beQuiet);
            else
                ConsoleUtil.WriteLn(
                    messageType: ErrorTypes.Ok,
                    category: appName,
                    message: $"Succeeded with ({warningCount}) warnings",
                    quiet: beQuiet);
#if DEBUG
            Debug.WriteLine($"Debug: Finished processing file {fileName}");
#endif
            if (errorCount > 0) return 1; // standard exit code for error
            if (warningCount > 0) return 78; // configuration error exit code on Linux
            return 0; // success exit code
        }

    }
}