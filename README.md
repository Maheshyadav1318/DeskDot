# DeskDot

DeskDot is a modern Dynamic Island-inspired productivity overlay for Windows 10 and Windows 11. DeskDot puts essential tools, system information, notifications, media controls, and everyday productivity features right at your fingertips.

Designed &amp; Developed by D.R.M.Y. ✨

## Live demo

A static, browser-based recreation of the DeskDot Dynamic Island overlay:

- **Collapsed pill** — live clock, date, CPU/RAM read-outs
- **Expanded island** — greeting, clock, next calendar item, quick actions, media player with album art, 25-minute focus timer, system stats
- **Interaction** — click the island to expand, click the desktop or press `Esc` to collapse, `Alt`+`Space` toggles

## Project structure

```
index.html          Dynamic Island markup (collapsed + expanded states)
css/style.css       Desktop wallpaper, island styling, responsive layout
js/app.js           Clock, simulated system stats, focus timer, media player
images/             Album art and static assets
```

## Run locally

No build step required — it is plain HTML/CSS/JS. Serve the folder with any static server:

```bash
python -m http.server 8080
# then open http://localhost:8080
```

Or simply open `index.html` in a browser.

## Roadmap

- Native Windows shell (WPF / WinUI 3) host with a true always-on-top overlay
- Real system telemetry (CPU, RAM, GPU, battery, network)
- Media session integration via the Windows System Media Transport Controls
- Notification mirroring and calendar integration
- Configurable hotkeys and per-monitor placement
