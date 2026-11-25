# Security policy

## Supported versions

DeskDot is a local desktop overlay that stores its state under `%APPDATA%\DeskDot`. Only the
latest release is supported.

| Version | Supported |
| --- | --- |
| 1.0.x | ✅ |
| < 1.0 | ❌ |

## Reporting a vulnerability

Please **do not** open a public issue. Report privately using GitHub's
[Report a vulnerability](https://github.com/Maheshyadav1318/DeskDot/security/advisories/new)
form, or email the maintainer listed on the repository profile.

Please include reproduction steps, the affected version, and your Windows build. You can
expect an initial response within about a week.

## What DeskDot does and does not do

Helping you reason about risk:

- **No network access.** The app makes no HTTP requests and has no telemetry, analytics or
  update-check traffic. There is nothing to intercept outbound.
- **No elevation.** It ships with `requestedExecutionLevel level="asInvoker"`; it never asks
  for administrator rights.
- **Local files only.** Reads and writes `%APPDATA%\DeskDot\settings.json`,
  `%APPDATA%\DeskDot\agenda.txt` and `%APPDATA%\DeskDot\crash.log`. It opens no other paths.
- **One registry key.** "Start with Windows" writes the `DeskDot` value under
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (per-user, no HKLM).
- **One global hotkey** (`Alt+Space` by default) and a tray icon, both owned by the current
  user session.
- **Media card is local simulation** in v1.0: it plays a bundled demo playlist and does not
  read other applications' media sessions yet.

## Binary provenance

Release binaries are built by GitHub Actions from this repository
(`.github/workflows/build.yml`) and are attached to GitHub Releases. The build is
reproducible from source with the .NET 8 SDK. Binaries are currently **unsigned**, so
SmartScreen may warn on first run; verify the download against the release page and, if you
prefer, build from source instead.

## Dependencies

The application itself has **no third-party runtime dependencies** — only the .NET 8 BCL,
WPF and WinForms (`NotifyIcon`). The test project uses xunit and Microsoft.NET.Test.Sdk at
build time only. Dependabot is configured to keep the GitHub Actions used by CI current.
