using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace AssemblyVersionIncrementer
{
    public class Incrementer
    {

        #region File Helpers
        public enum VersionFileType { Unknown, SDK, AssemblyInfo }

        /// <summary>
        /// Returns enum VersionFileType for given filename by 
        /// matching file extension to known types
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static VersionFileType GetFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            switch (extension)
            {
                case ".cs": return VersionFileType.AssemblyInfo;
                case ".csproj": return VersionFileType.SDK;
                default: return VersionFileType.Unknown;
            }
        }
        #endregion

        #region Version processing
        /// <summary>
        /// Increments version string e.g. 1.2.3.4 by incrementBy on 
        /// the position positionIncrement.and returns incremented version.
        /// 
        /// If setVersion is supplied then this is returned instead.
        /// </summary>
        /// <param name="versionString"></param>
        /// <param name="positionToIncrement"></param>
        /// <param name="incrementBy"></param>
        /// <param name="setVersion">If not empty string this will be set as version</param>
        /// <returns>(bool success, string incrementedVersion)</returns>
        public static (bool, string) IncrementVersion(
            string versionString,
            int positionToIncrement,
            int incrementBy,
            string setVersion = ""
            )
        {
            if (!string.IsNullOrWhiteSpace(setVersion)) return (true, setVersion);

            const string delimiter = ".";
            bool success = false;
            string result = versionString;

            string[] split = versionString.Split(delimiter.ToCharArray());
            if (positionToIncrement < split.Length)
            {
                Int32.TryParse(split[positionToIncrement], out int parsedNumber);
                parsedNumber = parsedNumber + incrementBy;
                split[positionToIncrement] = parsedNumber.ToString();
                success = true;
                result = string.Join(delimiter, split);
            }
            return (success, result);
        }

        #endregion

        #region File Processing Methods

        public static (List<IncrementerError>, XmlDocument) ProcessSDKFile(
            string fileName,
            int positionToIncrement,
            int incrementBy,
            string setVersion = "")
        {
            List<IncrementerError> errors = new List<IncrementerError>();

            // XPath list of nodes where to look for version information
            // these nodes will also be checked for existence and warning will be made
            // if no node was found
            List<string> nodesToChange = new List<string>()
            {
                "//AssemblyVersion",
                "//FileVersion",
                "//Version"
            };

            // XPath list of nodes to check for existence; if not existent or configured as 'false'
            // raise as warning as they could be unintentionally omitted or indicate an issue
            List<string> nodesWarnIfNotExistsOrFalse = new List<string>()
            {
                "//IncludeSourceRevisionInInformationalVersion",
                "//GenerateAssemblyInfo"
            };

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(fileName);

            if (xmlDoc == null || xmlDoc.DocumentElement == null)
                throw new IOException($"Unknown error or empty xml file {fileName}");

            foreach (string tag in nodesToChange)
            {
                XmlNodeList? xmlNodeList = xmlDoc.DocumentElement.SelectNodes(tag);
                if (xmlNodeList == null || xmlNodeList?.Count < 1)
                {
                    errors.Add(IncrementerError.Warning(($"Node <{tag}> not found")));
                    break;
                }

                if (xmlNodeList?.Count > 1)
                {
                    errors.Add(IncrementerError.Warning(
                        $"Node <{tag}> found {xmlNodeList?.Count} times"));
                    break;
                }

                string currentValue = xmlNodeList!.Item(0)!.InnerText ?? string.Empty;
                if (string.IsNullOrEmpty(currentValue))
                {
                    errors.Add(IncrementerError.Warning
                        ($"Tag <{tag}> present but value empty"));
                    break;
                }

                (bool success, string newVersion) = IncrementVersion(
                    currentValue,
                    positionToIncrement,
                    incrementBy,
                    setVersion
                    );

                if (!success)
                    errors.Add(IncrementerError.Error(
                        $"Error incrementing <{tag}>, value '{currentValue}'"));
                else
                {
                    xmlNodeList!.Item(0)!.InnerText = newVersion;
                    errors.Add(IncrementerError.Ok(
                        $"Set <{tag}> from '{currentValue}' to " +
                        $"'{newVersion}'"));
                }
            }

            foreach (string tag in nodesWarnIfNotExistsOrFalse)
            {
                XmlNodeList? xmlNodeList = xmlDoc.DocumentElement.SelectNodes(tag);
                if (xmlNodeList == null || xmlNodeList?.Count < 1)
                {
                    errors.Add(IncrementerError.Warning($"Node <{tag}> not found"));
                    break;
                }

                if (xmlNodeList?.Count > 1)
                {
                    errors.Add(IncrementerError.Warning(
                        $"Node <{tag}> found {xmlNodeList?.Count} times"));
                    break;
                }

                var tagValue = xmlNodeList!.Item(0)!.Value ?? string.Empty;
                if (string.IsNullOrEmpty(tagValue) ||
                    string.Equals(tagValue, "false", StringComparison.OrdinalIgnoreCase)
                    )
                {
                    errors.Add(IncrementerError.Warning($"Node <{tag}> is set to false"));
                }
            }

            return (errors, xmlDoc);
        }


        public static string GetOldStylePrefix(string tag) => $"[assembly: {tag}(\"";

        public static (List<IncrementerError>, List<string>) ProcessAssemblyFile(
            string fileName,
            int positionToIncrement,
            int incrementBy,
            string setVersion = ""
            )
        {
            const string postfix = "\")]";
            const string CONST_ASSEMBLY = "AssemblyVersion";
            const string CONST_ASSEMBLY_FILE = "AssemblyFileVersion";

            List<string> result = new List<string>();
            List<IncrementerError> errors = new List<IncrementerError>();

            bool foundAVersionInFile = false;
            bool foundFVersionInFile = false;

            List<string> fileLines = File.ReadLines(fileName).ToList();
            foreach (string line in fileLines)
            {
                string currentLine = line;
                (bool foundAVersion, string aVersion) = GetVersionFromAssemblyFile(CONST_ASSEMBLY, currentLine);
                (bool foundFVersion, string fVersion) = GetVersionFromAssemblyFile(CONST_ASSEMBLY_FILE, currentLine);

                if (foundAVersion)
                {
                    (bool aIncremented, string newAVersion) = IncrementVersion(
                        aVersion,
                        positionToIncrement,
                        incrementBy,
                        setVersion);
                    if (aIncremented)
                    {
                        errors.Add(IncrementerError.Ok($"Found {CONST_ASSEMBLY} old: {aVersion}, new {newAVersion}"));
                        foundAVersionInFile = true;
                        currentLine = GetOldStylePrefix(CONST_ASSEMBLY) + newAVersion + postfix;
                    }
                    else errors.Add(IncrementerError.Warning($"Unable to increment {CONST_ASSEMBLY} '{aVersion}'"));
                }

                if (foundFVersion)
                {
                    (bool fIncremented, string newFVersion) = IncrementVersion(
                        fVersion,
                        positionToIncrement,
                        incrementBy,
                        setVersion);
                    if (fIncremented)
                    {
                        errors.Add(IncrementerError.Ok($"Found {CONST_ASSEMBLY_FILE} old: {fVersion}, new {newFVersion}"));
                        foundFVersionInFile = true;
                        currentLine = GetOldStylePrefix(CONST_ASSEMBLY_FILE) + newFVersion + postfix;
                    }
                    else errors.Add(IncrementerError.Warning($"Unable to increment {CONST_ASSEMBLY_FILE} '{aVersion}'"));
                }

                result.Add(currentLine);
            }

            if (!foundAVersionInFile) errors.Add(IncrementerError.Warning($"{CONST_ASSEMBLY} not found in file"));
            if (!foundFVersionInFile) errors.Add(IncrementerError.Warning($"{CONST_ASSEMBLY_FILE} not found in file"));

            return (errors, result);
        }

        /// <summary>
        /// Finds 'tag' if present in the fileLine (in format expected in the 
        /// old-style AssemblyFileInfo). 
        /// Tag is normally either AssemblyVersion or AssemblyFileVersion
        /// </summary>
        /// <param name="tag">AssemblyVersion or AssemblyFileVersion</param>
        /// <param name="fileLine"></param>
        /// <returns>(bool success, string just_version_number)</returns>
        public static (bool, string) GetVersionFromAssemblyFile(string tag, string fileLine)
        {
            string prefix = GetOldStylePrefix(tag);
            const string postfix = "\")]";

            bool success = false;
            string resultV = string.Empty;

            success = fileLine.Replace("\t", "").Trim().StartsWith(prefix);
            if (success)
            {
                int endIndex = fileLine.IndexOf(postfix);
                if (endIndex > 0)
                {
                    int startIndex = fileLine.IndexOf(prefix) + prefix.Length;
                    if (endIndex > startIndex)
                    {
                        resultV = fileLine.Substring(startIndex, endIndex - startIndex).Trim();
                    }
                }
            }
            return (success, resultV);
        }
        #endregion

        #region Error list utilities

        public static int HasErrors(List<IncrementerError> errorList)
            => errorList.Where(e=>e.ErrorType == IncrementerError.ErrorTypes.Error).Count();

        public static int HasWarnings(List<IncrementerError> errorList)
            => errorList.Where(e => e.ErrorType == IncrementerError.ErrorTypes.Warning).Count();

        #endregion

    }
}
