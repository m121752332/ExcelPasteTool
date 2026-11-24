# DataSplitter Pro (ExcelPasteTool)

[繁體中文](README.md) | **English**

A small cross-platform desktop tool that turns one column of Excel data into a delimited string and a SQL `IN` condition with a single click.

Copy a column of part numbers, order numbers or codes from Excel, click **Paste Data**, and you get all of these at once:

| Output | Example | Typical use |
| --- | --- | --- |
| Delimited data | `A\|B\|C\|D` | Multi-value search fields in an ERP or a report |
| SQL condition | `('A', 'B', 'C', 'D')` | Put it right after `WHERE column IN` |
| Data table | One numbered row per value | Check the count, remove unwanted items |

- The repository is `ExcelPasteTool`, the product is **DataSplitter Pro**, and the executable is `DataSplitter`.
- Runs on Windows, macOS and Linux. Built with [.NET 11](https://dotnet.microsoft.com/) and [Avalonia UI](https://avaloniaui.net/) 12.
- The download bundles the .NET runtime, so **you do not need to install .NET**.

## Contents

- [Features](#features)
- [Download and install](#download-and-install)
- [How to use](#how-to-use)
- [Automatic updates](#automatic-updates)
- [Development](#development)
- [Tests](#tests)
- [Packaging and releases](#packaging-and-releases)
- [Project structure](#project-structure)
- [More documentation](#more-documentation)

## Features

- **Convert on paste**: reads the clipboard, takes the first column of each line, and removes blank lines and duplicates.
- **Duplicates view**: the **Duplicates** button lists the values removed as duplicates, how many times each appeared and how many were removed.
- **Three synced views**: change the delimited data, the SQL condition or the table, and the other two update.
- **Direct editing**: type or edit in the delimited data box; the table and the SQL condition update at once.
- **Remove one row**: hover a table row and click the ✕ on its right.
- **Five separators**: pipe `|`, comma `,` (double quotes handled by CSV rules), semicolon `;`, colon `:` and space.
- **One-click copy**: the delimited data and the SQL condition each have a copy button; click the **Row** header to copy the whole column.
- **SQL-safe quoting**: a single quote in a value becomes two single quotes.
- **Look and feel**: five languages (Traditional Chinese, Simplified Chinese, English, Japanese, Korean), four themes (GitHub Dark, Light, Tiffany Blue, Rainbow Wonderland), adjustable font and font size, and an optional compact title bar.
- **Guided tour and help**: the **Tour** button on the Tools page walks through each area (Space goes to the next step); press `F1` for the illustrated help.
- **Shortcuts**: `Alt+P` pastes the clipboard, `Alt+Q` toggles the sidebar, `Alt+F4` closes the app.
- **Automatic updates**: checks GitHub for a new version at startup, downloads it inside the app, and shows the release notes of each version.

## Download and install

1. Download the file for your platform from the [Releases page](https://github.com/m121752332/ExcelPasteTool/releases/latest):

   | Platform | File |
   | --- | --- |
   | Windows 64-bit | `DataSplitter_win_x64.zip` |
   | macOS (Apple Silicon, M series) | `DataSplitter_osx_arm64.zip` |
   | macOS (Intel) | `DataSplitter_osx_x64.zip` |
   | Linux 64-bit | `DataSplitter_linux_x64.zip` |

2. Unzip it into a folder you can **write to** (for example Desktop or Documents). Do not put it under `C:\Program Files\`, or the settings cannot be saved.
3. Run it:
   - **Windows**: double-click `DataSplitter.exe`. If SmartScreen warns on the first run, click **More info** → **Run anyway**.
   - **macOS**: double-click `DataSplitter.app` (you can drag it into Applications). The other file named `DataSplitter` in the folder is a compatibility script for older auto-updates; ignore it.
     The app is not signed or notarized by Apple, so the first launch after a browser download says it is from an unidentified developer. Do one of these once, then it opens normally:
     - **Terminal (recommended)**: open Terminal, type `cd ` (with a trailing space), drag the unzipped folder into the window, press Enter, run `xattr -dr com.apple.quarantine .`, then double-click `DataSplitter.app` in Finder.
     - **System Settings**: double-click `DataSplitter.app` once so it is blocked, open **System Settings > Privacy & Security**, find "DataSplitter was blocked" and click **Open Anyway**, then confirm with your password.

     The zip includes `README_macOS.txt` with the same steps.
   - **Linux**: `chmod +x DataSplitter && ./DataSplitter`. It needs X11 and CJK fonts; on Debian or Ubuntu:

     ```bash
     sudo apt install libx11-6 libice6 libsm6 libfontconfig1 fonts-noto-cjk
     ```

     To show the help (F1) inside the app, also install `libwebkit2gtk-4.1-0`. Without it, the help opens in your default browser.

Settings file:

| Platform | Location |
| --- | --- |
| Windows, Linux | `config.json` next to the executable |
| macOS | `~/Library/Application Support/DataSplitter/config.json` |

## How to use

1. In Excel, select a column and copy it (Ctrl+C).
2. Click **Paste Data** on the right (or press `Alt+P`).
3. The delimited data (top left) and the SQL condition (bottom left) appear at once. Use the button at the top right of each box to copy it.
4. When you need to:
   - Change the **separator** in the toolbar.
   - Hover a row and click **✕** to remove it.
   - Click **Duplicates** to see which values were removed as duplicates.
   - Type or edit directly in the delimited data box.
   - Click **Clear Data** to start over.

When you paste several columns, only the first column of each line is used (columns are split on Tab, comma, semicolon or pipe).

Press **F1** at any time for the help. By default it opens inside the app with the system web engine (WebView2 on Windows, WKWebView on macOS, WebKitGTK on Linux). You can set **Help Viewer** to **Default browser** on the Settings page. If no engine is available, the help opens in the default browser.

The toolbar changes the language and theme for the current session. To keep them, change them on the **Settings** page in the sidebar.

## Automatic updates

- At startup the app checks the latest published GitHub Release and offers to download a newer version.
- The update picks the zip for your platform, replaces the executable and keeps `config.json`.
- Every zip bundles the .NET runtime, so updating from older versions (including the .NET 8 and .NET 10 builds) needs no extra install.

How it works and the release rules: [docs/AutoUpdate.md](docs/AutoUpdate.md) (Traditional Chinese).

## Development

### Tools

| Tool | Version | Notes |
| --- | --- | --- |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/11.0) | 11.0.100 or a later 11.0 SDK | Pinned in `global.json` |
| Git | Any recent version | To get the source |

Run `dotnet --list-sdks` to see the installed SDKs.

IDE (pick one):

- **Visual Studio 2026** (update to a version that supports .NET 11; Visual Studio 2022 does not): install the ".NET desktop development" workload and the [Avalonia for Visual Studio](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS) extension.
- **JetBrains Rider**: Avalonia support is built in.
- **VS Code**: install C# Dev Kit and Avalonia for VS Code.

### Get the source, build and run

Run these commands from the repository root:

```bash
git clone https://github.com/m121752332/ExcelPasteTool.git
cd ExcelPasteTool

# Build the whole solution (app + tests)
dotnet build

# Run the app
dotnet run --project src/ExcelPasteTool
```

- You can also open `ExcelPasteTool.sln` in your IDE, set `ExcelPasteTool` as the startup project and press F5.
- Build output goes to `src/ExcelPasteTool/bin/<Configuration>/net11.0/`, and the settings file used during development is there too. `packaging/config.json` is only the default template for packaging.
- NuGet package versions are managed centrally in `Directory.Packages.props`; `PackageReference` items in `.csproj` files carry no version. When you add a package, update both, or you get error NU1008.
- UI text comes from the dictionaries in `src/ExcelPasteTool/Helpers/LanguageManager.cs`. `Resources/Languages/*.json` is packaged but not read yet. When you add text, update both.

## Tests

```bash
# Unit tests (xUnit)
dotnet test tests/ExcelPasteTool.Tests

# UI tests (Avalonia Headless: render screens and compare them with baseline images)
pwsh tests/run-ui-tests.ps1
```

- The UI test report is `TestResults/ui/summary.json`. Usage and how to update baselines: [docs/UI-TESTING.md](docs/UI-TESTING.md) (Traditional Chinese).
- CI (`build.yml`) builds the Release configuration and runs the unit tests on every pull request and push to `master`.

Check these by hand after a change, because the automated tests do not cover them:

| Item | Action | Expected result |
| --- | --- | --- |
| Paste | Copy a column with duplicates and blank rows from Excel, click **Paste Data** | Duplicates and blanks removed, count correct |
| Sync | Paste `A`, `B`, `C`, `D` | Delimited data `A\|B\|C\|D`, SQL condition `('A', 'B', 'C', 'D')` |
| Remove one | Hover row 2 and click ✕ | `A`, `C`, `D` remain, renumbered 1–3, other views in sync |
| Edit | Type `X\|Y` in the delimited data box | Table and SQL condition update at once |
| Separator | Switch to semicolon | Delimited data becomes `A;C;D` |
| Single quote | Paste `O'Neil` | SQL condition `('O''Neil')` |
| Copy | Click the three copy buttons | Clipboard content correct, success toast shown |
| Clear | Click **Clear Data** | All three views empty, separator back to pipe |
| Language and theme | Switch through each once | All text and colors update, nothing left over |
| Settings saved | Change language, theme and font on Settings, restart | Settings kept |

## Packaging and releases

### One set of publish settings

The publish settings for the four platforms (self-contained .NET 11, single file, bundled native libraries, ReadyToRun, no `.pdb`) live in `src/ExcelPasteTool/Properties/PublishProfiles/<rid>.pubxml`. Visual Studio's **Publish**, the command line and GitHub Actions all read these files, so they give the same result. Change publish settings only there.

| Method | How | Output |
| --- | --- | --- |
| Visual Studio | Right-click the project → **Publish**, pick `win-x64`, `linux-x64`, `osx-x64` or `osx-arm64` | `artifacts/<rid>/` |
| Command line | `dotnet publish src/ExcelPasteTool -c Release -p:PublishProfile=win-x64` | `artifacts/<rid>/` |
| Zip package | `bash packaging/publish.sh win-x64 1.7.0` (needs `jq` and `zip`; run the macOS builds on a Mac) | `DataSplitter_win_x64.zip` in the repository root |

`artifacts/` is in `.gitignore`. When you hand a manual build to someone, add `packaging/config.json` to the output folder; `publish.sh` does this for you.

### Publish a release (GitHub Actions)

When you push a version tag, `release.yml` builds the four zips with `packaging/publish.sh` and creates a GitHub Release:

1. Set `Version`, `AssemblyVersion`, `FileVersion` and `InformationalVersion` in `src/ExcelPasteTool/ExcelPasteTool.csproj` to the new version, and add an entry with notes in all five languages at the top of `src/ExcelPasteTool/Resources/Changelog.json` (the unit tests check this). Merge into `master`.
2. Create and push the tag (`1.7.0` or `v1.7.0` both work):

   ```bash
   git checkout master && git pull
   git tag 1.7.0
   git push origin 1.7.0
   ```

3. When the workflow finishes, check that the Releases page has all four zips.

Do not mark the Release as Draft or Pre-release, and do not rename the zips, or installed copies will not see the update.

## Project structure

```text
ExcelPasteTool/                     # Repository root
├── ExcelPasteTool.sln              # Solution (app + two test projects)
├── Directory.Packages.props        # Central NuGet package versions
├── global.json                     # .NET SDK version
├── README.md, README.en.md, AI_CONTEXT.md
│
├── src/ExcelPasteTool/             # App project (assembly name DataSplitter)
│   ├── ExcelPasteTool.csproj       # Project file (the version is here)
│   ├── Program.cs                  # Entry point
│   ├── App.axaml(.cs)              # App startup (settings, theme, font)
│   ├── Views/                      # Windows and pages (main, tools, settings, about, help, update, release notes, duplicates)
│   ├── ViewModels/                 # Settings page and sidebar logic
│   ├── Models/                     # Data models (DataItem, SidebarItem)
│   ├── Services/                   # Settings, paths, update check and auto-update, help, release notes
│   ├── Helpers/                    # Theme, language, font, icons, toasts, paste and separator handling
│   ├── Controls/                   # Custom controls (tour overlay TourOverlay)
│   ├── Converters/                 # Binding converters
│   ├── Assets/                     # Themes, Styles, Fonts (Noto Sans TC regular/bold), Help, Images, Logos
│   ├── Resources/                  # Languages/*.json, Changelog.json
│   └── Properties/                 # Resources.resx, PublishProfiles/ (publish settings for four platforms)
│
├── tests/
│   ├── ExcelPasteTool.Tests/       # Unit tests (xUnit)
│   ├── ExcelPasteTool.UITests/     # Avalonia Headless UI tests and baseline images
│   └── run-ui-tests.ps1            # One-step UI test run and report
│
├── packaging/
│   ├── publish.sh                  # Publish one platform and zip it (called by release.yml)
│   ├── config.json                 # Default settings template (added to each zip)
│   └── macos/                      # Info.plist, icon and compatibility launcher for the .app
│
├── docs/                           # More documentation
└── .github/workflows/              # build.yml (build + unit tests), release.yml (packaging and release)
```

Source paths in the other documents (`Views/`, `Helpers/` and so on) are relative to `src/ExcelPasteTool/`.

## More documentation

These documents are in Traditional Chinese:

- [Project setup guide](docs/SETUP.md): settings fields, theme and font mechanics, adding a theme or a language, FAQ.
- [Project structure](docs/project_structure.md): folder layout, common commands, what to update when you move files.
- [Automatic updates](docs/AutoUpdate.md)
- [UI tests](docs/UI-TESTING.md)
