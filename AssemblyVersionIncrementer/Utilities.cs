using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace AssemblyVersionIncrementer
{
    public static class Utilities
    {
        public static string GetNowString()
            => DateTime.Now.ToString("yyyy_MM_dd_HHmmss");

        public static string GetCurrentFolder() => 
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

        /// <summary>
        /// Returns suggested backup filename for a given fileName
        /// This is based on current date/time and .bak as extension
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetBackupName(string fileName, string backupExtension= ".bak")
        {
            string path = Path.GetDirectoryName(fileName) ?? string.Empty;
            string bareFileName = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            string newName = bareFileName + $"-{GetNowString()}{extension}{backupExtension}";
            return Path.Combine(path, newName);
        }

        /// <summary>
        /// Saves a backup copy of the file using special backup name 
        /// a.txt => a-2024_07_04_173256.txt.bak
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>backup file's file name</returns>
        public static string BackupFile(string fileName, string backupExtension=".bak")
        {
            var backupFilename = GetBackupName(fileName, backupExtension);
            File.Copy(fileName, backupFilename);
            return backupFilename;
        }

        /// <summary>
        /// Gets hex hash of file checksum based on MD5
        /// https://stackoverflow.com/questions/10520048/calculate-md5-checksum-for-a-file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
