using System;
using System.Collections.Generic;
using System.IO;
using WoWRegeneration.Repositories;

namespace WoWRegeneration.Data
{
    /// <summary>
    ///     Downloads the manifest's files one after another with aria2c, using several connections per file.
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
        public bool Start(string aria2)
        {
            Program.Log("Downloading " + Files.Count + " files with aria2c (" + DownloadSettings.Connections +
                        " connections per file)", ConsoleColor.Yellow);

            for (int i = 0; i < Files.Count; i++)
            {
                FileObject file = Files[i];
                Program.Log();
                Program.Log("[" + (i + 1) + "/" + Files.Count + "] " + file.Path, ConsoleColor.Yellow);

                if (!Aria2.Download(aria2, file.Url, Path.Combine(BasePath, file.Path)))
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
    }
}
