/* DeskDot — Dynamic Island demo overlay
   Designed & Developed by D.R.M.Y. ✨ */
(function () {
  "use strict";

  const island = document.getElementById("island");
  const collapsedView = document.getElementById("collapsedView");
  const expandedView = document.getElementById("expandedView");

  const collapsedTime = document.getElementById("collapsedTime");
  const collapsedDate = document.getElementById("collapsedDate");
  const clockEl = document.getElementById("clock");
  const dateEl = document.getElementById("dateLine");
  const greetingEl = document.getElementById("greeting");

  const miniCpu = document.getElementById("miniCpu");
  const miniRam = document.getElementById("miniRam");
  const cpuVal = document.getElementById("cpuVal");
  const ramVal = document.getElementById("ramVal");
  const cpuBar = document.getElementById("cpuBar");
  const ramBar = document.getElementById("ramBar");

  const focusTimeEl = document.getElementById("focusTime");
  const focusToggle = document.getElementById("focusToggle");
  const focusReset = document.getElementById("focusReset");

  const playBtn = document.getElementById("playBtn");
  const prevBtn = document.getElementById("prevBtn");
  const nextBtn = document.getElementById("nextBtn");
  const progressFill = document.getElementById("progressFill");
  const trackTitle = document.querySelector(".track-title");
  const trackArtist = document.querySelector(".track-artist");

  const desktop = document.getElementById("desktop");

  /* ---------------- Clock & greeting ---------------- */
  const dateFmt = new Intl.DateTimeFormat("en-US", {
    weekday: "short",
    month: "short",
    day: "numeric"
  });

  function two(n) {
    return String(n).padStart(2, "0");
  }

  function greetingFor(hour) {
    if (hour < 5) return "Still up? Take it easy. 🌙";
    if (hour < 12) return "Good morning! ✨";
    if (hour < 17) return "Have a wonderful day! ✨";
    if (hour < 21) return "Good evening! 🌇";
    return "Winding down for the night. 🌙";
  }

  function renderClock() {
    const now = new Date();
    const time = two(now.getHours()) + ":" + two(now.getMinutes());
    const date = dateFmt.format(now);
    collapsedTime.textContent = time;
    clockEl.textContent = time;
    collapsedDate.textContent = date;
    dateEl.textContent = date;
    greetingEl.textContent = greetingFor(now.getHours());
  }

  renderClock();
  setInterval(renderClock, 1000 * 10);

  /* ---------------- Expand / collapse ---------------- */
  let expanded = false;

  function setExpanded(next) {
    expanded = next;
    island.classList.toggle("is-expanded", expanded);
    collapsedView.hidden = expanded;
    expandedView.hidden = !expanded;
  }

  island.addEventListener("click", function (event) {
    if (!expanded) {
      setExpanded(true);
      return;
    }
    // Buttons inside the expanded island should not collapse it.
    if (event.target.closest("button")) return;
  });

  island.addEventListener("keydown", function (event) {
    if (event.key === "Enter" || event.key === " ") {
      if (!expanded) {
        event.preventDefault();
        setExpanded(true);
      }
    }
  });

  desktop.addEventListener("click", function (event) {
    if (expanded && !island.contains(event.target)) setExpanded(false);
  });

  document.addEventListener("keydown", function (event) {
    if (event.key === "Escape" && expanded) setExpanded(false);
    if (event.altKey && event.code === "Space") {
      event.preventDefault();
      setExpanded(!expanded);
    }
  });

  island.tabIndex = 0;

  /* ---------------- System stats simulation ---------------- */
  let cpu = 14;
  let ram = 11;

  function renderStats() {
    const cpuText = Math.round(cpu) + "%";
    const ramText = ram.toFixed(0) + "G";
    miniCpu.textContent = cpuText;
    miniRam.textContent = ramText;
    cpuVal.textContent = cpuText;
    ramVal.textContent = ramText;
    cpuBar.style.width = Math.min(100, cpu) + "%";
    ramBar.style.width = Math.min(100, (ram / 32) * 100) + "%";
  }

  function drift(value, min, max, step) {
    const next = value + (Math.random() - 0.5) * step;
    return Math.min(max, Math.max(min, next));
  }

  renderStats();
  setInterval(function () {
    cpu = drift(cpu, 4, 72, 14);
    ram = drift(ram, 7.5, 18, 0.7);
    renderStats();
  }, 4200);

  /* ---------------- Focus timer ---------------- */
  const FOCUS_TOTAL = 25 * 60;
  let focusLeft = FOCUS_TOTAL;
  let focusRunning = false;
  let focusInterval = null;

  function renderFocus() {
    const minutes = Math.floor(focusLeft / 60);
    const seconds = focusLeft % 60;
    focusTimeEl.textContent = two(minutes) + ":" + two(seconds);
    focusToggle.textContent = focusRunning ? "Pause" : "Start";
  }

  function tickFocus() {
    focusLeft -= 1;
    if (focusLeft <= 0) {
      focusLeft = 0;
      stopFocus();
      focusTimeEl.textContent = "Done ✨";
      focusToggle.textContent = "Start";
      return;
    }
    renderFocus();
  }

  function startFocus() {
    focusRunning = true;
    focusInterval = setInterval(tickFocus, 1000);
    renderFocus();
  }

  function stopFocus() {
    focusRunning = false;
    clearInterval(focusInterval);
    focusInterval = null;
    if (focusLeft > 0) renderFocus();
  }

  focusToggle.addEventListener("click", function () {
    if (focusRunning) {
      stopFocus();
    } else {
      if (focusLeft <= 0) focusLeft = FOCUS_TOTAL;
      startFocus();
    }
  });

  focusReset.addEventListener("click", function () {
    stopFocus();
    focusLeft = FOCUS_TOTAL;
    renderFocus();
  });

  renderFocus();

  /* ---------------- Media player simulation ---------------- */
  const tracks = [
    { title: "Amber Stillness", artist: "Ólafur & Kiasmos" },
    { title: "Quiet Horizons", artist: "Nils Frahm" },
    { title: "Night Circuit", artist: "Bonobo" },
    { title: "Soft Focus", artist: "Tycho" },
    { title: "Afterglow", artist: "Lusine" }
  ];

  let trackIndex = 0;
  let playing = true;
  let position = 32; // percent

  function renderTrack() {
    const track = tracks[trackIndex];
    trackTitle.textContent = track.title;
    trackArtist.textContent = track.artist;
    progressFill.style.width = position + "%";
    playBtn.textContent = playing ? "⏸" : "▶";
    playBtn.setAttribute("aria-label", playing ? "Pause" : "Play");
  }

  playBtn.addEventListener("click", function () {
    playing = !playing;
    renderTrack();
  });

  prevBtn.addEventListener("click", function () {
    trackIndex = (trackIndex - 1 + tracks.length) % tracks.length;
    position = 0;
    renderTrack();
  });

  nextBtn.addEventListener("click", function () {
    trackIndex = (trackIndex + 1) % tracks.length;
    position = 0;
    renderTrack();
  });

  setInterval(function () {
    if (!playing) return;
    position += 0.35;
    if (position >= 100) {
      position = 0;
      trackIndex = (trackIndex + 1) % tracks.length;
      trackTitle.textContent = tracks[trackIndex].title;
      trackArtist.textContent = tracks[trackIndex].artist;
    }
    progressFill.style.width = Math.min(100, position) + "%";
  }, 1000);

  renderTrack();

  /* ---------------- Quick actions ---------------- */
  const actionLabels = {
    DeskDot: "Opening DeskDot…",
    Setup: "Launching DeskDot Setup…",
    Settings: "Opening Settings…",
    "DeskDot.sln": "Opening solution in Visual Studio…"
  };

  document.querySelectorAll(".qa").forEach(function (button) {
    button.addEventListener("click", function () {
      const label = actionLabels[button.dataset.action] || "Launching…";
      button.textContent = label;
      setTimeout(function () {
        button.textContent = button.dataset.action;
      }, 1400);
    });
  });
})();
