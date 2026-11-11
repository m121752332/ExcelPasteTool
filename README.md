# DataSplitter Pro（ExcelPasteTool）

把 Excel 的一欄資料，一鍵轉成「分隔符號字串」和「SQL `IN` 條件」的跨平台桌面小工具。

從 Excel 複製一欄料號、單號或代碼，按下「資料貼上」，就會同時得到：

| 輸出 | 範例 | 常見用途 |
| --- | --- | --- |
| 分隔資料 | `A\|B\|C\|D` | 貼到 ERP 或報表的多值查詢欄位 |
| SQL 條件 | `('A', 'B', 'C', 'D')` | 直接接在 `WHERE 欄位 IN` 後面 |
| 資料表 | 逐筆列出並編號 | 檢查筆數、移除不要的項目 |

- 專案名稱是 `ExcelPasteTool`，產品名稱是 **DataSplitter Pro**，執行檔叫 `DataSplitter`。
- 支援 Windows、macOS、Linux，以 [.NET 10](https://dotnet.microsoft.com/) 和 [Avalonia UI](https://avaloniaui.net/) 11 開發。

## 目錄

- [功能](#功能)
- [下載與安裝](#下載與安裝)
- [使用方式](#使用方式)
- [開發環境部署](#開發環境部署)
- [建置與執行](#建置與執行)
- [打包發佈](#打包發佈)
- [測試](#測試)
- [專案結構](#專案結構)
- [相關文件](#相關文件)

## 功能

- **貼上即轉換**：從剪貼簿讀取資料，每一行只取第一欄，自動去除空白行與重複值。
- **三個區塊互相同步**：分隔資料、SQL 條件、資料表任一處變動，另外兩處會跟著更新。
- **可直接編輯**：在「分隔資料」框裡手動輸入或修改，資料表與 SQL 條件會即時重算。
- **單筆移除**：滑鼠移到資料表的某一列，右側會出現 ✕，點一下就移除該筆。
- **四種分隔符號**：豎線 `|`、分號 `;`、冒號 `:`、空白。
- **一鍵複製**：分隔資料、SQL 條件各有複製按鈕；點資料表的「行號」表頭可複製整欄。
- **SQL 安全處理**：值裡面的單引號會自動轉成兩個單引號。
- **介面設定**：三種語言（繁體中文、簡體中文、English）、三種主題（GitHub Dark、Light、蒂芬妮藍）、可調整字型與字體大小。
- **內建使用說明**：按 `F1` 或點左側選單的「說明」，就會開啟給新手看的圖解教學，內容跟著語言與主題切換。
- **自動更新**：啟動時檢查 GitHub 是否有新版本，可在程式內下載更新。

## 下載與安裝

一般使用者不需要安裝 .NET，下載後解壓縮就能用。

1. 到 [Releases 頁面](https://github.com/m121752332/ExcelPasteTool/releases/latest) 下載對應平台的檔案：

   | 平台 | 檔案 |
   | --- | --- |
   | Windows 64 位元 | `DataSplitter_win_x64.zip` |
   | macOS（Apple Silicon，M 系列） | `DataSplitter_osx_arm64.zip` |
   | macOS（Intel） | `DataSplitter_osx_x64.zip` |
   | Linux 64 位元 | `DataSplitter_linux_x64.zip` |

2. 解壓縮到**可以寫入**的資料夾（例如桌面或文件），不要放在 `C:\Program Files\` 底下，否則設定無法儲存。
3. 執行：
   - **Windows**：雙擊 `DataSplitter.exe`。第一次執行若出現 SmartScreen 警告，按「其他資訊」→「仍要執行」。
   - **macOS**：程式沒有 Apple 開發者簽章與公證，用瀏覽器下載後第一次雙擊會出現「無法打開，因為它來自未識別的開發者」。請先做一次下面任一種方式，之後就能直接雙擊開啟：
     - **終端機（建議）**：打開「終端機」，輸入 `cd `（後面留一個空格）再把解壓後的資料夾拖進視窗、按 Enter，然後執行：

       ```bash
       xattr -dr com.apple.quarantine .
       chmod +x DataSplitter
       ./DataSplitter
       ```

     - **系統設定**：先雙擊一次讓它被擋，再到「系統設定 > 隱私權與安全性」，往下找到「已阻擋『DataSplitter』」按「強制打開」（舊版 macOS 為「仍要打開」），輸入密碼確認。

     zip 裡附有 `README_macOS.txt`，內容相同。之後透過程式內的自動更新取得的新版不會再被擋。
   - **Linux**：`chmod +x DataSplitter && ./DataSplitter`。需要 X11 與中文字型，Debian／Ubuntu 可執行：

     ```bash
     sudo apt install libx11-6 libice6 libsm6 libfontconfig1 fonts-noto-cjk
     ```

     想在程式內顯示使用說明（F1），需另外安裝 `libwebkit2gtk-4.1-0`；沒有安裝時說明會改用預設瀏覽器開啟。

之後有新版本時，程式啟動會自動提示更新。

## 使用方式

1. 在 Excel 選取一欄資料並複製（Ctrl+C）。
2. 在程式右側按「**資料貼上**」。
3. 左上的「分隔資料」和左下的「SQL 條件」會立刻產生，按各自右上角的按鈕即可複製。
4. 需要時可以：
   - 在上方工具列切換**分隔符號**。
   - 把滑鼠移到某一列，按右側的 **✕** 移除該筆。
   - 直接在「分隔資料」框裡輸入或修改內容。
   - 按「**清空資料**」回到初始狀態。

貼上多欄資料時只會取每一行的第一欄（以 Tab、逗號、分號、豎線判斷欄位）。

不確定怎麼操作時，隨時按 **F1** 開啟使用說明。說明預設顯示在程式內（使用系統內建的瀏覽器引擎：Windows 為 WebView2、macOS 為 WKWebView、Linux 為 WebKitGTK）；也可以到「設定」頁把「說明開啟方式」改成「預設瀏覽器」。電腦上沒有可用的引擎時，會自動改用預設瀏覽器開啟。

語言與主題可在工具列臨時切換；要永久保存，請到左側選單的「設定」頁調整。設定存放在執行檔旁邊的 `config.json`。

## 開發環境部署

### 需要的工具

| 工具 | 版本 | 說明 |
| --- | --- | --- |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0.100 以上的 10.0 系列 | 由 `global.json` 指定 |
| Git | 近期版本 | 取得原始碼 |

用 `dotnet --list-sdks` 可以確認本機已安裝的版本。

### IDE（擇一）

- **Visual Studio 2026**（18.0 以上，Visual Studio 2022 不支援 .NET 10）：安裝「.NET 桌面開發」工作負載和 [Avalonia for Visual Studio](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS) 擴充套件。
- **JetBrains Rider**：內建 Avalonia 支援。
- **VS Code**：安裝 C# Dev Kit 和 Avalonia for VS Code。

### 取得原始碼

```bash
git clone https://github.com/m121752332/ExcelPasteTool.git
cd ExcelPasteTool
dotnet restore
```

NuGet 套件版本集中在 `Directory.Packages.props` 管理，`.csproj` 的 `PackageReference` 不寫版本號。新增套件時兩邊都要改，否則會出現 NU1008 錯誤。

## 建置與執行

```bash
# 建置（Debug）
dotnet build

# 建置（Release）
dotnet build -c Release

# 直接執行
dotnet run
```

- 建置輸出在 `bin/<Configuration>/net10.0/`。
- 在 IDE 中將 `ExcelPasteTool` 設為啟動專案後按 F5 也可以。
- 開發時讀寫的設定檔是 `bin/<Configuration>/net10.0/config.json`，不是專案根目錄那份；根目錄的 `config.json` 只是打包時用的預設範本。

## 打包發佈

### 本機打包

產出自含 .NET 執行階段的單一執行檔，使用者電腦不需要安裝 .NET：

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/win-x64

# macOS ARM64（M 系列）
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/osx-arm64

# macOS x64（Intel）
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/osx-x64

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/linux-x64
```

輸出目錄 `artifacts/` 已列入 `.gitignore`。要交給別人使用時，把根目錄的 `config.json` 一起放進輸出資料夾。

### 發佈正式版本（GitHub Actions）

推送版本 tag 後，`release.yml` 會自動打包四個平台的 zip 並建立 GitHub Release：

1. 把 `ExcelPasteTool.csproj` 的 `Version`、`AssemblyVersion`、`FileVersion`、`InformationalVersion` 改成新版本，合併到 `master`。
2. 建立並推送 tag（`1.3.0` 或 `v1.3.0` 都可以）：

   ```bash
   git checkout master && git pull
   git tag 1.3.0
   git push origin 1.3.0
   ```

3. 等 workflow 跑完，到 Releases 頁面確認四個 zip 都已上傳。

不要把 Release 設成 Draft 或 Pre-release，也不要改 zip 檔名，否則已安裝的使用者收不到更新提示。

## 測試

**目前專案沒有單元測試專案**，`dotnet test` 不會執行任何測試。CI（`build.yml`）只在開 PR 和推送到 `master` 時確認 Release 組態可以編譯。

修改程式後，請至少手動確認以下項目：

| 項目 | 操作 | 預期結果 |
| --- | --- | --- |
| 貼上 | 從 Excel 複製含重複值與空白列的一欄，按「資料貼上」 | 資料表去除重複與空白，筆數正確 |
| 同步 | 貼上 `A`、`B`、`C`、`D` | 分隔資料為 `A\|B\|C\|D`，SQL 條件為 `('A', 'B', 'C', 'D')` |
| 單筆移除 | 移到第 2 列按 ✕ | 剩 `A`、`C`、`D`，行號重排為 1～3，另外兩個區塊同步 |
| 手動編輯 | 在分隔資料框輸入 `X\|Y` | 資料表與 SQL 條件即時更新 |
| 分隔符號 | 切換成分號 | 分隔資料變成 `A;C;D` |
| 單引號 | 貼上 `O'Neil` | SQL 條件為 `('O''Neil')` |
| 複製 | 按三個複製按鈕 | 剪貼簿內容正確，出現成功提示 |
| 清空 | 按「清空資料」 | 三個區塊清空，分隔符號回到豎線 |
| 語言與主題 | 各切換一輪 | 文字與配色全部更新，沒有殘留 |
| 設定保存 | 在設定頁改語言、主題、字型後重開程式 | 設定保留 |

資料轉換邏輯目前寫在 `Views/DataToolView.axaml.cs` 裡，和畫面綁在一起。若要加入單元測試，需要先把這段邏輯抽成獨立類別，再建立測試專案（例如 xUnit）。

## 專案結構

```text
ExcelPasteTool/
├── Program.cs                  # 程式進入點
├── App.axaml(.cs)              # 應用程式初始化（載入設定、主題、字型）
├── ExcelPasteTool.csproj       # 專案檔（版本號在這裡）
├── Directory.Packages.props    # NuGet 套件版本集中管理
├── global.json                 # 指定 .NET SDK 版本
├── config.json                 # 預設設定範本（打包時附上）
│
├── Views/                      # 畫面
│   ├── MainWindow              # 主視窗與側邊欄
│   ├── DataToolView            # 資料處理工具（主要功能）
│   ├── SettingsView            # 設定頁
│   ├── AboutView               # 關於頁
│   └── UpdateDialog            # 更新對話框
├── ViewModels/                 # 設定頁與側邊欄的畫面邏輯
├── Models/                     # 資料模型（DataItem、SidebarItem）
├── Services/                   # 設定檔讀寫、版本檢查與自動更新
├── Helpers/                    # 主題、語言、字型、圖示、Toast 提示
├── Converters/                 # Binding 轉換器
│
├── Assets/
│   ├── Themes/                 # 各主題配色（只放顏色）
│   ├── Styles/AppStyles.axaml  # 所有主題共用的樣式
│   ├── Fonts/                  # 內建字型 Noto Sans TC
│   └── Images/、Logos/         # 圖片與圖示
├── Resources/Languages/        # 語系 JSON
│
├── docs/                       # 補充文件
└── .github/workflows/          # CI 建置與 Release 自動發佈
```

畫面上的文字目前以 `Helpers/LanguageManager.cs` 內的字典為準；`Resources/Languages/*.json` 有一起打包，但程式還沒有讀取。新增文字時兩邊都要更新。

## 相關文件

- [專案配置說明書](docs/SETUP.md)：設定檔欄位、主題與字型機制、新增主題或語言的步驟、常見問題。
- [自動更新說明](docs/AutoUpdate.md)
- [專案結構說明](docs/project_structure.md)
