using System;
using System.IO;

namespace WoWRegeneration.Data
{
    /// <summary>
    ///     Download options, set from the command line.
    /// </summary>
    public static class DownloadSettings
    {
        public const int DefaultConnections = 16;

        static DownloadSettings()
        {
            Connections = DefaultConnections;
        }

        /// <summary>Connections aria2c opens per file (1-16).</summary>
        public static int Connections { get; set; }

        /// <summary>Explicit path to aria2c, or null to search for it.</summary>
        public static string Aria2Path { get; set; }

        /// <summary>
        ///     Reads --connections/-x N and --aria2c PATH. Returns false and logs the problem
        ///     if an argument is invalid.
        /// </summary>
        public static bool Parse(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "--connections" || arg == "-x")
                {
                    int value;
                    if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out value) || value < 1 || value > 16)
                    {
                        Program.Log(arg + " needs a number from 1 to 16 (aria2c's limit).", ConsoleColor.Red);
                        return false;
                    }
                    Connections = value;
                    i++;
                }
                else if (arg == "--aria2c")
                {
                    if (i + 1 >= args.Length)
                    {
                        Program.Log("--aria2c needs the path to aria2c.", ConsoleColor.Red);
                        return false;
                    }
                    Aria2Path = args[i + 1];
                    i++;
                }
                else
                {
                    Program.Log("Unknown argument: " + arg, ConsoleColor.Red);
                    Program.Log("Usage: WoWRegeneration [--connections N] [--aria2c PATH]");
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    ///     Finds the aria2c executable.
    /// </summary>
    public static class Aria2Locator
    {
        private static readonly string[] Names = {"aria2c.exe", "aria2c"};

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
    }
}
