# Testing

Two layers: an automated suite that runs on every push, and a manual pass on a real Windows
machine before a release is tagged.

## Automated

```powershell
dotnet test tests/DeskDot.Tests -c Release
```

166 xunit tests over everything that does not need a desktop: `Core/`, `Models/` and the
portable services (`SettingsService`, `NotesService`, `AgendaService`). CI runs the same suite
on an Ubuntu runner (`Unit tests` job) inside a Windows-targeting project, then builds both
architectures with `-warnaserror`. A red test or a single warning blocks the build.

What the suite covers:

| Area | Examples |
| --- | --- |
| `DayPlan` | `HH:mm` and `11:30AM`/`2:00PM` parsing, ordering, splitting ahead vs passed, malformed lines |
| `WeatherCodes` | WMO code → wording and glyph, unknown codes, temperature/unit formatting |
| `ClipboardText` | trimming, consecutive-duplicate removal, cap, secret detection, preview elision |
| `PowerStatus` | charge states, clamping out-of-range readings, desktop with no battery |
| `ScreenTimeAggregator` | app totals, sorting, day-over-day comparison, week bars, duration text |
| `FocusClock` | countdown, pause/resume, completion, reset, formatting |
| `SystemFormat` | CPU/memory/uptime formatting and percentages |
| `HotkeyGesture` | parsing and canonical spelling round-trip for every offered combination |
| `SettingsService` | defaults, round-trip, corrupt-file recovery, atomic save, privacy defaults |
| `NotesService` | add, delete, ordering, persistence, empty state |

Not covered automatically (needs Win32 or WPF): the tray, global hotkeys, clipboard capture,
foreground-window sampling, the weather HTTP call itself, and anything visual.

## Manual checklist — before tagging a release

Run `pwsh scripts/build.ps1` first so the published build is what you test.

### First run

- [ ] `dotnet run --project src/DeskDot` opens the dashboard, centred on the primary monitor.
- [ ] Tray icon appears; tooltip reads "DeskDot — A little space. A better day."
- [ ] No `crash.log` is written to `%APPDATA%\DeskDot\`.
- [ ] The greeting matches the time of day, and the clock ticks every second.

### Dashboard

- [ ] Every card renders with real values — no `{Binding}` text and no empty gaps.
- [ ] Focus: Start counts down, Pause freezes the ring, Reset returns to the chosen length,
      and finishing shows a tray balloon.
- [ ] Weather: with a city chosen, temperature, high/low, humidity and wind match the
      Open-Meteo page for that city; "updated" says how long ago.
- [ ] Weather without a network: the card shows its placeholder and does not throw.
- [ ] System: CPU moves; percentages roughly match Task Manager; memory matches the total
      RAM; a desktop without a battery reads "No battery".
- [ ] Hide the window and confirm system sampling stops (CPU use of DeskDot → ~0 in Task
      Manager); it resumes when shown again.
- [ ] Screen time (after opting in): the total grows while you work, the week bars fill, top
      apps are listed, and Details opens a readable breakdown.
- [ ] Clipboard (after opting in): copy text in another app, the card updates within a second;
      copy a password and confirm it shows as masked; Clear empties it.
- [ ] Notes: add, edit and delete; the latest note is pinned on the dashboard; entries survive
      a restart.
- [ ] Agenda: a `HH:mm Title | detail` line added to `%APPDATA%\DeskDot\agenda.txt` appears
      in the right group (ahead vs passed) without restarting.

### Window behaviour

- [ ] Both hotkeys work from another application, and rebinding in Setup takes effect at once.
- [ ] A gesture another app owns falls back to the default and Setup says so.
- [ ] Drag the window, resize it, restart: geometry is restored.
- [ ] Move it to a second monitor at a different scale, restart: it comes back on-screen and
      crisp (PerMonitorV2).
- [ ] After unplugging the monitor it was last on, the window reappears on the primary display.
- [ ] Show/hide from the tray and the hotkey both work; "Exit" closes everything but the app
      keeps its settings.
- [ ] Second launch toggles the running window instead of starting a duplicate.

### Setup dialog

- [ ] All four tabs (General / Modules / Weather / Privacy) render inside the dialog with no
      clipped controls, and combos and switches are readable.
- [ ] Turning a module off removes its card immediately and survives a restart.
- [ ] Weather lookup: typing "Guntur" lists matches; picking one stores the coordinates;
      °C ↔ °F switches the card's units.
- [ ] "Start with DeskDot with Windows" creates and removes the HKCU Run entry
      (`reg query HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v DeskDot`).
- [ ] Corrupting `settings.json` still starts the app with defaults.

### Sleep / resume / session

- [ ] Sleep and resume: the hotkeys still work and the clock resumes ticking.
- [ ] Lock and unlock: same, and screen time keeps a single running total (no double-counting).

### Packaging

- [ ] `pwsh scripts/build.ps1` produces `dist/DeskDot-2.0.0-setup.exe` plus both zips.
- [ ] Installing needs no elevation; the app runs from `%LOCALAPPDATA%\Programs\DeskDot` and
      launches from the Start menu (both entries).
- [ ] Uninstall removes the app folder and the Run entry.
- [ ] The portable zip runs on a machine without the .NET 8 desktop runtime.
- [ ] On Windows 10 21H2 everything renders as it does on Windows 11.
