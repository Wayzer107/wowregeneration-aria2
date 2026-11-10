using System;

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
}
