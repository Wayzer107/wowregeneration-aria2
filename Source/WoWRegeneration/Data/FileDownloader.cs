using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using WoWRegeneration.Repositories;

namespace WoWRegeneration.Data
{
    /// <summary>
    ///     Downloads files one after another with aria2c, using several connections per file.
    /// </summary>
    public class FileDownloader
    {
        public FileDownloader(WoWRepository repository, List<FileObject> files)
        {
            Files = files;
            BasePath = Path.GetFullPath(repository.GetDefaultDirectory());
        }

        private List<FileObject> Files { get; set; }
        private string BasePath { get; set; }

        /// <summary>
        ///     Downloads every file. Returns false if a download failed; files finished before
        ///     the failure stay recorded in the session, so the run can be resumed.
        /// </summary>
        public bool Start()
        {
            string aria2 = Aria2Locator.Find(DownloadSettings.Aria2Path);
            if (aria2 == null)
            {
                Program.Log("aria2c was not found. Put aria2c.exe next to WoWRegeneration.exe, add it to PATH, " +
                            "or pass --aria2c <path>.", ConsoleColor.Red);
                return false;
            }

            Program.Log("Downloading " + Files.Count + " files with aria2c (" + DownloadSettings.Connections +
                        " connections per file)", ConsoleColor.Yellow);

            for (int i = 0; i < Files.Count; i++)
            {
                FileObject file = Files[i];
                Program.Log();
                Program.Log("[" + (i + 1) + "/" + Files.Count + "] " + file.Path, ConsoleColor.Yellow);

                if (!DownloadFile(aria2, file))
                {
                    Program.Log("Download of " + file.Path + " failed. Run the program again to resume.",
                                ConsoleColor.Red);
                    return false;
                }

                WoWRegeneration.CurrentSession.CompletedFiles.Add(file.Path);
                WoWRegeneration.CurrentSession.SaveSession();
            }
            return true;
        }

        private bool DownloadFile(string aria2, FileObject file)
        {
            string target = Path.Combine(BasePath, file.Path);
            string directory = Path.GetDirectoryName(target);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // An existing file is resumed: with its .aria2 control file aria2c picks up every
            // segment, and without one (a partial file from the old WebClient downloader, which
            // writes from the start) --continue appends from the file's current length.
            int connections = DownloadSettings.Connections;
            var args = new StringBuilder();
            args.Append("--max-connection-per-server=").Append(connections);
            args.Append(" --split=").Append(connections);
            args.Append(" --min-split-size=1M");
            args.Append(" --continue=true");
            args.Append(" --allow-overwrite=true");
            args.Append(" --auto-file-renaming=false");
            args.Append(" --file-allocation=none");
            args.Append(" --max-tries=5 --retry-wait=3");
            args.Append(" --console-log-level=warn");
            args.Append(" --summary-interval=0");
            args.Append(" --download-result=hide");
            args.Append(" --dir=").Append(Quote(directory));
            args.Append(" --out=").Append(Quote(Path.GetFileName(target)));
            args.Append(' ').Append(Quote(file.Url));

            var startInfo = new ProcessStartInfo(aria2, args.ToString())
                {
                    UseShellExecute = false
                };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        Program.Log("aria2c exited with code " + process.ExitCode, ConsoleColor.Red);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log("Could not start aria2c: " + ex.Message, ConsoleColor.Red);
                return false;
            }

            return File.Exists(target);
        }

        /// <summary>
        ///     Quotes a command line argument. Trailing backslashes are removed so they cannot
        ///     escape the closing quote.
        /// </summary>
        private static string Quote(string value)
        {
            return "\"" + value.TrimEnd('\\', '/') + "\"";
        }
    }
}
