WoW Regeneration (aria2)
========================

Downloads a complete, playable World of Warcraft 4.3.4 (build 15595) client from Blizzard's CDN:
the MPQ game data plus the Windows client files (`Wow.exe`, `Wow-64.exe`, their libraries and `WoW.mfil`).

This is a modified version of [WoWRegeneration](https://github.com/The-Cataclysm-Preservation-Project/wowregeneration)
that also does the job of [ClientDownloader](https://github.com/The-Cataclysm-Preservation-Project/ClientDownloader),
so one tool produces the whole client. Downloads go through [aria2](https://aria2.github.io/) with
16 connections per file, and the project targets .NET 11.

## Changes from the original

- Each file is downloaded by `aria2c` with 16 connections (`--split=16`), instead of a single
  `WebClient` connection.
- Interrupted downloads resume. That includes a half-finished file left behind by the original
  WebClient version, which `aria2c --continue` appends to instead of starting over.
- A file is only recorded as complete when `aria2c` reports success. If a download fails, the
  program stops, and running it again resumes the session.
- Ported from .NET Framework 4.0 to .NET 11 (SDK-style project, `HttpClient` for the manifest).
- After the MPQs, the Windows client files are downloaded too (when the Win OS is chosen):
  11 files from Blizzard's repair server, `Wow-64.exe`, `Battle.net-64.dll` and `MovieProxy.exe`
  from the 64-bit installer zip, and a read-only `WoW.mfil`. Each is checked against its known MD5.
  Files that are already correct are skipped. A file with the wrong MD5 is kept as `name_BACKUP`
  and downloaded again.

## Requirements

- [.NET 11 SDK](https://dotnet.microsoft.com/download/dotnet/11.0) to build, or the .NET 11 runtime to run a build.
- `aria2c` next to `WoWRegeneration.exe` or on your `PATH`
  (Windows: `scoop install aria2` or `winget install aria2.aria2`; Linux: your package manager's `aria2` package).

## Build

```
cd Source
dotnet build -c Release
```

The program is written to `Source/WoWRegeneration/bin/Release/net11.0/`.

## Usage

Run the program from the folder you want the client downloaded into, and answer the prompts for
locale and OS. The client goes into a `WoW-15595` folder, and progress is kept in `session.xml`, so
you can stop at any point and pick up where you left off by running it again. When it finishes,
`WoW-15595` holds the complete client, ready to run with `Wow.exe` or `Wow-64.exe`.

```
WoWRegeneration [--connections N] [--aria2c PATH]
```

| Option | Default | Meaning |
|---|---|---|
| `--connections N`, `-x N` | 16 | Connections per file, 1 to 16 (aria2c's maximum). |
| `--aria2c PATH` | search | Path to `aria2c`. Without it, the program looks next to itself, then on `PATH`. |

## Credits

Original WoWRegeneration by valcriss, with later work by Yaki Khadafi, Dramacydal and funjoker
(The Cataclysm Preservation Project). The original repository has no license file.

The client file list, MD5s, download locations and `WoW.mfil` contents come from ClientDownloader
by The Cataclysm Preservation Project. The code that uses them here is a new implementation.
