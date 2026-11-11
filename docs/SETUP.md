# 專案配置說明書

本文件說明 ExcelPasteTool（產品名稱 **DataSplitter Pro**）的開發環境需求、建置、執行、設定檔與發佈方式。適用對象為第一次接手本專案的開發人員，或需要重新建立開發環境時參考。

---

## 1. 專案概要

| 項目 | 內容 |
| --- | --- |
| 專案名稱 | ExcelPasteTool |
| 產品名稱 | DataSplitter Pro |
| 執行檔名稱（AssemblyName） | `DataSplitter`（Windows 為 `DataSplitter.exe`） |
| 類型 | 跨平台桌面應用程式（Windows / macOS / Linux） |
| UI 框架 | [Avalonia UI](https://avaloniaui.net/) 11.3.3（Fluent 主題 + DataGrid） |
| 目標框架 | .NET 10（`net10.0`，LTS） |
| 架構模式 | MVVM（Views / ViewModels / Models / Services / Helpers） |
| 版本號 | 定義於 `ExcelPasteTool.csproj`，目前為 `1.5.4` |

---

## 2. 環境需求

### 2.1 必要工具

| 工具 | 版本 | 說明 |
| --- | --- | --- |
| .NET SDK | **10.0.100 以上**（由 `global.json` 指定） | 見下方「SDK 版本說明」 |
| Git | 任意近期版本 | 取得原始碼 |

### 2.2 建議 IDE（擇一）

- **Visual Studio 2026**（18.0 以上；Visual Studio 2022 不支援 .NET 10），並安裝「.NET 桌面開發」工作負載與 [Avalonia for Visual Studio](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS) 擴充套件（提供 AXAML 預覽）。
- **JetBrains Rider**，內建 Avalonia 支援（建議另裝 AvaloniaRider 外掛）。
- **VS Code**，安裝 C# Dev Kit 與 Avalonia for VS Code 擴充套件。

### 2.3 SDK 版本說明

`global.json` 指定：

```json
{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }
```

`rollForward: latestFeature` 表示本機只要裝有 **10.0 系列**中 ≥ 10.0.100 的任一 SDK（10.0.1xx、10.0.2xx…）即可建置；CI（`actions/setup-dotnet`）也會依此安裝最新的 10.0 SDK。若出現

```
A compatible .NET SDK was not found. Requested SDK version: 10.0.100
```

請至 <https://dotnet.microsoft.com/download/dotnet/10.0> 下載安裝 .NET 10 SDK，或用下列指令確認本機已安裝的版本：

```bash
dotnet --list-sdks
```

### 2.4 作業系統注意事項

- **Windows**：無額外需求。
- **macOS**：Apple Silicon（M1/M2/M3）請使用 `osx-arm64`，Intel 機種使用 `osx-x64`。
- **Linux**：需要 X11 與基本字型套件，例如 Debian/Ubuntu：
  ```bash
  sudo apt install libx11-6 libice6 libsm6 libfontconfig1 fonts-noto-cjk
  ```
  `fonts-noto-cjk` 用於正確顯示中文（見第 5.3 節）。

---

## 3. 取得原始碼與建置

```bash
git clone https://github.com/m121752332/ExcelPasteTool.git
cd ExcelPasteTool

# 還原 NuGet 套件
dotnet restore

# 建置（Debug）
dotnet build

# 建置（Release）
dotnet build -c Release
```

建置輸出位於 `bin/<Configuration>/net10.0/`。

### 3.1 NuGet 套件版本管理（Central Package Management）

本專案啟用了集中式套件版本管理，**`.csproj` 內的 `PackageReference` 不寫版本號**，版本統一在 `Directory.Packages.props` 管理：

| 套件 | 版本 |
| --- | --- |
| Avalonia | `$(AvaloniaVersion)` = 11.3.3 |
| Avalonia.Desktop | 同上 |
| Avalonia.Controls.DataGrid | 同上 |
| Avalonia.Themes.Fluent | 同上 |
| System.ComponentModel.Annotations | 5.0.0 |

- **升級 Avalonia**：只要修改 `Directory.Packages.props` 中的 `<AvaloniaVersion>`，四個 Avalonia 套件會一起升級，避免版本不一致。
- **新增套件**：先在 `Directory.Packages.props` 加入 `<PackageVersion Include="套件名" Version="x.y.z" />`，再於 `.csproj` 加入不含版本的 `<PackageReference Include="套件名" />`。若直接用 `dotnet add package`，請確認版本有寫到 `Directory.Packages.props`，否則會出現 NU1008 / NU1010 錯誤。

---

## 4. 執行

```bash
dotnet run
```

或在 IDE 中將 `ExcelPasteTool` 設為啟動專案後按 F5。

> **注意**：設定檔 `config.json` 固定讀寫「執行檔所在目錄」（`AppContext.BaseDirectory`）。開發時用 `dotnet run` 或 IDE 偵錯，讀寫的是 `bin/<Configuration>/net10.0/config.json`，**不是**專案根目錄那份（見第 5.1 節）。

### 4.1 啟動流程

1. `Program.Main` 建立 Avalonia `AppBuilder`（`UsePlatformDetect()` 自動選擇平台後端）。
2. `App.OnFrameworkInitializationCompleted()`：
   1. `ConfigServices.Load()` 讀取 `config.json`。
   2. 依設定套用語言（`LanguageManager.CurrentLanguage`）與主題（`ThemeManager.CurrentTheme`）。
   3. 在建立視窗前先套用主題配色（`ThemeManager.ApplyTheme`）與字型／字體大小（`FontManager.Apply`），避免第一個畫面閃爍。
   4. 初始化語言文字，建立 `MainWindow`。
3. 啟動後檢查 GitHub 是否有新版本（見 5.7）。

---

## 5. 設定說明

### 5.1 使用者設定檔 `config.json`

```json
{
  "Language": "TraditionalChinese",
  "Theme": "Dark",
  "FontFamily": "",
  "FontSize": 14
}
```

| 欄位 | 可用值 | 預設值 | 說明 |
| --- | --- | --- | --- |
| `Language` | `TraditionalChinese`、`SimplifiedChinese`、`English` | `TraditionalChinese` | 對應 `AppLanguage`（`Helpers/LanguageManager.cs`） |
| `Theme` | `Dark`、`Light`、`Tiffany`、`Rainbow` | `Dark` | 對應 `AppTheme`（`Helpers/ThemeManager.cs`）。`Dark` 現在是 GitHub Dark 配色，名稱保留以相容舊設定 |
| `FontFamily` | 空字串，或系統已安裝的字型名稱 | `""` | 空字串代表使用內建的 Noto Sans TC |
| `FontSize` | 10 ～ 24 | `14` | 基本字體大小，超出範圍會被夾到上下限並四捨五入 |

| `TourCompleted` | `true`、`false` | `false` | 是否已看過 App 導覽。`false`（或舊設定檔沒有此欄位）時，啟動後會自動播放一次導覽，看完或略過即寫回 `true`；改回 `false` 可讓下次啟動再播一次 |
| `CompactTitleBar` | `true`、`false` | `false` | 是否使用精簡標題列（隱藏系統標題列，只留側邊欄開關與最小化 / 最大化 / 關閉），可在「設定」頁切換，實作在 `Views/MainWindow.TitleBar.cs` |

快捷鍵（`Views/MainWindow.TitleBar.cs`，只在對應按鈕的 ToolTip 中提示，說明頁 `#shortcut-map` 有圖文對照）：`Alt+F4` 強制關閉、`Alt+Q` 開關側邊欄、`Alt+P` 把剪貼簿貼到「工具」頁右側資料表。

前四項都可以在 App 的「設定」頁變更，變更後會自動存檔。導覽可隨時在「工具」頁按右上角的「導覽」按鈕重看（其他頁面不顯示此按鈕）；Toast 通知統一放在主視窗最上層（`MainWindow.axaml` 的 `ToastPanel`）（實作在 `Controls/TourOverlay.cs` 與 `Views/MainWindow.Tour.cs`，步驟文字在 `Helpers/LanguageManager.cs` 的 `Tour*` 鍵）。

行為說明（`Services/ConfigServices.cs`）：

- 路徑為 `Path.Combine(AppContext.BaseDirectory, "config.json")`，**固定在執行檔旁邊**，不受啟動時工作目錄影響。
- 檔案不存在時：使用預設值，並在主控台輸出警告；使用者在設定頁做任何變更後會自動建立檔案。
- 檔案格式錯誤或值無法解析時：靜默忽略，使用預設值。
- 專案根目錄的 `config.json` 只是預設範本：建置時**不會**自動複製到輸出目錄，但 `release.yml` 打包時會把它複製進每個平台的 zip。
- 執行檔所在目錄必須可寫入，否則設定無法儲存（例如安裝在 `C:\Program Files\` 下時）。

### 5.2 主題

主題分成「配色」與「樣式」兩層：

| 檔案 | 內容 |
| --- | --- |
| `Assets/Themes/GitHubDarkPalette.axaml` | GitHub Dark 配色（`AppTheme.Dark`） |
| `Assets/Themes/LightPalette.axaml` | 光線模式配色（`AppTheme.Light`） |
| `Assets/Themes/TiffanyPalette.axaml` | 蒂芬妮藍配色（`AppTheme.Tiffany`） |
| `Assets/Themes/RainbowPalette.axaml` | 彩虹幻境配色（`AppTheme.Rainbow`，實驗性；Token 可以是漸層筆刷，不限單色） |
| `Assets/Styles/AppStyles.axaml` | 所有主題共用的版面樣式 |

- 配色檔是 `ResourceDictionary`，**只放顏色 Token**（例如 `WindowBackgroundBrush`、`AccentBrush`、`TableEvenRowBrush`），不放樣式。
- `AppStyles.axaml` 以 `DynamicResource` 參照這些 Token，因此切換主題時只要換掉配色檔，畫面就會自動更新。
- `ThemeManager.ApplyTheme()` 會：
  1. 設定 Fluent 的 `RequestedThemeVariant`（`Dark`、`Rainbow` 用暗色底版，`Light`、`Tiffany` 用亮色底版），讓下拉選單、捲軸、Slider 等內建控件同步。
  2. 從 `Application.Resources.MergedDictionaries` 移除 `Assets/Themes/` 下的舊配色，再加入新主題的配色檔。
- `App.axaml` 中預設載入 `GitHubDarkPalette.axaml`，啟動後才依 `config.json` 替換。

**新增主題的步驟**：

1. 複製一份現有的配色檔，例如 `Assets/Themes/XxxPalette.axaml`，修改顏色值。**每個 Token 都要定義**，缺少的 Token 會讓對應控件失去顏色。
2. 在 `AppTheme` 列舉加入新值（列舉名稱會寫入 `config.json`，日後請勿更名）。
3. 在 `ThemeManager` 中：`GetPaletteFile()` 對應到新檔案、`GetVariant()` 決定用亮色或暗色底版、`GetThemeName()` 加入顯示名稱、`GetAllThemes()` 加入下拉選單順序。
4. 在 `LanguageManager` 三種語言的字典中加入主題名稱字串（例如 `ThemeXxx`）。
5. 若要新增一個顏色 Token，必須在**所有**配色檔都加上，再到 `AppStyles.axaml` 使用。

`.axaml` 檔案會被 Avalonia 預設編譯進組件，不需要在 `.csproj` 另外加入。

### 5.3 字型與字體大小

- 內建字型為 **Noto Sans TC**：`Assets/Fonts/NotoSansTC-Regular.ttf` 與 `NotoSansTC-Bold.ttf` 已在 `.csproj` 設為 `AvaloniaResource`，打包進程式，各平台顯示一致，不依賴系統字型。
- `Assets/Fonts/` 內其他粗細（Thin、Light、Medium、SemiBold、ExtraBold、Black）**沒有打包**。如需使用，請在 `.csproj` 加入對應的 `AvaloniaResource`，但會增加執行檔大小。
- 使用者可在設定頁從系統字型清單（`FontManager.GetSystemFontFamilies()`）選擇其他字型，或調整字體大小（10 ～ 24）。
- 套用方式（`Helpers/FontManager.cs` 的 `Apply()`）是改寫 `Application.Resources` 中的資源：

  | 資源鍵 | 值 |
  | --- | --- |
  | `AppFontFamily` | 目前字型 |
  | `AppFontSize` | 基本大小 |
  | `AppFontSizeSmall` | 基本大小 − 2（最小 9） |
  | `AppFontSizeLarge` | 基本大小 + 2 |
  | `AppFontSizeTitle` | 基本大小 × 1.8 |
  | `ContentControlThemeFontFamily`、`ControlContentThemeFontSize` | 同步給 Fluent 內建控件 |

- 新增畫面時，文字請用 `{DynamicResource AppFontSize}` 這類資源，**不要寫死 `FontSize` 數值**，否則不會跟著設定變大或變小。

### 5.4 多國語系

- 支援繁體中文、簡體中文、英文。
- 目前畫面文字來源是 `Helpers/LanguageManager.cs` 內的字典（`GetTraditionalChineseTexts()` 等）。
- `Resources/Languages/*.json` 已設為 `AvaloniaResource` 打包，但程式尚未讀取這些 JSON。**新增或修改文字時，請以 `LanguageManager.cs` 為準**，並同步更新 JSON 以免日後切換來源時不一致。
- **新增語言的步驟**：在 `AppLanguage` 加值 → 在 `LanguageManager` 新增對應字典與 `LoadLanguage()` 分支、`GetLanguageName()`、`GetLanguageFontFamily()` → 在 `FontManager.GetCodeFontFamily()` 加入對應字型。

### 5.5 組件名稱與資源路徑

`.csproj` 中 `AssemblyName` 為 `DataSplitter`，Avalonia 資源 URI 依此組成：

```
avares://DataSplitter/Assets/Themes/GitHubDarkPalette.axaml
avares://DataSplitter/Assets/Fonts#Noto Sans TC
```

- `ThemeManager`、`FontManager` 透過 `Global.AssemblyName` 動態取得名稱。
- 但 **`App.axaml` 內寫死了 `avares://DataSplitter/...`**（預設配色、共用樣式、預設字型）。若修改 `AssemblyName`，必須同步修改 `App.axaml`，否則啟動時會找不到樣式與字型。自動更新與 Release zip 檔名也使用 `DataSplitter`，改名前請一併評估。

### 5.6 版本資訊

版本號定義於 `ExcelPasteTool.csproj`：

```xml
<Version>1.3.0</Version>
<AssemblyVersion>1.3.0.0</AssemblyVersion>
<FileVersion>1.3.0.0</FileVersion>
<InformationalVersion>1.3.0</InformationalVersion>
<IncludeSourceRevisionInInformationalVersion>false</IncludeSourceRevisionInInformationalVersion>
```

- 程式內的「目前版本」由 `Services/UpdateService.cs` 的 `CurrentVersion` 取得，來源是 `InformationalVersion`（取不到時退回 `AssemblyVersion`）。「關於」頁面與自動更新都使用這個值。
- `IncludeSourceRevisionInInformationalVersion=false` 是為了避免 SDK 在版本後面附加 `+commit` 雜湊，影響版本比對，請勿移除。
- 本機手動發佈時，請四個欄位一起更新；透過 GitHub Actions 推 tag 發佈時，workflow 會依 tag 覆寫版本（見 6.4），但仍建議把 csproj 同步改成新版本號。

### 5.7 自動更新

程式啟動時會查詢 GitHub 的 `releases/latest`（`m121752332/ExcelPasteTool`），若 tag 版本大於目前版本，就提示使用者更新。

- 只會偵測正式 Release：**Draft 與 Pre-release 不會被偵測到**。
- tag 可寫成 `1.1.0` 或 `v1.1.0`。
- Release 附件必須是 `.zip`，檔名需包含平台與架構，程式依此挑選對應檔案：
  `DataSplitter_win_x64.zip`、`DataSplitter_linux_x64.zip`、`DataSplitter_osx_x64.zip`、`DataSplitter_osx_arm64.zip`。
- 找不到對應的 zip（例如舊版 v1.0.x 的 `.rar` 附件）時，只能提示使用者手動下載。
- 下載與解壓縮的暫存目錄為系統暫存資料夾下的 `DataSplitter_update/`。

---

## 6. 發佈

### 6.1 單一執行檔（自含 .NET 執行階段）

使用者電腦不需安裝 .NET：

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/win-x64

# macOS x64（Intel）
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/osx-x64

# macOS ARM64（M1/M2/M3）
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o artifacts/osx-arm64

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/linux-x64
```

輸出目錄 `artifacts/` 已列入 `.gitignore`，不會被提交。

可選參數：

| 參數 | 效果 |
| --- | --- |
| `-p:IncludeNativeLibrariesForSelfExtract=true` | 把 Avalonia 的原生函式庫（如 Skia）一起包進單一檔案，輸出目錄只剩一個執行檔 |
| `-p:EnableCompressionInSingleFile=true` | 壓縮單一檔案，縮小體積，啟動稍慢 |
| `--self-contained false` | 不含執行階段，體積小，但使用者需自行安裝 .NET 10 Runtime（Avalonia 不需要僅供 WPF/WinForms 使用的 Desktop Runtime） |

### 6.2 發佈設定檔（PublishProfiles）

也可以用 `Properties/PublishProfiles/*.pubxml` 做一鍵發佈，但 `.gitignore` 排除了 `*.pubxml`，**這些設定檔不在版本庫中**。若要使用，請自行在本機建立，例如 `Properties/PublishProfiles/Windows-x64.pubxml`：

```xml
<Project>
  <PropertyGroup>
    <Configuration>Release</Configuration>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <PublishDir>artifacts\win-x64\</PublishDir>
  </PropertyGroup>
</Project>
```

然後執行：

```bash
dotnet publish -p:PublishProfile=Properties/PublishProfiles/Windows-x64.pubxml
```

若希望團隊共用，可在 `.gitignore` 中加入 `!Properties/PublishProfiles/*.pubxml` 例外規則後提交。

### 6.3 各平台後續處理

- **macOS**：`dotnet publish` 產出的是可執行檔，不是 `.app`；直接雙擊會先開「終端機」。release workflow 會在 macOS runner 上把它包成 `DataSplitter.app`（`packaging/macos/` 內有 `Info.plist` 範本、圖示 `DataSplitter.icns` 與相容啟動腳本），並用 `codesign --sign -` 對整個 `.app` 做 ad-hoc 簽章。zip 結構與自動更新的關係見 `docs/AutoUpdate.md`。
  - .NET 10 SDK 即使在 Linux 上交叉發佈，也會自動替 `osx-*` 執行檔加上 **ad-hoc 簽章**（Apple Silicon 必須至少有 ad-hoc 簽章才能執行）。release workflow 會檢查這一點，缺簽章時 Release 會失敗。
  - ad-hoc 簽章**不能**通過 Gatekeeper。用瀏覽器下載的檔案帶有 `com.apple.quarantine` 屬性，第一次開啟會出現「來自未識別的開發者」，使用者需執行 `xattr -dr com.apple.quarantine <資料夾>` 或到「系統設定 > 隱私權與安全性」按「強制打開」（見 README 與 zip 內的 `README_macOS.txt`）。
  - 程式內自動更新是用 HttpClient 下載，不會帶 quarantine 屬性，所以更新後的新版不會被擋。
  - 要讓使用者雙擊就能開，需要付費的 Apple Developer Program 帳號，用 Developer ID Application 憑證簽署（含 hardened runtime）並送 Apple 公證（notarization）。這需要在 macOS runner 上執行 `codesign` 與 `xcrun notarytool`，憑證與帳號放在 GitHub Secrets。
  - 改成 `.app` 套件本身不會解決 Gatekeeper 問題；而且自動更新目前要求 zip 根目錄（或唯一子資料夾）直接有 `DataSplitter` 執行檔，改格式前要先調整 `UpdateService.PrepareUpdate`，並考慮舊版的升級路徑。
- **Linux**：發佈後需給予執行權限：`chmod +x artifacts/linux-x64/DataSplitter`。
- **Windows**：未簽章的執行檔第一次執行可能出現 SmartScreen 警告，屬正常現象。

### 6.4 GitHub Actions 與 Release 流程

專案有兩個 workflow（`.github/workflows/`）：

| 檔案 | 觸發時機 | 動作 |
| --- | --- | --- |
| `build.yml` | 開 PR、推送到 `master` | `dotnet build -c Release` 確認可編譯 |
| `release.yml` | 推送版本 tag（`1.2.3` 或 `v1.2.3`） | 四個平台各自 `dotnet publish`（自含、單一檔案、內含原生函式庫），附上 `config.json`，打包成 `DataSplitter_<平台>_<架構>.zip`，並建立 GitHub Release 上傳 |

**.NET runtime 檢查**：Release 的 zip 是自包含（self-contained）單一檔案，已內含 .NET 10 runtime，使用者電腦**不需要**另外安裝 .NET。`release.yml` 每個平台發佈後會執行「Verify bundled .NET 10 runtime」步驟，讀取 `DataSplitter.runtimeconfig.json` 確認內含 `Microsoft.NETCore.App 10.x`；若有人誤改成 `--self-contained false` 或目標框架不是 `net10.0`，Release 會直接失敗，不會發出需要使用者自行裝 runtime 的版本。因為 runtime 跟著執行檔一起更新，舊版（.NET 8 打包的 1.3.0 以前）透過自動更新換成 1.4.0 也不需要任何額外安裝。

推新版的步驟：

1. 將 `.csproj` 的版本欄位改成新版本（例如 `1.3.0`），合併到 `master`。
2. 建立 tag 並推送：
   ```bash
   git checkout master && git pull
   git tag 1.3.0
   git push origin 1.3.0
   ```
3. 等 `Release` workflow 跑完，到 GitHub 的 Releases 頁面確認四個 zip 都已上傳。workflow 會自動產生 Release Notes，可再手動補充說明。
4. 已安裝舊版的使用者下次啟動程式時，就會收到更新提示（見 5.7）。

> 不要把 Release 設為 Draft 或 Pre-release，也不要改 zip 檔名，否則自動更新偵測不到。

---

## 7. 目錄結構速覽

| 目錄 / 檔案 | 用途 |
| --- | --- |
| `Program.cs`、`App.axaml(.cs)` | 程式進入點與應用程式初始化 |
| `Views/` | 畫面（`MainWindow`、`DataToolView`、`SettingsView`、`AboutView`） |
| `ViewModels/` | 畫面邏輯（`SettingsViewModel`、`SidebarViewModel`） |
| `Models/` | 資料模型（`DataItem`、`SidebarItem`） |
| `Services/` | 設定檔讀寫（`ConfigServices`）、版本檢查與自動更新（`UpdateService`） |
| `Helpers/` | 主題、語言、字型、圖示、Toast 提示等工具類 |
| `Converters/` | Binding 用的 `IValueConverter` |
| `Assets/Themes/` | 各主題配色（只放顏色） |
| `Assets/Styles/` | 共用樣式 `AppStyles.axaml` |
| `Assets/` 其他 | 字型、圖片、圖示 |
| `Resources/Languages/` | 語系 JSON（目前未被程式讀取，見 5.4） |
| `docs/` | 補充文件 |
| `.github/workflows/` | CI 建置與 Release 自動發佈（見 6.4） |

詳細結構請見 [README.md](../README.md)。

---

## 8. 常見問題

**Q1. 建置出現 `A compatible .NET SDK was not found`。**
本機沒有 .NET 10 SDK（10.0.100 以上），請依 2.3 安裝。

**Q2. 出現 NU1008「Projects that use central package version management should not define the version on the PackageReference items」。**
`.csproj` 的 `PackageReference` 不可帶 `Version`，請把版本移到 `Directory.Packages.props`（見 3.1）。

**Q3. 修改設定後重開程式，設定沒有保留。**
`config.json` 存在執行檔旁邊。請確認該目錄可寫入；開發時要看的是 `bin/<Configuration>/net10.0/config.json`，不是專案根目錄那份。

**Q4. 改了 `AssemblyName` 之後程式啟動就沒有樣式或崩潰。**
`App.axaml` 內的 `avares://DataSplitter/...` 需一併修改（見 5.5）。

**Q5. 切換字體大小後，某些文字沒有跟著變。**
該處的 `FontSize` 寫死了數值，請改用 `{DynamicResource AppFontSize}`（或 `AppFontSizeSmall` / `AppFontSizeLarge` / `AppFontSizeTitle`），見 5.3。

**Q6. 新主題的某些區塊沒有顏色或顏色怪異。**
新配色檔缺少某些顏色 Token。請和 `GitHubDarkPalette.axaml` 逐項比對，確認每個 `x:Key` 都有定義，見 5.2。

**Q7. 檔案編碼。**
程式碼已統一為 **UTF-8**。目前只剩 `AI_CONTEXT.md`、`docs/ConfigService.md`、`docs/SidebarItem.md` 三份舊文件仍是 Big5，在 GitHub 上會顯示亂碼，閱讀時請用 Big5 開啟。新增或修改檔案一律使用 UTF-8。
