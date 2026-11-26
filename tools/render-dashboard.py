#!/usr/bin/env python3
"""Render docs/screenshots/dashboard.png — a faithful mock of the v2 dashboard.

The visual checks that matter (spacing, type sizes, palette, ordering) can be verified
head-lessly, so the README ships a screenshot that can be regenerated from this file rather
than a hand-cropped capture that goes stale. Layout constants mirror src/DeskDot/MainWindow.xaml.

Usage:  python3 tools/render-dashboard.py [output.png]
"""
import subprocess
import sys

# ---- palette (Themes/Light.xaml) ---------------------------------------------
PAGE, CARD, BORDER = "#F7F5F0", "#FFFFFF", "#EDE9E1"
INK, INK_SOFT, INK_FAINT = "#20211D", "#6C6F66", "#9EA096"
SAGE, SAGE_DEEP, SAGE_SOFT = "#6E7F6A", "#4C5A48", "#EAF0E6"
SAND, TRACK = "#F0EAE0", "#EFEBE3"

SERIF, SANS = "serif", "sans"
W, H = 1160, 1010
M = 28          # page margin
GAP = 16        # gutter between cards
R = 18          # card radius

draw = []
overlay = []   # drawn after the weather photo is composited


def card(x, y, w, h, bg=CARD):
    draw.append(["rrect", x, y, w, h, R, bg, BORDER])


def text(x, y, s, font=SANS, size=12, color=INK, weight="normal"):
    draw.append(["text", x, y, s, font, size, color, weight])


def kicker(x, y, s):
    text(x, y, s.upper(), size=9.5, color=SAGE_DEEP, weight="bold")


def pill(x, y, w, h, label, filled=False):
    """Buttons: PrimaryPill is sage with cream text, GhostPill is outlined."""
    bg = SAGE if filled else None
    draw.append(["rrect", x, y, w, h, h / 2, bg or "none", None if filled else BORDER])
    text(x + 16, y + h / 2 + 4, label, size=11.5,
         color="#F7F5F0" if filled else INK_SOFT, weight="bold")


def meter(x, y, w, h, ratio, color=SAGE):
    draw.append(["rrect", x, y, w, h, h / 2, TRACK, None])
    fill = max(4, int(w * ratio))
    draw.append(["rrect", x, y, fill, h, h / 2, color, None])


def labelled_bar(x, y, w, ratio, label):
    draw.append(["rrect", x, y, w, 34, 4, TRACK, None])
    h = max(5, int(34 * ratio))
    draw.append(["rrect", x, y + 34 - h, w, h, 2, SAGE, None])
    text(x + w / 2 - 4, y + 48, label, size=9, color=INK_FAINT)


# ---- header ------------------------------------------------------------------
draw.append(["circle", M + 5, 26, 4.5, SAGE])
text(M + 20, 32, "DeskDot", SERIF, 17, INK)
text(M + 92, 32, "·  A little space. A better day.", size=12, color=INK_FAINT)
pill(W - M - 78, 14, 78, 26, "Modules")
pill(W - M - 168, 14, 84, 26, "Customize")

body_top = 58
col = (W - 2 * M) - GAP
left_w = int(col * 1.35 / 2.35)
right_w = col - left_w
L, RX = M, M + left_w + GAP

# ---- row 1: focus + weather --------------------------------------------------
y0 = body_top
h1 = 236
card(L, y0, left_w, h1)
kicker(L + 22, y0 + 30, "A LITTLE FOCUS")
text(L + 22, y0 + 96, "25:00", SERIF, 54, INK)
text(L + 22, y0 + 124, "One thing at a time", size=12.5, color=INK_SOFT)
text(L + 22, y0 + 144, "Good morning", size=12.5, color=INK_SOFT)
pill(L + 22, y0 + 170, 108, 32, "Start focus", filled=True)
meter(L + 148, y0 + 182, left_w - 190, 4, 1.0)
text(L + left_w - 96, y0 + 214, "2 ahead", size=11, color=INK_FAINT)

card(RX, y0, right_w, h1)
PHOTO_H = 118
# The photo band is composited afterwards (see render()); text goes on top of it.
overlay += [
    ["text", RX + 22, y0 + 62, "Your city", SANS, 12, "#F3F1EA", "bold"],
    ["text", RX + 22, y0 + 101, "18°", SERIF, 34, "#FFFFFF", "normal"],
    ["text", RX + 78, y0 + 101, "Partly cloudy", SANS, 12.5, "#E7E9E2", "normal"],
    ["text", RX + right_w - 48, y0 + 44, "☁", SANS, 21, "#F3F1EA", "normal"],
    ["text", RX + 22, y0 + 160, "A little possibility.", SANS, 12.5, INK_SOFT, "normal"],
    ["text", RX + 22, y0 + 186, "H: 21°    L: 13°    64%    8 km/h", SANS, 11, INK_FAINT, "normal"],
    ["text", RX + 22, y0 + 210, "Updated 09:41", SANS, 11, INK_FAINT, "normal"],
]

# ---- row 2: system + screen time ---------------------------------------------
y1 = y0 + h1 + GAP
h2 = 224
sys_w = (col - GAP) // 2
card(L, y1, sys_w, h2)
kicker(L + 22, y1 + 30, "THIS PC · LIVE")
draw.append(["circle", L + 22 + 104, y1 + 26, 3, SAGE])
rows = [("CPU", "12%", 0.12), ("MEMORY", "46%", 0.46), ("BATTERY", "100% Charging", 1.0)]
ry = y1 + 60
for label, value, ratio in rows:
    text(L + 22, ry, label, size=9.5, color=INK_FAINT, weight="bold")
    text(L + 22, ry + 22, value, size=13, weight="bold")
    meter(L + 22, ry + 30, sys_w - 44, 4, ratio)
    ry += 50
text(L + 22, y1 + h2 - 14, "No drivers · no elevation   ·   pauses when hidden", size=10.5, color=INK_FAINT)

st_x = L + sys_w + GAP
card(st_x, y1, col - sys_w - GAP, h2)
kicker(st_x + 22, y1 + 30, "SCREEN TIME")
text(st_x + col - sys_w - GAP - 74, y1 + 30, "Details", size=11, color=SAGE_DEEP, weight="bold")
text(st_x + 22, y1 + 68, "2h 34m", SERIF, 34, INK)
text(st_x + 162, y1 + 70, "+12%", size=11.5, color=SAGE_DEEP, weight="bold")
text(st_x + 22, y1 + 90, "A little more than yesterday. Take a breath.", size=11.5, color=INK_SOFT)
bx = st_x + 22
for i, (ratio, label) in enumerate([(0.5, "M"), (0.72, "T"), (0.42, "W"), (0.8, "T"), (0.62, "F"), (0.3, "S"), (0.22, "S")]):
    labelled_bar(bx + i * 20, y1 + 110, 14, ratio, label)
text(st_x + 22, y1 + 186, "Chrome", size=12, color=INK)
text(st_x + col - sys_w - GAP - 74, y1 + 186, "1h 12m", size=11, color=INK_FAINT)
text(st_x + 22, y1 + 206, "VS Code", size=12, color=INK)
text(st_x + col - sys_w - GAP - 74, y1 + 206, "48m", size=11, color=INK_FAINT)

# ---- row 3: reminder + clipboard ---------------------------------------------
y2 = y1 + h2 + GAP
h3 = 146
half = (col - GAP) // 2
card(L, y2, half, h3, SAND)
kicker(L + 22, y2 + 30, "A LITTLE REMINDER")
text(L + 22, y2 + 62, "You don't have to do it all.", SERIF, 19, INK)
text(L + 22, y2 + 86, "Just the next right thing.", SERIF, 19, INK)
pill(L + 22, y2 + 104, 96, 26, "Just for you")
pill(L + 128, y2 + 104, 96, 26, "Open notes")

cx = L + half + GAP
card(cx, y2, col - half - GAP, h3)
kicker(cx + 22, y2 + 30, "CLIPBOARD")
text(cx + col - half - GAP - 74, y2 + 30, "View all", size=11, color=SAGE_DEEP, weight="bold")
text(cx + 22, y2 + 58, "\"Make room for what matters.\"", SERIF, 15, INK)
text(cx + 22, y2 + 80, "2 min ago", size=11, color=INK_FAINT)
text(cx + col - half - GAP - 120, y2 + 80, "4 items", size=11, color=INK_FAINT)
text(cx + 22, y2 + 114, "Stays on your device", size=11, color=INK_FAINT)
pill(cx + col - half - GAP - 78, y2 + 98, 60, 24, "Clear")

# ---- row 4: agenda + notes ---------------------------------------------------
y3 = y2 + h3 + GAP
h4 = 150
ag_w = int(col * 1.35 / 2.35)
card(L, y3, ag_w, h4)
kicker(L + 22, y3 + 30, "UP NEXT")
text(L + ag_w - 96, y3 + 30, "2 ahead", size=11, color=INK_FAINT)
for i, (t, title, detail) in enumerate([
        ("11:30", "Design catch-up", "30 min · Product design"),
        ("14:00", "A little focus time", "Make room for deep work")]):
    yy = y3 + 62 + i * 44
    text(L + 22, yy, t, size=11, color=SAGE_DEEP, weight="bold")
    text(L + 96, yy, title, size=12.5, color=INK, weight="bold")
    text(L + 96, yy + 18, detail, size=11, color=INK_FAINT)

nx = L + ag_w + GAP
card(nx, y3, col - ag_w - GAP, h4)
kicker(nx + 22, y3 + 30, "NOTES")
text(nx + col - ag_w - GAP - 74, y3 + 30, "2 notes", size=11, color=INK_FAINT)
text(nx + 22, y3 + 64, "Groceries", SERIF, 17, INK)
text(nx + 22, y3 + 86, "milk, oats, something green", size=11.5, color=INK_SOFT)
pill(nx + 22, y3 + 108, 84, 26, "New note")

# ---- row 5: modules + footer chrome -----------------------------------------
y4 = y3 + h4 + GAP
card(L, y4, col // 2 + 120, 92)
kicker(L + 22, y4 + 30, "YOUR MODULES")
text(L + 176, y4 + 30, "6 little helpers, enabled", size=11, color=INK_FAINT)
names = ["Focus", "Weather", "System", "Clock", "Notes", "Agenda"]
mx = L + 22
for n in names:
    wdt = 22 + len(n) * 6.6
    draw.append(["rrect", mx, y4 + 48, wdt, 26, 13, SAGE_SOFT, None])
    text(mx + 11, y4 + 65, n, size=11, color=SAGE_DEEP)
    mx += wdt + 8
text(L, H - 22, "Wed, 25 Nov  ·  09:41  ·  DeskDot · D.R.M.Y", size=11, color=INK_FAINT)
text(W - M - 250, H - 22, "Toggle Ctrl+Alt+D   ·   Setup Ctrl+,", size=11, color=INK_FAINT)

# ---- render ------------------------------------------------------------------
out = sys.argv[1] if len(sys.argv) > 1 else "docs/screenshots/dashboard.png"

# Text and shapes below the photo are emitted first; the band is composited over the card,
# then the band's own text is drawn on top of the image.
def emit(items, args):
    for e in items:
        kind = e[0]
        if kind == "rrect":
            _, x, y, rw, rh, r, fill, stroke = e
            if fill != "none":
                args += ["-fill", fill, "-stroke", "none", "-draw",
                         f"roundrectangle {x},{y} {x + rw},{y + rh} {r},{r}"]
            if stroke:
                args += ["-fill", "none", "-stroke", stroke, "-strokewidth", "1", "-draw",
                         f"roundrectangle {x},{y} {x + rw},{y + rh} {r},{r}"]
        elif kind == "rect":
            _, x, y, rw, rh, fill = e
            args += ["-fill", fill, "-stroke", "none", "-draw", f"rectangle {x},{y} {x + rw},{y + rh}"]
        elif kind == "circle":
            _, cx, cy, r, fill = e
            args += ["-fill", fill, "-stroke", "none", "-draw", f"circle {cx},{cy} {cx + r},{cy}"]
        elif kind == "text":
            _, x, y, s_, font, size, color, weight = e
            fam = ("DejaVu-Serif" if font == "serif" else "DejaVu-Sans") + ("-Bold" if weight == "bold" else "")
            args += ["-stroke", "none", "-fill", color, "-font", fam, "-pointsize", str(size),
                     "-annotate", f"+{int(x)}+{int(y)}", s_.replace("%", "%%")]
    return args


def build():
    args = ["convert", "-size", f"{W}x{H}", f"xc:{PAGE}"]
    args = emit(draw, args)

    # Photo band: crop the hero to the band, round only its top corners, tint, then place.
    band, mask, tint = "/tmp/dd-band.png", "/tmp/dd-mask.png", "/tmp/dd-tint.png"
    subprocess.run([
        "convert", "src/DeskDot/Assets/dashboard-hero.jpg",
        "-resize", f"{right_w}x{PHOTO_H}^", "-gravity", "center",
        "-extent", f"{right_w}x{PHOTO_H}", band], check=True)
    subprocess.run([
        "convert", "-size", f"{right_w}x{PHOTO_H}", "xc:black",
        "-fill", "white", "-draw", f"roundrectangle 0,0 {right_w - 1},{2 * R} {R},{R}",
        "-fill", "white", "-draw", f"rectangle 0,{R} {right_w - 1},{PHOTO_H - 1}", mask], check=True)
    subprocess.run([
        "convert", band, "-alpha", "off", mask, "-compose", "CopyOpacity", "-composite", band], check=True)
    # Tint: sage at 55% over the band, clipped to the same rounded mask.
    subprocess.run([
        "convert", band, "-fill", "rgba(48,60,52,0.52)",
        "-draw", f"rectangle 0,0 {right_w},{PHOTO_H}",
        "-alpha", "off", mask, "-compose", "CopyOpacity", "-composite", tint], check=True)

    args += [band, "-geometry", f"+{RX}+{y0}", "-composite",
             tint, "-geometry", f"+{RX}+{y0}", "-composite"]
    return emit(overlay, args)


args = build()
args += ["-depth", "8", out]
subprocess.run(args, check=True)
print("wrote", out)
