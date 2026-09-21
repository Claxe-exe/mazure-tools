# Roadmap and ideas

Nothing here is promised or implemented yet. Ideas are ordered roughly by how much they add for the effort.
Items that change system state must follow the project rule: **show what will happen, ask first, offer undo where possible.**

## Give the app a soul

- **More life for Mazu.** Mazu v1 (five moods, speech bubbles, drag, poke) shipped in 0.2.0. Next: the tray icon shows the current mood, Mazu cheers after a successful ping / hash match, blinks and looks around, more lines, optional sound.
- **Live charts.** Dashboard and System currently show only numbers. Add 60-second sparklines for CPU / RAM / GPU / network throughput with peak markers, and an optional CSV export.
- **Smart alerts.** Tray notifications for "CPU above 90 % for a minute (top process: …)", "disk below 10 % free", "network dropped / came back", with thresholds and quiet hours in Settings.

## New tools

- **"Fix my network" wizard.** A guided diagnosis instead of blind buttons: adapter up? → gateway reachable? → DNS resolves? → internet reachable? → then offer the right fix (flush DNS, renew IP, reset Winsock) step by step with the result of each step.
- **Startup manager.** List everything that starts with Windows (Run keys, Startup folders, scheduled tasks) with a rough impact estimate; enable / disable with undo.
- **Disk cleanup with preview.** Windows temp, thumbnail cache, Recycle Bin, leftover update files — show sizes first, delete only after confirmation. Plus a "biggest folders" view for Storage.
- **Network monitor.** Live up/down speed per adapter, active connections per process (like `netstat -b`), traceroute, DNS lookup, and an optional speed test.
- **Mini overlay + global hotkey.** A tiny always-on-top CPU / RAM / ping widget, toggled with a hotkey.
- **System report export.** One click to an HTML/Markdown report (hardware, OS, network, top processes) for support tickets, with toggles to leave private data out.
- **Hosts file editor** with automatic backup and diff, and an **environment variables** editor.

## Already known (from the README)

- [ ] DNS server changing (with a clear before/after and restore)
- [ ] Windows services management
- [ ] Preserve command-line arguments when restarting a process
- [ ] Process details: path, user, threads, command line
- [ ] Code-signed releases (removes the SmartScreen warning)
- [ ] Automated tests for the services (ping, hash, storage, guard) in CI
- [ ] Test the ARM64 build on real hardware
