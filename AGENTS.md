# AGENTS.md

Guidance for AI agents working on **DCSSimpleLauncher** — a WinUI 3 desktop app that launches [Digital Combat Simulator](https://www.digitalcombatsimulator.com/) with configurable options.

## Project overview

The app lets users:

- Point at their DCS install folder, Saved Games folder, and a launcher data folder (settings).
- Toggle VR and the in-game launcher before starting DCS.
- Patch `Config/Options.lua` (with backup) via Lua parsing, then launch `bin\DCS.exe`.
- Force-stop running DCS processes.
- Add custom apps to launch prior to launching `DCS.exe` in External Tools page (companion apps launched before DCS), profile management, and broader launcher features. The codebase is early-stage; expect incomplete wiring and stubs.

## Tech stack

| Layer | Choice |
|-------|--------|
| UI | WinUI 3 (`Microsoft.WindowsAppSDK`) |
| Runtime | .NET 10 (`net10.0-windows10.0.22621.0`) |
| Packaging | MSIX tooling enabled (`EnableMsixTooling`) |
| Lua | [LuaCSharp](https://www.nuget.org/packages/LuaCSharp) 0.5.3 — read/modify DCS `Options.lua` |
| Settings | `Windows.Storage.ApplicationData.Current.LocalSettings` |
| Profiles | JSON files in the user-selected launcher data folder |

**Primary IDE:** Visual Studio (solution: `DCSSimpleLauncher.sln`). VS Code / Zed configs exist but are secondary.

## Repository layout

```
DCSSimpleLauncher/
├── App.xaml(.cs)           # Application entry; exposes App.Window for pickers
├── MainWindow.xaml(.cs)    # Shell: NavigationView + Frame, custom title bar
├── Views/                  # Pages navigated inside ContentFrame
│   ├── Launcher.xaml(.cs)  # Launch DCS, VR/Launcher toggles, force stop
│   ├── SettingsPage.xaml(.cs)  # Folder pickers for DCS paths
│   └── ExternalToolsPage.xaml(.cs)  # Stub — current focus area
├── Data/
│   ├── Profile.cs          # Name, UseVR, UseLauncher, PrelaunchApps
│   └── CompanionApp.cs     # External tool definition (Name, Path, Args, Delay, Minimize)
├── Helper/
│   ├── SettingsKeys.cs     # LocalSettings key constants — use these, not raw strings
│   ├── LuaParserExtension.cs  # LuaTable → formatted Lua string (C# 13 extension)
│   └── WindowHelper.cs     # Multi-window helper (legacy namespace — see pitfalls)
└── DCSSimpleLauncher.csproj
```

## Architecture

```
App.OnLaunched → MainWindow
                      ├── NavigationView (Launcher | External Tools | Settings)
                      └── Frame → Page (Views.*)
```

**Navigation:** `MainWindow.MainNavigation_ItemInvoked` maps `Tag` values (`Launcher`, `ExternalToolsPage`) and the settings gear to `ContentFrame.Navigate(...)`.

**Settings keys** (always use `Helper.SettingsKeys`):

| Key constant | Stored value |
|--------------|--------------|
| `DCS_FOLDER` | DCS installation root |
| `SAVEDGAMES_DCS_FOLDER` | `%USERPROFILE%\Saved Games\DCS` (or custom) |
| `SAVEDGAMES_DCSLAUNCHER_FOLDER` | Folder for launcher profiles/data |
| `CURRENT_PROFILE` | Active profile filename (e.g. `Default.json`) |

**Launch flow** (`Views/Launcher.xaml.cs`):

1. Validate DCS and Saved Games folders are set.
2. Load `Options.lua` with `LuaState.DoFileAsync`.
3. Set `options.miscellaneous.launcher` and `options.VR.enable`.
4. Serialize back with `LuaTable.ToFormattedString()`, backup original to `Options_backup.lua`, write file.
5. Start `DCS.exe` with `UseShellExecute = true`.

**Profiles:** `Profile` is deserialized from JSON on the launcher page load. Saving profile changes and a profile UI are not fully implemented yet. `CompanionApp` models pre-launch external tools for the External Tools page.

## Build and run

```powershell
# Build (x64 — required; AnyCPU is remapped in .csproj)
dotnet build DCSSimpleLauncher.csproj /property:Platform=x64

# Run
dotnet run --project DCSSimpleLauncher.csproj /property:Platform=x64
```

In **Visual Studio:** open `DCSSimpleLauncher.sln`, set platform to **x64**, F5.

Output: `bin/x64/Debug/net10.0-windows10.0.22621.0/DCSSimpleLauncher.dll`

**Note:** `win-x64.pubxml` publish profile is referenced but missing — publish may warn until profiles are added.

## Coding conventions

Follow existing patterns in the file you edit:

- **Namespaces:** Root `DCSSimpleLauncher`; pages under `DCSSimpleLauncher.Views`; helpers under `DCSSimpleLauncher.Helper`; models under `DCSSimpleLauncher.Data`. New code should use `DCSSimpleLauncher`, not legacy `SimpleDCSLauncher`.
- **Nullable:** Enabled project-wide. Honor nullability; fix warnings when touching a file.
- **Visibility:** `internal` for app-private types (`Profile`, `SettingsKeys`). Pages are `public sealed partial`.
- **XAML:** Code-behind event handlers (`Click="..."`). `x:Bind` with `Mode=TwoWay` for simple page properties.
- **WinUI pickers:** Always initialize with the window HWND — see `SettingsPage` for the pattern (`App.Window` → `WindowNative.GetWindowHandle` → `InitializeWithWindow.Initialize`).
- **Dialogs:** Use `ContentDialog` with `XamlRoot = this.XamlRoot` on pages.
- **Settings:** Read/write via `ApplicationData.Current.LocalSettings.Values[SettingsKeys.*]` — do not hardcode key strings (see existing inconsistency in `Launcher.xaml.cs` lines 86–87).
- **Scope:** Minimal diffs. Do not refactor unrelated code. Match brace style and usings of the surrounding file.

## Current focus: External Tools page

`Views/ExternalToolsPage` is a stub (placeholder “Add External Tool” button). Intended behavior (from `Data/CompanionApp` and `Profile.PrelaunchApps`):

- Manage a list of companion apps (executable path, args, delay, minimize).
- Persist them on the active `Profile` JSON in the launcher data folder.
- Launch them (in order, with delays) before DCS when the user starts a session.

When implementing, wire through `Profile` / `SettingsKeys` and reuse the launch patterns from `Launcher.xaml.cs`.

## Logging and errors

**Direction:** Move away from silent `catch { }` blocks and ad-hoc `Console.WriteLine`. A proper logging approach will be introduced — coordinate with the maintainer before picking a library (e.g. `Microsoft.Extensions.Logging`, Serilog, or a thin file logger).

Until then:

- Show user-visible errors via `ContentDialog` for actionable failures.
- Do not swallow exceptions without at least logging once a logger exists.
- Remove debug `Console.WriteLine` when replacing with real logging.

## Testing

No test project yet. **Add tests when touching critical logic** — especially:

- `LuaParserExtension` serialization (round-trip shape, nested tables, booleans).
- Profile JSON serialization/deserialization.
- Settings key usage and path validation helpers (if extracted).

Prefer a separate test project (`DCSSimpleLauncher.Tests`) with xUnit or MSTest if adding tests. Manual verification on Windows with a real DCS install is still required for launch flows.

## Pitfalls and known issues

| Issue | Location | Guidance |
|-------|----------|----------|
| Wrong namespace | `Helper/WindowHelper.cs` uses `SimpleDCSLauncher.Helper` | Fix to `DCSSimpleLauncher.Helper` when touching the file |
| Hardcoded settings keys | `Launcher.xaml.cs` uses `"SavedGamesDCSFolder"` / `"DCSFolder"` | Replace with `SettingsKeys` constants |
| Silent catch | `Launcher` constructor profile load | Add logging; consider user feedback |
| Profile not saved | Launcher toggles update in-memory `Profile` only | Persist to JSON when implementing profiles |
| `Options.lua` rewrite | Full-file replace via `ToFormattedString` | Preserve DCS compatibility; test against real configs; backup already exists |
| MSIX identity | `Package.appxmanifest` display names mix SimpleDCSLauncher / DCSSimpleLauncher | Align naming when packaging |

## Dependencies

- Do not add packages without a clear need.
- **LuaCSharp** is required for DCS config editing; keep Lua serialization compatible with DCS’s expected `options = { ... }` format.
- **Windows App SDK** version is pinned in `.csproj` — bump deliberately and test WinUI behavior.

## What agents should not do

- Do not commit `.idea/`, `.vs/`, `bin/`, `obj/`, or user-specific IDE state unless explicitly asked.
- Do not create git commits or push unless the user requests it.
- Do not rewrite `Options.lua` with a different format than DCS expects.
- Do not assume DCS is installed in CI; unit-test pure logic, manual-test launch paths.
- Do not expand scope into unrelated refactors (title bar experiments, packaging) unless asked.

## Useful references

- [WinUI 3 docs](https://learn.microsoft.com/windows/apps/winui/winui3/)
- [Windows App SDK](https://learn.microsoft.com/windows/apps/windows-app-sdk/)
- DCS config: `%SavedGames%\DCS\Config\Options.lua`
- Repo: https://github.com/sdiprizio/DCSSimpleLauncher
