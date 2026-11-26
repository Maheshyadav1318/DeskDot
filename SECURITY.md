# Security policy

## Supported versions

DeskDot is a local desktop dashboard that stores its state under `%APPDATA%\DeskDot`. Only
the latest release is supported.

| Version | Supported |
| --- | --- |
| 2.0.x | ✅ |
| 1.0.x | ❌ (no longer maintained) |

## Reporting a vulnerability

Please **do not** open a public issue. Report privately using GitHub's
[Report a vulnerability](https://github.com/Maheshyadav1318/DeskDot/security/advisories/new)
form, or email the maintainer listed on the repository profile.

Please include reproduction steps, the affected version, and your Windows build. You can
expect an initial response within about a week.

## What DeskDot does and does not do

Helping you reason about risk:

- **One network destination, and only for weather.** v2.0 asked for a live weather card, so
  the app talks to **Open-Meteo** (open-meteo.com), a free service that needs no API key and
  no account:
  - `https://geocoding-api.open-meteo.com/v1/search` — once, when you type a city into Setup,
    to turn that name into coordinates.
  - `https://api.open-meteo.com/v1/forecast` — every 15 minutes, with only the coordinates
    and the unit system, to fetch the current temperature and today's high/low.

  Both requests are plain HTTPS GETs. Nothing identifying is sent: no account, no device id,
  no city name after the initial lookup, no app version. Turn the Weather module off in Setup
  → Modules and the app makes no requests at all. There is still **no telemetry, no
  analytics, no crash reporting and no update check**.
- **No elevation.** It ships with `requestedExecutionLevel level="asInvoker"`; it never asks
  for administrator rights.
- **Local files only.** Reads and writes `%APPDATA%\DeskDot\settings.json`,
  `%APPDATA%\DeskDot\agenda.txt`, `%APPDATA%\DeskDot\notes.json` and
  `%APPDATA%\DeskDot\crash.log` and, when screen time is enabled,
  `%APPDATA%\DeskDot\screentime.json`. It opens no other paths. Clipboard history is
  **memory only** and is gone when DeskDot exits.
- **Clipboard history is opt-in and never leaves the machine.** It is off by default, keeps at
  most 25 text entries in RAM, masks entries that look like secrets (passwords, keys, tokens,
  card numbers) and offers Clear. Images and files are ignored.
- **Screen time is opt-in.** It reads the *title of the foreground window* every few seconds
  to attribute time to an app name, keeps a rolling week of per-app totals in
  `%APPDATA%\DeskDot\screentime.json`, and records no window titles, no keystrokes, no
  screenshots and no URLs.
- **Agenda is a local text file.** `%APPDATA%\DeskDot\agenda.txt` — one `HH:mm Title` per
  line. DeskDot only reads it; edit it in any text editor.
- **One registry key.** "Start with Windows" writes the `DeskDot` value under
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (per-user, no HKLM).
- **Two global hotkeys** (`Ctrl+Alt+D` to show/hide, `Ctrl+OemComma` for Setup by default) and
  a tray icon, both owned by the current user session. Both are configurable in Setup.

## Binary provenance

Release binaries are built by GitHub Actions from this repository
(`.github/workflows/build.yml`) and are attached to GitHub Releases. The build is
reproducible from source with the .NET 8 SDK. Binaries are currently **unsigned**, so
SmartScreen may warn on first run; verify the download against the release page and, if you
prefer, build from source instead.

## Dependencies

The application itself has **no third-party runtime dependencies** — only the .NET 8 BCL,
WPF and WinForms (`NotifyIcon`). It talks to the public Open-Meteo HTTP API using
`HttpClient` from the BCL; no SDK or package is bundled for it. The test project uses xunit
and Microsoft.NET.Test.Sdk at build time only. Dependabot is configured to keep the GitHub
Actions used by CI current.
