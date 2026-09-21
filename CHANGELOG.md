# Changelog

All notable changes to this project are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/).

## [0.2.0] — Mazu

### Added
- **Mazu, the desktop mascot**: an original blue slime that lives bottom-right, always on top. Five moods (happy, tired, worried, sleepy, surprised) driven by CPU, memory, disk, network and idle time, with speech bubbles (several lines per mood, English and Türkçe). Click to chat, drag to move (position is remembered), right-click to hide. Setting: *Show Mazu*.
- New logo and icon (Mazu), rendered from vector art by `tools/New-Icon.ps1`.
- `ISystemMonitor.Subscribe(..., includeGpu)`: consumers that do not need the GPU keep its expensive counters closed.
- Author credit: Claxe.

### Removed
- The third-party mascot image; the project now ships only original artwork.

## [0.1.0] — first public MVP

### Added
- Dashboard, System, Network, Processes, Storage, Utilities and Settings pages.
- Live CPU / RAM / GPU metrics (built-in Windows counters, no third-party packages), sampled only while the page is visible.
- Network: ping with statistics, DNS / gateway / local and public IP, adapter details, flush DNS cache, renew IP.
- Process manager with end / restart and a protection layer for critical Windows processes.
- Storage: disk usage and folder size calculator.
- Utilities: SHA-256 / SHA-1 / MD5 (file and text, compare), port checker, CMD / PowerShell / Explorer launchers.
- System tray (Open Mazure Tools, Dashboard, Network, Exit) and minimise-to-tray on close.
- English and Türkçe interface, switchable live in Settings; first run follows the Windows language.
- Dark (default) and light theme; native title bar follows the theme.
- Portable build script (`publish.ps1`): single-file, self-contained, x64 and ARM64.
- Mazure mascot logo and icon.
