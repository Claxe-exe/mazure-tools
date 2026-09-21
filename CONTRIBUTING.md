# Contributing to Mazure Tools

Thanks for helping! Bug reports, ideas (see [ROADMAP.md](ROADMAP.md)), translations and pull requests are all welcome.

## Setup

```powershell
winget install Microsoft.DotNet.SDK.10
dotnet build
dotnet run
```

## Ground rules

1. **UI never does system work.** Views bind to view models; view models call service interfaces (`INetworkService`, `IProcessService`, …). No `Process.Start`, registry or file access in views or view models.
2. **Safe by default.** Anything that changes the system must explain what it will do, ask for confirmation, and report a clear result. Never elevate silently; use `IElevationService`.
3. **No third-party packages.** Prefer built-in Windows APIs and system tools. If a package is truly needed, explain why in the pull request.
4. **No fake data.** If something is not implemented or not available, say so in the UI (`N/A`, a note) and leave a TODO — do not show made-up values.
5. **Every visible text is translated.** Add keys to **both** `Languages/Strings.English.xaml` and `Strings.Turkish.xaml`, use `{DynamicResource Key}` in XAML and `Loc.T("Key")` in code, and run:

   ```powershell
   .\tools\Test-Localization.ps1
   ```
6. **Only poll while visible.** Timers start in `OnActivated` and stop in `OnDeactivated` of the page view model.
7. Errors become readable messages (`ErrorMessages`), never a raw stack trace.

## Trying Mazu's moods

Set the environment variable `MAZURE_MASCOT_MOOD` to `Happy`, `Tired`, `Worried`, `Sleepy` or `Surprised` before starting the app to force a mood (useful for screenshots and for checking new artwork).

## Adding a tool

Create `Modules/YourTool/` with a service (interface + implementation), a `PageViewModel`, and a `UserControl`. Add the id to `PageId`, map view model → view in `Views/Templates.xaml`, register everything in `App.xaml.cs`, and add the navigation text (`Nav_<PageId>`) to both language files.

## Pull requests

Keep them focused, build with 0 warnings, and fill in the checklist in the template. Screenshots help for UI changes (please use a clean profile — no personal names, IPs or file paths).
