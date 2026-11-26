<div align="center">

<img src="src/DeskDot/Assets/app-icon.png" width="120" alt="DeskDot icon" />

# DeskDot

**A little space. A better day.**

A calm daily dashboard for Windows 10 and Windows 11 — focus timer, live weather, clipboard
history, quick notes, screen time, agenda and system stats, in one quiet window.

[![Build](https://github.com/Maheshyadav1318/DeskDot/actions/workflows/build.yml/badge.svg)](https://github.com/Maheshyadav1318/DeskDot/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-0a0a0c?style=flat)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat)](#requirements)

Designed &amp; Developed by **D.R.M.Y.** ✨

</div>

<p align="center">
  <img src="src/DeskDot/Assets/dashboard-hero.jpg" width="720" alt="A calm landscape — the feeling DeskDot is going for" />
</p>

---

## ⬇️ Install

Grab the latest build from **[Releases](https://github.com/Maheshyadav1318/DeskDot/releases)**.

| Asset | What it is |
| --- | --- |
| `DeskDot-2.0.0-setup.exe` | Per-user installer — **no admin prompt**, Start-menu entries, optional launch-at-sign-in and desktop shortcut |
| `DeskDot-win-x64.zip` | Portable x64 build, self-contained (no .NET install needed) — unzip and run `DeskDot.exe` |
| `DeskDot-win-arm64.zip` | Portable build for Windows on ARM |

Three clicks: run the installer → choose whether DeskDot starts with Windows → **Install**.
It lands in `%LOCALAPPDATA%\Programs\DeskDot` for your user only, with no driver and no
service. To remove it, use *Apps & features* like any other app; your settings and notes stay
in `%APPDATA%\DeskDot` unless you delete them.

> Binaries are produced by CI from this repository and are **not code-signed**, so SmartScreen
> may warn on first run — see [SECURITY.md](SECURITY.md#binary-provenance). Prefer building
> from source if you'd rather.

### Requirements

- Windows 10 1809 (build 17763) or newer, or Windows 11 — x64 or ARM64
- Nothing else: the portable and installed builds are self-contained
- Building from source needs the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## ✨ What it does

Eight small modules on one page. Each one can be switched off in **Setup → Modules**.

| | Module | What it shows |
| --- | --- | --- |
| ⏱️ | **Focus** | A 25-minute timer (15/20/25/30/45/50/60 to choose from) with Start, Pause and Reset, a thin progress ring and a quiet chime when it ends |
| ⛅ | **Weather** | Current conditions, today's high/low, humidity and wind for your city — live from Open-Meteo, or a clearly-labelled placeholder if you stay offline |
| 🖥️ | **System** | CPU, memory and battery, sampled every three seconds and **paused while the window is hidden** |
| 📊 | **Screen time** *(opt-in)* | Today's total, how it compares with yesterday, a Mon–Sun bar chart and your top apps |
| 🔔 | **Clock** | The time, the date and a greeting that fits the hour |
| 📋 | **Clipboard** *(opt-in)* | The last text you copied, how long ago, and a browser for the last 25 entries — memory only, masked when it looks like a secret |
| 📝 | **Notes** | One-line thoughts kept in a local JSON file, with the latest one pinned on the dashboard |
| 🗓️ | **Agenda** | Your agenda from a plain text file, split into what's still ahead and what's already passed |

Plus a **reminder** card that rotates through a few quiet lines, a **module manager** showing
all eight, and two global hotkeys. The page reflows when a module is switched off, so what is
left gets the room.

### The whole window

```
  DeskDot   ·   A little space. A better day.                       Modules  Setup  Hide
╭──────────────────────────────────────────────────────────────────────────────────────╮
│  A LITTLE FOCUS                       ╭───────────────╮   ╭────── THIS PC · LIVE ────╮
│                                       │  Your city    │   │ CPU        12%           │
│  25:00                                │  18°  Partly  │   │ MEMORY     46%           │
│  One thing at a time   [Start focus]  │  cloudy       │   │ BATTERY    100% Charging │
│  Good morning                         │  H 21° L 13°  │   │ No drivers · no admin    │
│                                       ╰───────────────╯   ╰──────────────────────────╯
│  ╭── SCREEN TIME ────────╮  ╭── A LITTLE REMINDER ──────────────────────────────────╮
│  │ 2h 34m   12% vs yest. │  │ "You don't have to do it all. Just the next right      │
│  │ M T W T F S S         │  │  thing."                          [Another] [Notes]     │
│  │ Chrome 1h 12m         │  ╰────────────────────────────────────────────────────────╯
│  ╰───────────────────────╯  ╭── CLIPBOARD ──╮  ╭── AGENDA ──────╮  ╭── NOTES ───────╮
│                             │ "Make room…"  │  │ 11:30 Design…  │  │ Groceries      │
│                             │ 2 min ago     │  │ 14:00 Focus    │  │ milk, oats     │
│                             ╰───────────────╯  ╰────────────────╯  ╰────────────────╯
│  YOUR MODULES · 8 little helpers            Toggle Ctrl+Alt+D  ·  Setup Ctrl+,          │
╰──────────────────────────────────────────────────────────────────────────────────────╯
```

---

## 🔒 Privacy, in plain language

- **Offline by default.** DeskDot works fully with no internet. The single exception is the
  weather card: DeskDot never guesses your location. The first request happens only after you
  pick a city in Setup (one geocoding lookup, once), and then it refreshes from
  **Open-Meteo** every 15 minutes — coordinates and units, nothing else. No account, no
  telemetry, no analytics, no update check.
- **Clipboard history and screen time are off until you switch them on**, and both are
  explained in Setup → Privacy before you do. Clipboard history never leaves memory: it is
  gone when DeskDot closes, and entries that look like passwords, tokens or card numbers are
  masked.
- **Screen time stays local.** DeskDot reads which app has focus to count minutes per app; it
  never records window titles, keystrokes, screenshots or URLs.
- **No elevation, ever.** No driver, no service, no admin prompt — the installer and the app
  both run as you.

Full detail, including exactly which URLs are contacted, is in [SECURITY.md](SECURITY.md).

---

## ⌨️ Hotkeys

| Action | Default | Change it |
| --- | --- | --- |
| Show / hide DeskDot | `Ctrl+Alt+D` | Setup → General |
| Open Setup | `Ctrl+,` | Setup → General |

Both can be set to a handful of sensible combinations. If a combination is already taken by
another app, DeskDot falls back to its default and says so in Setup.

### Command line

| Switch | Effect |
| --- | --- |
| `--tray` / `--minimised` | Start hidden in the tray (used by the "start with Windows" shortcut) |
| `--settings` | Open Setup instead of the dashboard |

---

## ⚙️ Setup

Everything lives in one small window, in four tabs:

- **General** — the two hotkeys, focus length, always-on-top, start with Windows, start minimised.
- **Modules** — one switch per module, so the page keeps only what you use.
- **Weather** — type a city, pick it from the list, choose °C or °F. Coordinates are cached,
  so the lookup happens once.
- **Privacy** — the clipboard and screen-time switches, each with a paragraph on exactly what
  is stored and where, plus the buttons to clear the history that exists.

### Files it keeps

| Path | Contents |
| --- | --- |
| `%APPDATA%\DeskDot\settings.json` | Your preferences, module choices and window position |
| `%APPDATA%\DeskDot\screentime.json` | Per-app screen-time totals, only if you enable screen time |
| `%APPDATA%\DeskDot\notes.json` | Notes |
| `%APPDATA%\DeskDot\agenda.txt` | Agenda — one `HH:mm Title` per line, edit it in Notepad ([sample](docs/agenda.sample.txt)) |
| `%APPDATA%\DeskDot\crash.log` | Written only if something goes wrong |

A sample of the settings file is in [docs/settings.sample.json](docs/settings.sample.json).

---

## 🛠️ Build from source

```powershell
git clone https://github.com/Maheshyadav1318/DeskDot.git
cd DeskDot

dotnet build DeskDot.sln -c Release        # compile
dotnet test  tests/DeskDot.Tests           # 166 tests — run these on Windows;
                                           # Core/ itself is portable
dotnet run   --project src/DeskDot         # launch the dashboard
```

One script does the whole release if you have [Inno Setup 6](https://jrsoftware.org/isinfo.php):

```powershell
pwsh scripts/build.ps1                      # publish x64 + arm64, zip, build the installer
pwsh scripts/build.ps1 -SkipInstaller       # just the portable zips
```

### Layout

```
src/DeskDot/
  Core/          pure, unit-tested logic (time parsing, weather codes, clipboard rules,
                 power formatting, screen-time aggregation, hotkey gestures)
  Models/        settings and dashboard view models
  Services/      Win32 + file I/O: hotkeys, tray, weather, clipboard, screen time, notes
  ViewModels/    DashboardViewModel — the single binding surface
  Views/         Setup, Notes and Clipboard windows
  Themes/        Light.xaml (palette) and DashboardControls.xaml (pill/toggle/meter styles)
  MainWindow.*   the dashboard
tests/DeskDot.Tests/   166 xunit tests over Core/Models/Services
installer/DeskDot.iss   Inno Setup script (per-user, no elevation)
scripts/build.ps1      one-command release build
```

`Core/` deliberately has no WPF or Win32 dependency, so most logic is testable without a
desktop; CI runs the suite on an Ubuntu runner inside a Windows-targeting project. See [docs/TESTING.md](docs/TESTING.md) for what is
covered and what needs a real desktop.

---

## 🗺️ Roadmap

[ROADMAP.md](docs/ROADMAP.md) has what's next — per-window screen time, a compact mini mode,
and a few refinements. v2.0 replaced the original Dynamic Island overlay with this dashboard;
the island is not coming back, but the ideas it carried (glanceable focus, quiet stats) all
live here.

## 🤝 Contributing

Bug reports and pull requests are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md) and the
[issue templates](.github/ISSUE_TEMPLATE). Please don't report security issues in public;
use [SECURITY.md](SECURITY.md).

## 📄 License

[MIT](LICENSE) © D.R.M.Y.

<div align="center"><sub>Made with a little space in mind.</sub></div>
