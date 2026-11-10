using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WoWRegeneration.Data
{
    /// <summary>
    ///     Downloads the Windows client files the MPQ manifest does not include (Wow.exe, its
    ///     libraries, the 64-bit client) and writes WoW.mfil, so the download folder is a
    ///     playable 4.3.4 client. Every file is checked against its known MD5. This replaces
    ///     the separate ClientDownloader tool from The Cataclysm Preservation Project.
    /// </summary>
    public static class ClientFiles
    {
        private const string RepairServer = "http://dist.blizzard.com.edgesuite.net/repair/wow/";

        private const string X64ZipName = "WoWLive-64-Win-15595.zip";
        private const string X64ZipUrl =
            "http://eu.media.battle.net.edgesuite.net/downloads/wow-installers/live/" + X64ZipName;

        private const string MfilName = "WoW.mfil";
        private const string MfilHash = "F8E7D7BA6CDE053B1A9F85BD36980A72";

        // Written with explicit CRLF line endings: the known hash is of the Windows file.
        private const string MfilContent =
            "version=2\r\n" +
            "server=akamai\r\n" +
            "\tlocation=http://dist.blizzard.com.edgesuite.net/wow-pod-retail/EU/15050.direct/\r\n" +
            "manifest_partial=wow-15595-0C3502F50D17376754B9E9CB0109F4C5.mfil\r\n";

        /// <summary>File name to MD5. Downloaded from the repair server, which stores files by MD5.</summary>
        private static readonly Dictionary<string, string> RepairFiles = new Dictionary<string, string>
            {
                {"BackgroundDownloader.exe", "E1FC69A72E4E23A96DBD535B372974A8"},
                {"Battle.net.dll", "24433A51A32335A39D2AF8CB55C467D3"},
                {"Blizzard Updater.exe", "82EF43D5F8D1B1C87C3505ECD241FFF6"},
                {"dbghelp.dll", "4003E34416EBD25E4C115D49DC15E1A7"},
                {"divxdecoder.dll", "57E72CAE12091DAFA29A8E4DB8B4F1D1"},
                {"Launcher.exe", "C7C7121E1DD819088403F514FEBD06BA"},
                {"Microsoft.VC80.CRT.manifest", "D34B3DA03C59F38A510EAA8CCC151EC7"},
                {"msvcr80.dll", "1169436EE42F860C7DB37A4692B38F0E"},
                {"Wow.exe", "DE5A2E274F2D3F2B89A2E6EC9CD8FD2A"},
                {"WowError.exe", "78766BBBFC6F9E5DA5D930CB11F0A1E1"},
                {"Repair.exe", "E198F00FE056B24ED58B36E1C6A048F4"}
            };

        /// <summary>File name to MD5. Extracted from the 64-bit installer zip.</summary>
        private static readonly Dictionary<string, string> X64Files = new Dictionary<string, string>
            {
                {"Wow-64.exe", "5ACD2205377352083D2D98B89F48B602"},
                {"Battle.net-64.dll", "5CA22973EDF3D10F9C69297A1EB28058"},
                {"MovieProxy.exe", "37EC741FCBDEEBD01F90D9877D872EA1"}
            };

        /// <summary>
        ///     Makes sure every client file is present in basePath with the right MD5. Files that
        ///     are already correct are skipped; a file with the wrong MD5 is kept as name_BACKUP
        ///     and downloaded again. Returns false if a file could not be downloaded or verified.
        /// </summary>
        public static bool Download(string aria2, string basePath)
        {
            Program.Log();
            Program.Log("Checking client files (Wow.exe and libraries)", ConsoleColor.Yellow);

            foreach (KeyValuePair<string, string> file in RepairFiles)
            {
                string path = Path.Combine(basePath, file.Key);
                if (IsPresent(path, file.Value))
                    continue;

                string md5 = file.Value;
                string url = RepairServer + md5[0] + "/" + md5[1] + "/" + md5;
                Program.Log("Downloading " + file.Key, ConsoleColor.Yellow);
                if (!Aria2.Download(aria2, url, path) || !Verify(path, md5))
                    return false;
            }

            List<string> missingX64 = X64Files.Keys.Where(name => !IsPresent(Path.Combine(basePath, name), X64Files[name]))
                                              .ToList();
            if (missingX64.Count > 0 && !DownloadX64(aria2, basePath, missingX64))
                return false;

            if (!IsPresent(Path.Combine(basePath, MfilName), MfilHash))
            {
                string path = Path.Combine(basePath, MfilName);
                Program.Log("Writing " + MfilName, ConsoleColor.Yellow);
                File.WriteAllText(path, MfilContent, new UTF8Encoding(false));
                if (!Verify(path, MfilHash))
                    return false;
                // The client rewrites WoW.mfil unless it is read-only.
                File.SetAttributes(path, FileAttributes.ReadOnly);
            }

            Program.Log("Client files are complete", ConsoleColor.Green);
            return true;
        }

        private static bool DownloadX64(string aria2, string basePath, List<string> names)
        {
            string zipPath = Path.Combine(basePath, X64ZipName);
            Program.Log("Downloading " + X64ZipName, ConsoleColor.Yellow);
            if (!Aria2.Download(aria2, X64ZipUrl, zipPath))
                return false;

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    foreach (string name in names)
                    {
                        ZipArchiveEntry entry = archive.GetEntry(name);
                        if (entry == null)
                        {
                            Program.Log(name + " is missing from " + X64ZipName, ConsoleColor.Red);
                            return false;
                        }

                        string path = Path.Combine(basePath, name);
                        Program.Log("Extracting " + name, ConsoleColor.Yellow);
                        entry.ExtractToFile(path, true);
                        if (!Verify(path, X64Files[name]))
                            return false;
                    }
                }
            }
            catch (InvalidDataException ex)
            {
                Program.Log(X64ZipName + " is damaged: " + ex.Message, ConsoleColor.Red);
                File.Delete(zipPath);
                return false;
            }

            File.Delete(zipPath);
            return true;
        }

        /// <summary>
        ///     True if path exists with the expected MD5. A file with a different MD5 is renamed
        ///     to name_BACKUP.ext (replacing an older backup) so it can be downloaded again.
        /// </summary>
        private static bool IsPresent(string path, string md5)
        {
            if (!File.Exists(path))
                return false;

            if (Md5Of(path) == md5)
            {
                Program.Log(Path.GetFileName(path) + " is already present, skipping", ConsoleColor.DarkGray);
                return true;
            }

            string backup = Path.Combine(Path.GetDirectoryName(path),
                                         Path.GetFileNameWithoutExtension(path) + "_BACKUP" + Path.GetExtension(path));
            File.SetAttributes(path, FileAttributes.Normal);
            File.Move(path, backup, true);

            // A leftover aria2 control file would describe the file just moved away.
            if (File.Exists(path + ".aria2"))
                File.Delete(path + ".aria2");

            Program.Log(Path.GetFileName(path) + " does not match its known MD5; kept it as " +
                        Path.GetFileName(backup), ConsoleColor.DarkYellow);
            return false;
        }

        private static bool Verify(string path, string md5)
        {
            string actual = Md5Of(path);
            if (actual == md5)
                return true;
            Program.Log(Path.GetFileName(path) + " has MD5 " + actual + ", expected " + md5, ConsoleColor.Red);
            return false;
        }

        private static string Md5Of(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                return Convert.ToHexString(MD5.HashData(stream));
            }
        }
    }
}
