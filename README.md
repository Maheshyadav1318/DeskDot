<div align="center">

<img src="src/DeskDot/Assets/app-icon.png" width="120" alt="DeskDot icon" />

# DeskDot

**A Dynamic Island-inspired productivity overlay for Windows 10 and Windows 11.**

Your clock, next agenda item, media controls, a focus timer and live system stats —
in one small island that sits above your desktop.

[![Build](https://github.com/Maheshyadav1318/DeskDot/actions/workflows/build.yml/badge.svg)](https://github.com/Maheshyadav1318/DeskDot/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-0a0a0c?style=flat)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4?style=flat)](#requirements)

Designed &amp; Developed by **D.R.M.Y.** ✨

</div>

```
  ● 07:56  ·  Wed, Nov 25            CPU 14%  RAM 11G          DeskDot
╭──────────────────────────────────────────────────────────────────────╮
│  Have a wonderful day! ✨      │  ▣  Amber Stillness                  │
│  07:56  Wed, Nov 25            │     Ólafur & Kiasmos                 │
│  ╭──────────────────────────╮  │     ▬▬▬▬▬▬▬▬▭▭▭▭▭▭▭▭      ⏮ ⏸ ⏭    │
│  │ Next · 14:00             │  │  ╭────────────╮ ╭────────────╮      │
│  │ Test sleep/resume & …    │  │  │ FOCUS 25:00│ │ CPU   14%  │      │
│  ╰──────────────────────────╯  │  │ Start Reset│ │ RAM  11G   │      │
│  [DeskDot] [Focus] [Settings]  │  ╰────────────╯ ╰────────────╯      │
╰──────────────────────────────────────────────────────────────────────╯
```

---

## ⬇️ Install

Grab the latest build from **[Releases](https://github.com/Maheshyadav1318/DeskDot/releases)**.

| Asset | What it is |
| --- | --- |
| `DeskDot-<version>-setup.exe` | Per-user installer — **no admin prompt**, Start-menu entry, optional launch-at-login |
| `DeskDot-win-x64.zip` | Portable x64 build, self-contained (no .NET install needed) |
| `DeskDot-win-arm64.zip` | Portable build for Windows on ARM |

> Binaries are produced by CI from this repository and are **not code-signed**, so SmartScreen
> may warn on first run — see [SECURITY.md](SECURITY.md#binary-provenance). Prefer building
> from source if you'd rather.

### Requirements

- Windows 10 1809 (build 17763) or newer, or Windows 11 — x64 or ARM64
- Nothing else: the portable and installed builds are self-contained
- Building from source needs the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## ✨ What it does

| | Feature |
| --- | --- |
| 🏝️ | **One island, two states** — a compact pill (clock, date, CPU/RAM) that morphs into the full panel and back. |
| 📊 | **Real system stats** — CPU from a `GetSystemTimes` delta, memory from `GlobalMemoryStatusEx`. No services, no NuGet packages. |
| ⏱️ | **Focus timer** — Start / Pause / Reset countdown with a tray notification when the session ends. |
| 🎵 | **Media card** — album art, transport buttons and click-to-seek on the progress bar. |
| 📅 | **Next up** — reads your own `agenda.txt` and always shows the next item of the day. |
| ⌨️ | **Global hotkey** — `Alt+Space` toggles the island from anywhere; rebindable, with automatic fallback if another app owns the gesture. |
| 🖱️ | **Drag to place** — put the island anywhere; the position is remembered. |
| 🧰 | **Lives in the tray** — left-click toggles, right-click for Settings / Exit. Single instance. |
| 🔋 | **Resilient** — hotkey re-registers after sleep/resume and unlock; unreadable settings fall back to defaults; failures never crash the overlay. |

### Keyboard & mouse

| Action | Gesture |
| --- | --- |
| Expand / collapse | `Alt+Space` or click the island |
| Collapse | `Esc` or click the desktop |
| Move the island | Drag the island |
| Open Setup | Tray → *Settings…* or `DeskDot.exe --settings` |
| Play / pause, skip | Media buttons in the expanded panel |
| Seek | Click anywhere on the progress bar |

---

## 🚀 Build from source

```powershell
git clone https://github.com/Maheshyadav1318/DeskDot.git
cd DeskDot

# run it
dotnet run --project src/DeskDot/DeskDot.csproj

# tests
dotnet test tests/DeskDot.Tests/DeskDot.Tests.csproj

# release build → self-contained publish → installer
pwsh ./scripts/build.ps1 -Configuration Release -Publish -Installer
```

Visual Studio users can open `DeskDot.sln`, choose `Release | x64` and press F5.

### Cutting a release

```powershell
git tag v1.0.0
git push origin v1.0.0
```

CI then builds `win-x64` + `win-arm64`, compiles the Inno Setup installer and attaches all three
assets to the GitHub Release. Every push and pull request runs the unit tests first and builds
with warnings-as-errors.

---

## ⚙️ Configuration

Settings are edited in **DeskDot Setup** and stored in `%APPDATA%\DeskDot\settings.json`
(see [`docs/settings.sample.json`](docs/settings.sample.json)):

| Key | Default | Meaning |
| --- | --- | --- |
| `ToggleHotkey` | `Alt+Space` | Global expand/collapse gesture |
| `CollapseOnFocusLoss` | `true` | Collapse when the island loses focus |
| `AlwaysOnTop` | `true` | Keep the island above other windows |
| `StartWithWindows` | `false` | HKCU Run entry (per-user, no elevation) |
| `StatsIntervalMs` | `4000` | CPU/RAM sample interval |
| `FocusMinutes` | `25` | Focus session length |
| `DisplayName` | `""` | Optional name used in the greeting |
| `IslandLeft` / `IslandTop` | `null` | Remembered position; `null` centres the island |

### Your own agenda

Create `%APPDATA%\DeskDot\agenda.txt` — one `HH:mm Title` entry per line
(see [`docs/agenda.sample.txt`](docs/agenda.sample.txt)). Without it, a short demo agenda is used.

```text
# %APPDATA%\DeskDot\agenda.txt
09:30 Stand-up & inbox triage
11:00 Deep work block
14:00 Test sleep/resume & hotkey re-registration
18:00 Wrap up
```

Crashes are appended to `%APPDATA%\DeskDot\crash.log`.

---

## 📁 Repository layout

```text
DeskDot.sln                      Solution: app + tests (Release | x64 / arm64)
src/DeskDot/
  Core/                          Pure logic — agenda planning, hotkey parsing, focus clock,
                                 formatting, greetings              (unit tested)
  Models/                        Records: settings, system snapshot, agenda item, track
  Services/                      Win32/WPF services: stats, media, focus timer, hotkey,
                                 tray, power notifications, agenda, settings, startup
  ViewModels/                    Bindable island state
  Views/                         "DeskDot Setup" dialog
  Themes/                        Palette (Dark.xaml) and control styles (Controls.xaml)
  Assets/                        App icon (png + multi-size ico) and album art
  MainWindow.xaml(.cs)           The island: morph, drag, tray and hotkey wiring
  App.xaml(.cs)                  Entry point: single instance, crash log
tests/DeskDot.Tests/             xUnit suite over Core + file-based services (runs on Linux)
installer/DeskDot.iss            Inno Setup script (per-user install)
scripts/build.ps1                Build / publish / installer helper
docs/                            ROADMAP.md, TESTING.md (manual QA), sample config
.github/                         CI workflow, issue/PR templates, Dependabot
```

---

## ✅ Status

| Area | State |
| --- | --- |
| Island window, morph, drag, hotkey, tray, single instance | ✅ Done |
| Live CPU/RAM, focus timer, agenda card, settings | ✅ Done |
| Unit tests (89 cases) gating CI, warnings-as-errors | ✅ Done |
| Installer + x64/ARM64 builds, release attachments | ✅ Verified in CI |
| Media card | 🟡 Demo playlist; system-player mirroring is the v1.1 goal |
| Code signing, auto-update | ⛔ Not yet |

See [docs/ROADMAP.md](docs/ROADMAP.md) for what's next, and
[docs/TESTING.md](docs/TESTING.md) for the manual QA checklist used before a release.

---

## 🤝 Contributing

Issues and pull requests are welcome — please read [CONTRIBUTING.md](CONTRIBUTING.md) first.
New logic belongs in `src/DeskDot/Core` **with tests**; everything else is verified by hand
against `docs/TESTING.md`. Security problems go through [SECURITY.md](SECURITY.md), not the
public issue tracker.

## 📄 License

[MIT](LICENSE) © D.R.M.Y.

<div align="center"><sub>Made with care for people who like their desktop a little tidier. ✨</sub></div>
