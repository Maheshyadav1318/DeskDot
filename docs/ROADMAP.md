# DeskDot roadmap

## v1.0 — shipping in this repo

- Borderless, per-monitor-DPI-aware always-on-top island window (WPF, .NET 8)
- Pill ↔ panel morph with drop shadow, rounded corners and acrylic-free solid surface
- Live clock, date and time-of-day greeting
- CPU load (GetSystemTimes delta) and physical memory (GlobalMemoryStatusEx) — no NuGet deps
- Focus countdown with tray notification on completion
- Bundled demo playlist with working transport + seek
- Agenda card: reads `%APPDATA%\DeskDot\agenda.txt` (`HH:mm Title` per line), demo agenda as fallback
- Global toggle hotkey (configurable, defaults to `Alt+Space`)
- Drag to reposition, position persisted across restarts
- Tray icon with show/hide, settings and exit
- Single-instance; a second launch toggles the running island
- Sleep/resume and session-unlock handling: hotkey re-registration and timer restart
- Start with Windows (HKCU Run entry)
- Settings dialog persisted to `%APPDATA%\DeskDot\settings.json`

## v1.1 — next

- **Windows System Media Transport Controls (GSMTC)** so the island mirrors the real player
  and transport commands go to it — `MediaService` already exposes the seam.
- Album art from the media session, falling back to the bundled artwork
- Per-monitor placement: remember a position per display and restore on display changes
- Auto-collapse timer (e.g. collapse 6 s after expanding if untouched)

## v2.0 — the bigger surface

- Notification mirroring via UserNotificationListener (opt-in)
- Calendar integration through Outlook / Windows.ApplicationModel.Appointments
- Wi-Fi / battery / volume flyouts inside the island
- Plugin manifests so features can be toggled per user
- Auto-update channel (Velopack or a signed GitHub Releases feed)
- MSIX packaging alongside the Inno Setup installer

## Known limitations

- The overlay targets Windows 10 1809 (build 17763) and later; the Windows 11 DWM
  rounded-corner and dark-mode attributes are silently ignored on Windows 10.
- The app is unsigned. SmartScreen will warn on first run until a code-signing
  certificate is added to the release pipeline.
- The browser demo in `docs/demo/` is a design reference, not the shipping app.
