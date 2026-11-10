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
| 目標框架 | .NET 8（`net8.0`） |
| 架構模式 | MVVM（Views / ViewModels / Models / Services / Helpers） |
| 版本號 | 定義於 `ExcelPasteTool.csproj`，目前為 `1.0.0` |

---

## 2. 環境需求

### 2.1 必要工具

| 工具 | 版本 | 說明 |
| --- | --- | --- |
| .NET SDK | **8.0.413**（由 `global.json` 鎖定） | 見下方「SDK 版本說明」 |
| Git | 任意近期版本 | 取得原始碼 |

### 2.2 建議 IDE（擇一）

- **Visual Studio 2022**（17.14 以上，方案檔 `ExcelPasteTool.sln` 由此版本建立），並安裝「.NET 桌面開發」工作負載與 [Avalonia for Visual Studio](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS) 擴充套件（提供 AXAML 預覽）。
- **JetBrains Rider**，內建 Avalonia 支援（建議另裝 AvaloniaRider 外掛）。
- **VS Code**，安裝 C# Dev Kit 與 Avalonia for VS Code 擴充套件。

### 2.3 SDK 版本說明

`global.json` 指定：

```json
{ "sdk": { "version": "8.0.413" } }
```

未指定 `rollForward`，因此預設行為為 `latestPatch`：只要本機裝有 **8.0.4xx** 系列中 ≥ 413 的 SDK 即可建置。若出現

```
A compatible .NET SDK was not found. Requested SDK version: 8.0.413
```

請至 <https://dotnet.microsoft.com/download/dotnet/8.0> 下載安裝 8.0.4xx SDK，或用下列指令確認本機已安裝的版本：

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

建置輸出位於 `bin/<Configuration>/net8.0/`。

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

> **重要**：設定檔 `config.json` 是以「目前工作目錄」的相對路徑讀寫（見第 5.1 節）。`dotnet run` 在專案根目錄執行時，讀寫的就是根目錄下的 `config.json`；在 IDE 中偵錯時，工作目錄通常是 `bin/Debug/net8.0/`，此時會讀寫該目錄下的 `config.json`。

### 4.1 啟動流程

1. `Program.Main` 建立 Avalonia `AppBuilder`（`UsePlatformDetect()` 自動選擇平台後端）。
2. `App.OnFrameworkInitializationCompleted()`：
   1. `ConfigServices.Load()` 讀取 `config.json`。
   2. 依設定套用語言（`LanguageManager.CurrentLanguage`）與主題（`ThemeManager.CurrentTheme`）。
   3. 建立 `MainWindow`，套用主題樣式並初始化語言文字。

---

## 5. 設定說明

### 5.1 使用者設定檔 `config.json`

```json
{
  "Language": "TraditionalChinese",
  "Theme": "Dark"
}
```

| 欄位 | 可用值 | 預設值 | 對應列舉 |
| --- | --- | --- | --- |
| `Language` | `TraditionalChinese`、`SimplifiedChinese`、`English` | `TraditionalChinese` | `AppLanguage`（`Helpers/LanguageManager.cs`） |
| `Theme` | `Dark`、`Light` | `Dark` | `AppTheme`（`Helpers/ThemeManager.cs`） |

行為說明（`Services/ConfigServices.cs`）：

- 路徑為相對路徑 `config.json`，**相對於程式啟動時的工作目錄**，而非執行檔所在目錄。
- 檔案不存在時：使用預設值，並在主控台輸出警告；使用者在設定頁變更語言或主題後會自動建立檔案。
- 檔案格式錯誤或值無法解析時：靜默忽略，使用預設值。
- `config.json` **目前沒有設定成隨建置複製到輸出目錄**，發佈後的程式第一次啟動會使用預設值。如需預設帶入，可在 `.csproj` 加入：
  ```xml
  <ItemGroup>
    <None Update="config.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
  ```

### 5.2 主題

- 主題樣式檔位於 `Assets/Styles/`：
  - `DarkTheme.axaml`：暗黑模式
  - `LightTheme.axaml`：光線模式
  - `SidebarTheme.axaml`：側邊欄樣式（由 `MainWindow` 載入）
- `ThemeManager.ApplyTheme()` 會同時切換 Fluent 內建的 `ThemeVariant`（讓 TextBox、ComboBox、Popup 等內建控件同步）並替換自訂樣式。
- **新增主題的步驟**：
  1. 在 `Assets/Styles/` 新增 `XxxTheme.axaml`，並於 `.csproj` 加入 `<AvaloniaResource Include="Assets\Styles\XxxTheme.axaml" />`。
  2. 在 `AppTheme` 列舉加入新值。
  3. 在 `ThemeManager.ApplyTheme()` 的移除清單與載入路徑 `switch` 加入新主題；在 `GetThemeName()`、`GetAllThemes()` 加入顯示名稱。
  4. 在 `LanguageManager` 三種語言的字典中加入主題名稱字串。

### 5.3 字型

- 樣式中指定的字型順序為 `Noto Sans TC, Source Han Sans TC, Microsoft JhengHei, Segoe UI, Arial`，程式碼輸入框使用 `Consolas`。
- `Assets/Fonts/` 內雖附有 Noto Sans TC 字型檔，但**目前未在 `.csproj` 設為 `AvaloniaResource`，也未註冊為內嵌字型**，實際上會使用系統已安裝的字型。在未安裝這些字型的 Linux / macOS 上，中文可能顯示為預設字型。

### 5.4 多國語系

- 支援繁體中文、簡體中文、英文。
- 目前畫面文字來源是 `Helpers/LanguageManager.cs` 內的字典（`GetTraditionalChineseTexts()` 等）。
- `Resources/Languages/*.json` 已設為 `AvaloniaResource` 打包，但程式尚未讀取這些 JSON。**新增或修改文字時，請以 `LanguageManager.cs` 為準**，並同步更新 JSON 以免日後切換來源時不一致。
- **新增語言的步驟**：在 `AppLanguage` 加值 → 在 `LanguageManager` 新增對應字典與 `LoadLanguage()` 分支、`GetLanguageName()`、`GetLanguageFontFamily()` → 在 `FontManager.GetCodeFontFamily()` 加入對應字型。

### 5.5 組件名稱與資源路徑

`.csproj` 中 `AssemblyName` 為 `DataSplitter`，Avalonia 資源 URI 依此組成：

```
avares://DataSplitter/Assets/Styles/DarkTheme.axaml
```

- `ThemeManager`、`MainWindow` 透過 `Global.AssemblyName` 動態取得名稱。
- 但 **`App.axaml` 內寫死了 `avares://DataSplitter/...`**。若修改 `AssemblyName`，必須同步修改 `App.axaml`，否則啟動時會找不到樣式。

### 5.6 版本資訊

版本號定義於 `ExcelPasteTool.csproj`：

```xml
<AssemblyVersion>1.0.0.0</AssemblyVersion>
<AssemblyFileVersion>1.0.0.0</AssemblyFileVersion>
<AssemblyInformationalVersion>1.0.0</AssemblyInformationalVersion>
```

「關於」頁面（`Views/AboutView.axaml.cs`）顯示的是 `AssemblyVersion` 的前三碼（例如 `v1.0.0`）。**發佈新版時請三個欄位一起更新**。

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
| `--self-contained false` | 不含執行階段，體積小，但使用者需自行安裝 .NET 8 Desktop Runtime |

### 6.2 發佈設定檔（PublishProfiles）

`README.md` 中提到 `Properties/PublishProfiles/*.pubxml` 的一鍵發佈方式，但 `.gitignore` 排除了 `*.pubxml`，**這些設定檔不在版本庫中**。若要使用，請自行在本機建立，例如 `Properties/PublishProfiles/Windows-x64.pubxml`：

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

- **macOS**：`dotnet publish` 產出的是可執行檔，不是 `.app`。若要發佈給一般使用者，需另外建立 `.app` 套件結構（`Info.plist`、圖示 `.icns`）並視需要進行簽章與公證（notarization），否則 Gatekeeper 會阻擋。
- **Linux**：發佈後需給予執行權限：`chmod +x artifacts/linux-x64/DataSplitter`。
- **Windows**：未簽章的執行檔第一次執行可能出現 SmartScreen 警告，屬正常現象。

### 6.4 GitHub Release 建議流程

1. 更新 `.csproj` 的三個版本欄位。
2. 依 6.1 產出各平台檔案，並分別壓縮（例如 `DataSplitter-1.0.1-win-x64.zip`）。
3. 建立 Git 標籤 `v1.0.1` 並推送：
   ```bash
   git tag v1.0.1
   git push origin v1.0.1
   ```
4. 在 GitHub 的 Releases 頁面以該標籤建立 Release 並上傳壓縮檔。

> 標籤統一使用 `v主.次.修` 格式，方便日後實作「檢查新版本」功能時比對版本。

---

## 7. 目錄結構速覽

| 目錄 / 檔案 | 用途 |
| --- | --- |
| `Program.cs`、`App.axaml(.cs)` | 程式進入點與應用程式初始化 |
| `Views/` | 畫面（`MainWindow`、`DataToolView`、`SettingsView`、`AboutView`） |
| `ViewModels/` | 畫面邏輯（`SettingsViewModel`、`SidebarViewModel`） |
| `Models/` | 資料模型（`DataItem`、`SidebarItem`） |
| `Services/` | 設定檔讀寫（`ConfigServices`） |
| `Helpers/` | 主題、語言、字型、圖示、Toast 提示等工具類 |
| `Converters/` | Binding 用的 `IValueConverter` |
| `Assets/` | 樣式、字型、圖片、圖示 |
| `Resources/Languages/` | 語系 JSON（目前未被程式讀取，見 5.4） |
| `docs/` | 補充文件 |

詳細結構請見 [README.md](../README.md)。

---

## 8. 常見問題

**Q1. 建置出現 `A compatible .NET SDK was not found`。**
本機沒有 8.0.4xx 系列 SDK，請依 2.3 安裝。

**Q2. 出現 NU1008「Projects that use central package version management should not define the version on the PackageReference items」。**
`.csproj` 的 `PackageReference` 不可帶 `Version`，請把版本移到 `Directory.Packages.props`（見 3.1）。

**Q3. 修改設定後重開程式，設定沒有保留。**
`config.json` 依工作目錄讀寫。從不同目錄啟動（例如捷徑的「開始位置」不同）會讀到不同的檔案，請確認啟動時的工作目錄。

**Q4. 改了 `AssemblyName` 之後程式啟動就沒有樣式或崩潰。**
`App.axaml` 內的 `avares://DataSplitter/...` 需一併修改（見 5.5）。

**Q5. 部分舊檔案的中文註解在編輯器中顯示為亂碼。**
專案中約有 15 個檔案是以 Big5 編碼儲存（例如 `App.axaml.cs`、`Services/ConfigServices.cs`、`Helpers/ThemeManager.cs`、`Directory.Packages.props`、`docs/ConfigService.md`、`AI_CONTEXT.md`），其他檔案是 UTF-8。編譯不受影響（只有註解與字串常值含中文），但在預設 UTF-8 的編輯器或 GitHub 網頁上會顯示亂碼。請以 Big5 開啟這些檔案閱讀；新檔案一律使用 **UTF-8** 儲存，建議日後將舊檔統一轉為 UTF-8。
