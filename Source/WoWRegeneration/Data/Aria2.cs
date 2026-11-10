using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace WoWRegeneration.Data
{
    /// <summary>
    ///     Finds aria2c and downloads single files with it.
    /// </summary>
    public static class Aria2
    {
        private static readonly string[] Names = {"aria2c.exe", "aria2c"};

        /// <summary>
        ///     Returns the aria2c to use, or logs how to provide one and returns null.
        /// </summary>
        public static string Resolve()
        {
            string aria2 = Find(DownloadSettings.Aria2Path);
            if (aria2 == null)
                Program.Log("aria2c was not found. Put aria2c.exe next to WoWRegeneration.exe, add it to PATH, " +
                            "or pass --aria2c <path>.", ConsoleColor.Red);
            return aria2;
        }

        /// <summary>
        ///     Returns the explicit path if given and present, else aria2c next to this program,
        ///     else the first aria2c on PATH. Returns null if none is found.
        /// </summary>
        public static string Find(string explicitPath)
        {
            if (!String.IsNullOrEmpty(explicitPath))
                return File.Exists(explicitPath) ? Path.GetFullPath(explicitPath) : null;

            string found = FindIn(AppContext.BaseDirectory);
            if (found != null)
                return found;

            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string dir in path.Split(Path.PathSeparator))
            {
                found = FindIn(dir.Trim().Trim('"'));
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        ///     Downloads url to target with DownloadSettings.Connections connections. An existing
        ///     file is resumed: with its .aria2 control file aria2c picks up every segment, and
        ///     without one (a partial file written from the start, as the old WebClient
        ///     downloader did) --continue appends from the file's current length.
        ///     Returns true when aria2c succeeded and the file exists.
        /// </summary>
        public static bool Download(string aria2, string url, string target)
        {
            string directory = Path.GetDirectoryName(target);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

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
            args.Append(' ').Append(Quote(url));

            var startInfo = new ProcessStartInfo(aria2, args.ToString())
                {
                    UseShellExecute = false
                };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();

                    // aria2c leaves its progress line without a newline; end it so the next
                    // message starts on its own line.
                    if (!Console.IsOutputRedirected && Console.CursorLeft > 0)
                        Console.WriteLine();

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

        private static string FindIn(string directory)
        {
            if (String.IsNullOrEmpty(directory))
                return null;
            foreach (string name in Names)
            {
                try
                {
                    string candidate = Path.Combine(directory, name);
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch (ArgumentException)
                {
                    // Malformed PATH entry.
                }
            }
            return null;
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
