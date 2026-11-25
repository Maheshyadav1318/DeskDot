# Manual QA checklist

The app targets Windows, so these checks are run by hand on a real machine (or a VM)
before tagging a release. `scripts/build.ps1 -Configuration Release` should finish clean
before you start.

## First run

- [ ] `dotnet run --project src/DeskDot/DeskDot.csproj` shows the collapsed pill near the
      top-centre of the primary monitor, with a live clock and CPU/RAM values.
- [ ] No taskbar button appears and the window is not in Alt+Tab.
- [ ] Tray icon is visible and its tooltip reads "DeskDot — Dynamic Island for Windows".
- [ ] No `crash.log` is written to `%APPDATA%\DeskDot\`.

## Expand / collapse

- [ ] Clicking the pill morphs it into the full panel without the contents clipping.
- [ ] `Alt+Space` toggles from any application.
- [ ] `Esc` collapses; clicking the desktop collapses (when "collapse on focus loss" is on).
- [ ] Re-expanding shows the panel at full opacity (no stuck fade).
- [ ] Expanding near the right edge of the screen keeps the panel on-screen.

## Content

- [ ] Clock, date and greeting update; greeting changes by time of day.
- [ ] CPU/RAM values move every few seconds and match Task Manager roughly.
- [ ] Media: play/pause toggles the glyph, next/previous change the track, clicking the
      progress bar seeks.
- [ ] Focus: Start counts down, Pause freezes, Reset returns to the configured length,
      and a tray balloon appears when it hits 00:00.
- [ ] "Next · HH:mm" reads `%APPDATA%\DeskDot\agenda.txt` when present and wraps to the
      first item after the last entry of the day.

## Window behaviour

- [ ] Dragging the island chrome moves it; the position survives a restart.
- [ ] The island stays on top of other windows while expanded and collapsed.
- [ ] Toggling "keep above other windows" off lets other apps cover it.
- [ ] Second launch (`DeskDot.exe` again) toggles the running island instead of starting a
      duplicate; only one tray icon exists.

## Settings

- [ ] Save writes `%APPDATA%\DeskDot\settings.json` with the edited values.
- [ ] Changing the hotkey rebinds it immediately; the old gesture stops working.
- [ ] Choosing a gesture another app owns falls back to `Alt+Space` and still works.
- [ ] "Start DeskDot with Windows" creates and removes the HKCU Run entry
      (`reg query HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v DeskDot`).
- [ ] Corrupting `settings.json` (invalid JSON) still starts the app with defaults.

## Sleep / resume / session

- [ ] Sleep and resume the PC: the hotkey still toggles the island afterwards.
- [ ] Lock and unlock the session: clock and stats resume updating.
- [ ] After resume, no duplicate timers appear (stats don't tick twice as fast).

## Packaging

- [ ] `pwsh ./scripts/build.ps1 -Release -Publish -Installer` produces
      `dist/DeskDot-<version>-setup.exe`.
- [ ] Installing per-user needs no elevation; the app runs from
      `%LOCALAPPDATA%\Programs\DeskDot` and launches from the Start menu.
- [ ] Uninstall removes the app folder and the Run entry.
- [ ] Portable zip runs on a machine without the .NET 8 desktop runtime installed.
- [ ] On Windows 10 21H2 the app runs and simply ignores the Windows 11 corner attributes.
- [ ] On a non-100% display scale the island renders crisply (PerMonitorV2).
