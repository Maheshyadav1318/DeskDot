# DeskDot

DeskDot is a modern **Dynamic Island-inspired productivity overlay for Windows 10 and Windows 11**. It puts your clock, next agenda item, media controls, a focus timer and live system stats into one small island that hangs above your desktop — expand it when you need it, collapse it when you don't.

Designed &amp; Developed by **D.R.M.Y.** ✨

```
   ● 07:56  ·  Wed, Nov 25            CPU 14%  RAM 11G      DeskDot     ← collapsed
┌──────────────────────────────────────────────────────────────────────┐
│  Have a wonderful day! ✨      │  ▣ Amber Stillness                   │
│  07:56  Wed, Nov 25            │    Ólafur & Kiasmos                  │
│  ┌──────────────────────────┐  │    ▬▬▬▬▬▬▬▬▬▭▭▭▭▭▭▭▭        ⏮ ⏸ ⏭  │
│  │ Next · 14:00             │  │  ┌───────────┐ ┌───────────┐         │
│  │ Test sleep/resume & …    │  │  │ FOCUS 25:00│ │ CPU  14%  │         │
│  └──────────────────────────┘  │  │ [Start]   │ │ RAM  11G  │         │
│  [DeskDot] [Focus] [Settings]  │  └───────────┘ └───────────┘         │
└──────────────────────────────────────────────────────────────────────┘
```

## Download / install

Grab the latest installer from **[Releases](https://github.com/Maheshyadav1318/DeskDot/releases)**:

> **Status: v1.0.0 source is complete; binaries are produced by CI.** No release has been
> published yet. Push a `v1.0.0` tag (see *Build from source*) and GitHub Actions attaches
> the installer and both portable zips to the release automatically.

| File | Notes |
| --- | --- |
| `DeskDot-<version>-setup.exe` | Inno Setup installer, per-user, no admin prompt required |
| `DeskDot-win-x64.zip` | Portable, self-contained — no .NET install needed |
| `DeskDot-win-arm64.zip` | Portable build for Windows on ARM |

Requires **Windows 10 1809 (build 17763) or newer**. First run may show a SmartScreen prompt because the binaries are not code-signed yet.

## Status

| Area | State |
| --- | --- |
| WPF overlay, morph, drag, hotkey, tray, single instance | Done |
| Live CPU / memory, focus timer, agenda card | Done |
| Media card | Demo playlist (system-player mirroring scheduled for v1.1) |
| Installer + CI (x64, arm64, release attachments) | Done, needs a Windows run to produce binaries |
| Code signing / auto-update | Not yet |

## What it does

- **One island, two states** — a compact pill (clock, date, CPU/RAM) that morphs into the full panel and back.
- **Live system stats** — CPU load from a `GetSystemTimes` delta and memory from `GlobalMemoryStatusEx`; no NuGet dependencies, no background service.
- **Focus timer** — Pomodoro-style countdown in the island, with a tray notification when the session finishes.
- **Media card** — album art, track, transport buttons and click-to-seek on the progress bar (bundled demo playlist in v1.0; system-player mirroring lands in v1.1).
- **Next up** — reads `%APPDATA%\DeskDot\agenda.txt` (`HH:mm Title` per line) and shows the next item; ships with a demo agenda.
- **Global hotkey** — `Alt+Space` toggles the island from anywhere; changeable in Settings.
- **Drag to move** — position is remembered across restarts.
- **Lives in the tray** — left-click toggles, right-click gives Settings / Exit; single-instance.
- **Sleep/resume aware** — re-registers the hotkey and restarts timers after resume or unlock.

### Keyboard & mouse

| Action | Gesture |
| --- | --- |
| Expand / collapse | `Alt+Space`, or click the island |
| Collapse | `Esc`, or click the desktop |
| Open Setup | Tray → *Settings…*, or `DeskDot.exe --settings` |
| Move the island | Drag the island chrome |

## Build from source

**Prerequisites:** Windows 10/11, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), optional Visual Studio 2022 (17.9+) and [Inno Setup 6](https://jrsoftware.org/isinfo.php).

```powershell
git clone https://github.com/Maheshyadav1318/DeskDot.git
cd DeskDot

# Debug build + run
dotnet run --project src/DeskDot/DeskDot.csproj

# Release build, self-contained publish, and installer
pwsh ./scripts/build.ps1 -Configuration Release -Publish -Installer
```

Or open `DeskDot.sln` in Visual Studio, pick `Release | x64`, and press F5.

CI (`.github/workflows/build.yml`) builds `win-x64` and `win-arm64` on every push to `main`, and attaches an installer to GitHub Releases when you push a `v*` tag:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

## Repository layout

```
DeskDot.sln                 Solution (Release | x64 / arm64)
src/DeskDot/                WPF application (.NET 8, no NuGet packages)
  App.xaml(.cs)             Entry point, single instance, crash log
  MainWindow.xaml(.cs)      The island: pill ↔ panel morph, drag, hotkey, tray wiring
  ViewModels/               Bindable island state
  Services/                 Stats, media, focus timer, hotkey, tray, power, agenda, settings
  Themes/                   Palette and control styles
  Views/SettingsWindow      "DeskDot Setup" dialog
  Assets/                   Icon (png + ico) and album art
installer/DeskDot.iss       Inno Setup script
scripts/build.ps1           Build / publish / installer helper
docs/demo/                  Browser design reference (static HTML/CSS/JS)
docs/ROADMAP.md             What's shipped and what's next
```

## Configuration

Settings live in `%APPDATA%\DeskDot\settings.json` and can be edited from the UI:

```json
{
  "ToggleHotkey": "Alt+Space",
  "CollapseOnFocusLoss": true,
  "AlwaysOnTop": true,
  "StartWithWindows": false,
  "StatsIntervalMs": 4000,
  "FocusMinutes": 25,
  "DisplayName": ""
}
```

Crashes are appended to `%APPDATA%\DeskDot\crash.log`.

## Design reference

`docs/demo/index.html` is a static browser recreation of the island used to settle the visual design before porting to WPF. Open it directly, or serve it:

```bash
python -m http.server 8080 --directory docs/demo
```

It is a mock — the real application is the WPF build in `src/DeskDot`.

## Roadmap

See [docs/ROADMAP.md](docs/ROADMAP.md). Next up: System Media Transport Controls, album art from the live session, per-monitor placement and an auto-collapse timer.

## License

MIT — see [LICENSE](LICENSE).
