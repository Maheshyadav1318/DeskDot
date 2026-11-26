# DeskDot roadmap

## v2.0 — the dashboard (in this repo)

v2.0 replaced the Dynamic Island overlay with the dashboard the app is built around now:
one calm page of eight modules, each of which can be switched off.

Shipped:

- Light, quiet page design: Georgia display type on a warm neutral palette, generous spacing
- **Focus** — 15/20/25/30/45/50/60-minute timer, Start/Pause/Reset, progress ring, chime on end
- **Weather** — live Open-Meteo conditions, high/low, humidity and wind; city geocoding in
  Setup; offline placeholder when the network is unavailable; enabled per user
- **System** — CPU (`GetSystemTimes` delta), memory (`GlobalMemoryStatusEx`) and battery
  (`GetSystemPowerStatus`), sampled every 3 s and paused while the window is hidden
- **Screen time** *(opt-in)* — foreground-app totals, day-over-day comparison, Mon–Sun bars
  and top apps; one rolling week of history in `screentime.json`
- **Clipboard** *(opt-in)* — last 25 copied strings, in memory only, secrets masked, Clear button
- **Notes** — local JSON notes with the latest pinned on the dashboard and a browser window
- **Agenda** — the "Up next" card reads `%APPDATA%\DeskDot\agenda.txt`, split into ahead and passed
- **Clock** + greeting, a rotating reminder card, and a module manager showing all eight
- Two configurable hotkeys, tray menu, geometry persistence, single instance, sleep/resume
  recovery, start with Windows
- 166 unit tests over the pure logic; CI builds x64 and arm64 and produces the installer

Retired with v2.0: the island window and its pill ↔ panel morph, the simulated media card and
its bundled playlist, the dark island theme, and the demo agenda fallback.

## v2.1 — next

- **Per-window screen time** (title-level breakdown) behind the same opt-in, with a per-app
  exclusion list so games or a browser profile can be left out
- **Compact mini mode**: the same modules at a glance in a ~360 px column that can be docked
  to a screen edge, for people who liked the island's footprint
- **Weather cache**: keep the last forecast on disk so a cold start offline still shows
  something real instead of the placeholder
- **Notes**: markdown-lite in the browser, and per-note colour
- **Agenda editing** from the app instead of a text file
- **Reminder source**: let the reminder card draw from a `reminders.txt` the user curates

## Later

- Windows notification mirroring (UserNotificationListener, opt-in)
- Calendar integration through Outlook / `Windows.ApplicationModel.Appointments`
- Plug-in manifests so third-party modules can join the eight
- Auto-update channel (Velopack or a signed GitHub Releases feed)
- MSIX packaging alongside the Inno Setup installer
- Code-signed releases so SmartScreen stops warning

## Quality / tooling backlog

- UI automation smoke test that launches the app on a Windows runner and asserts every card
  renders with real data
- Coverage reporting (Coverlet) published to the CI summary
- `docs/screenshots/` refresh whenever the dashboard's look changes
- Visual check at 125%/150% scaling and on a 4K display

## Known limitations

- Windows 10 1809 (build 17763) and later only. The rounded corners are drawn by WPF, so the
  window looks the same on Windows 10 and Windows 11.
- The app is unsigned. SmartScreen will warn on first run until a code-signing certificate is
  added to the release pipeline.
- Weather needs a network and a city you've picked; without either the card explains itself
  rather than inventing a forecast.
- Screen time attributes time by app name only — two windows of the same browser count as one.
- Clipboard history is text-only and lives in memory, so it does not survive a restart.
