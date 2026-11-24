# DataSplitter Pro（ExcelPasteTool）

**繁體中文** | [English](README.en.md)

把 Excel 的一欄資料，一鍵轉成「分隔符號字串」和「SQL `IN` 條件」的跨平台桌面小工具。

從 Excel 複製一欄料號、單號或代碼，按下「資料貼上」，就會同時得到：

| 輸出 | 範例 | 常見用途 |
| --- | --- | --- |
| 分隔資料 | `A\|B\|C\|D` | 貼到 ERP 或報表的多值查詢欄位 |
| SQL 條件 | `('A', 'B', 'C', 'D')` | 直接接在 `WHERE 欄位 IN` 後面 |
| 資料表 | 逐筆列出並編號 | 檢查筆數、移除不要的項目 |

- 專案名稱是 `ExcelPasteTool`，產品名稱是 **DataSplitter Pro**，執行檔叫 `DataSplitter`。
- 支援 Windows、macOS、Linux，以 [.NET 11](https://dotnet.microsoft.com/) 和 [Avalonia UI](https://avaloniaui.net/) 12 開發。
- 下載的 zip 已內含 .NET 執行環境，**使用者不需要安裝 .NET**。

## 目錄

- [功能](#功能)
- [下載與安裝](#下載與安裝)
- [使用方式](#使用方式)
- [自動更新](#自動更新)
- [開發](#開發)
- [測試](#測試)
- [打包與發佈](#打包與發佈)
- [專案結構](#專案結構)
- [相關文件](#相關文件)

## 功能

- **貼上即轉換**：從剪貼簿讀取資料，每一行只取第一欄，自動去除空白行與重複值。
- **查看重複項**：「重複項」按鈕列出被移除的重複值、每個值出現幾次、移除了幾筆。
- **三個區塊互相同步**：分隔資料、SQL 條件、資料表任一處變動，另外兩處會跟著更新。
- **可直接編輯**：在「分隔資料」框裡手動輸入或修改，資料表與 SQL 條件會即時重算。
- **單筆移除**：滑鼠移到資料表的某一列，右側會出現 ✕，點一下就移除該筆。
- **五種分隔符號**：豎線 `|`、逗號 `,`（依 CSV 規則處理雙引號）、分號 `;`、冒號 `:`、空白。
- **一鍵複製**：分隔資料、SQL 條件各有複製按鈕；點資料表的「行號」表頭可複製整欄。
- **SQL 安全處理**：值裡面的單引號會自動轉成兩個單引號。
- **介面設定**：五種語言（繁體中文、簡體中文、English、日本語、한국어）、四種主題（GitHub Dark、Light、蒂芬妮藍、彩虹幻境）、可調整字型與字體大小、可切換精簡標題列。
- **新手引導**：「工具」頁右上角的「導覽」逐步介紹各區塊（空白鍵前往下一步）；按 `F1` 開啟圖解使用說明。
- **快捷鍵**：`Alt+P` 貼上剪貼簿資料、`Alt+Q` 開關側邊欄、`Alt+F4` 關閉程式。
- **自動更新**：啟動時檢查 GitHub 是否有新版本，可在程式內下載更新，並查看每一版的改版資訊。

## 下載與安裝

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
   - **macOS**：雙擊 `DataSplitter.app` 開啟（可以拖到「應用程式」資料夾）。資料夾裡另一個名為 `DataSplitter` 的檔案是給舊版自動更新用的相容腳本，不用理它。
     程式沒有 Apple 開發者簽章與公證，用瀏覽器下載後第一次開啟會出現「無法打開，因為它來自未識別的開發者」。請先做一次下面任一種方式，之後就能直接雙擊開啟：
     - **終端機（建議）**：打開「終端機」，輸入 `cd `（後面留一個空格）再把解壓後的資料夾拖進視窗、按 Enter，然後執行 `xattr -dr com.apple.quarantine .`，再回 Finder 雙擊 `DataSplitter.app`。
     - **系統設定**：先雙擊 `DataSplitter.app` 一次讓它被擋，再到「系統設定 > 隱私權與安全性」，往下找到「已阻擋『DataSplitter』」按「強制打開」（舊版 macOS 為「仍要打開」），輸入密碼確認。

     zip 裡附有 `README_macOS.txt`，內容相同。
   - **Linux**：`chmod +x DataSplitter && ./DataSplitter`。需要 X11 與中文字型，Debian／Ubuntu 可執行：

     ```bash
     sudo apt install libx11-6 libice6 libsm6 libfontconfig1 fonts-noto-cjk
     ```

     想在程式內顯示使用說明（F1），需另外安裝 `libwebkit2gtk-4.1-0`；沒有安裝時說明會改用預設瀏覽器開啟。

設定檔位置：

| 平台 | 位置 |
| --- | --- |
| Windows、Linux | 執行檔旁邊的 `config.json` |
| macOS | `~/Library/Application Support/DataSplitter/config.json` |

## 使用方式

1. 在 Excel 選取一欄資料並複製（Ctrl+C）。
2. 在程式右側按「**資料貼上**」（或按 `Alt+P`）。
3. 左上的「分隔資料」和左下的「SQL 條件」會立刻產生，按各自右上角的按鈕即可複製。
4. 需要時可以：
   - 在上方工具列切換**分隔符號**。
   - 把滑鼠移到某一列，按右側的 **✕** 移除該筆。
   - 按「**重複項**」查看哪些值因重複被移除。
   - 直接在「分隔資料」框裡輸入或修改內容。
   - 按「**清空資料**」回到初始狀態。

貼上多欄資料時只會取每一行的第一欄（以 Tab、逗號、分號、豎線判斷欄位）。

不確定怎麼操作時，隨時按 **F1** 開啟使用說明。說明預設顯示在程式內（使用系統內建的瀏覽器引擎：Windows 為 WebView2、macOS 為 WKWebView、Linux 為 WebKitGTK）；也可以到「設定」頁把「說明開啟方式」改成「預設瀏覽器」。電腦上沒有可用的引擎時，會自動改用預設瀏覽器開啟。

語言與主題可在工具列臨時切換；要永久保存，請到左側選單的「設定」頁調整。

## 自動更新

- 程式啟動時會檢查 GitHub 最新的正式版 Release，有新版時提示下載。
- 更新會依平台挑選對應的 zip，換掉執行檔並保留 `config.json`。
- 每個 zip 都內含 .NET 執行環境，從舊版（包含 .NET 8、.NET 10 打包的版本）更新上來也不需要另外安裝任何東西。

運作細節與推版規範見 [docs/AutoUpdate.md](docs/AutoUpdate.md)。

## 開發

### 需要的工具

| 工具 | 版本 | 說明 |
| --- | --- | --- |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/11.0) | 11.0.100 以上的 11.0 系列 | 由 `global.json` 指定 |
| Git | 近期版本 | 取得原始碼 |

用 `dotnet --list-sdks` 可以確認本機已安裝的版本。

IDE（擇一）：

- **Visual Studio 2026**（請更新到支援 .NET 11 的版本；Visual Studio 2022 不支援）：安裝「.NET 桌面開發」工作負載和 [Avalonia for Visual Studio](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaVS) 擴充套件。
- **JetBrains Rider**：內建 Avalonia 支援。
- **VS Code**：安裝 C# Dev Kit 和 Avalonia for VS Code。

### 取得原始碼、建置與執行

以下指令都在倉庫根目錄執行：

```bash
git clone https://github.com/m121752332/ExcelPasteTool.git
cd ExcelPasteTool

# 建置整個方案（主程式＋測試）
dotnet build

# 執行程式
dotnet run --project src/ExcelPasteTool
```

- 也可以在 IDE 開啟 `ExcelPasteTool.sln`，將 `ExcelPasteTool` 設為啟動專案後按 F5。
- 建置輸出在 `src/ExcelPasteTool/bin/<Configuration>/net11.0/`；開發時讀寫的設定檔也在這裡。`packaging/config.json` 只是打包用的預設範本。
- NuGet 套件版本集中在 `Directory.Packages.props` 管理，`.csproj` 的 `PackageReference` 不寫版本號。新增套件時兩邊都要改，否則會出現 NU1008 錯誤。
- 畫面文字以 `src/ExcelPasteTool/Helpers/LanguageManager.cs` 內的字典為準；`Resources/Languages/*.json` 有一起打包，但程式還沒有讀取。新增文字時兩邊都要更新。

## 測試

```bash
# 單元測試（xUnit）
dotnet test tests/ExcelPasteTool.Tests

# UI 畫面測試（Avalonia Headless：擷取畫面並和基準圖比對）
pwsh tests/run-ui-tests.ps1
```

- UI 測試的報告在 `TestResults/ui/summary.json`，用法與基準圖更新方式見 [docs/UI-TESTING.md](docs/UI-TESTING.md)。
- CI（`build.yml`）在開 PR 和推送到 `master` 時建置 Release 組態並跑單元測試。

自動測試沒涵蓋的部分，修改程式後請手動確認以下項目：

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

## 打包與發佈

### 發佈設定只有一份

四個平台的發佈參數（自包含 .NET 11、單一檔案、內含原生函式庫、ReadyToRun、不產生 .pdb）都寫在 `src/ExcelPasteTool/Properties/PublishProfiles/<rid>.pubxml`。Visual Studio「發佈」、命令列和 GitHub Actions 都讀這一份，結果一致；要改發佈參數只改這裡。

| 方式 | 做法 | 輸出 |
| --- | --- | --- |
| Visual Studio | 在專案按右鍵「發佈」，選 `win-x64`、`linux-x64`、`osx-x64` 或 `osx-arm64` | `artifacts/<rid>/` |
| 命令列 | `dotnet publish src/ExcelPasteTool -c Release -p:PublishProfile=win-x64` | `artifacts/<rid>/` |
| 打包成 zip | `bash packaging/publish.sh win-x64 1.7.1`（需要 `jq`、`zip`；macOS 版要在 Mac 上跑） | 根目錄的 `DataSplitter_win_x64.zip` |

`artifacts/` 已列入 `.gitignore`。手動交給別人使用時，把 `packaging/config.json` 一起放進輸出資料夾；`publish.sh` 會自動放。

### 發佈正式版本（GitHub Actions）

推送版本 tag 後，`release.yml` 會用 `packaging/publish.sh` 打包四個平台的 zip 並建立 GitHub Release：

1. 把 `src/ExcelPasteTool/ExcelPasteTool.csproj` 的 `Version`、`AssemblyVersion`、`FileVersion`、`InformationalVersion` 改成新版本，並在 `src/ExcelPasteTool/Resources/Changelog.json` 最前面加一筆五種語言的改版說明（單元測試會檢查），合併到 `master`。
2. 建立並推送 tag（`1.7.1` 或 `v1.7.1` 都可以）：

   ```bash
   git checkout master && git pull
   git tag 1.7.1
   git push origin 1.7.1
   ```

3. 等 workflow 跑完，到 Releases 頁面確認四個 zip 都已上傳。

不要把 Release 設成 Draft 或 Pre-release，也不要改 zip 檔名，否則已安裝的使用者收不到更新提示。

## 專案結構

```text
ExcelPasteTool/                     # 倉庫根目錄
├── ExcelPasteTool.sln              # 方案檔（主程式＋兩個測試專案）
├── Directory.Packages.props        # NuGet 套件版本集中管理
├── global.json                     # 指定 .NET SDK 版本
├── README.md、README.en.md、AI_CONTEXT.md
│
├── src/ExcelPasteTool/             # 主程式專案（組件名稱 DataSplitter）
│   ├── ExcelPasteTool.csproj       # 專案檔（版本號在這裡）
│   ├── Program.cs                  # 程式進入點
│   ├── App.axaml(.cs)              # 應用程式初始化（載入設定、主題、字型）
│   ├── Views/                      # 畫面（主視窗、資料工具、設定、關於、說明、更新、改版資訊、重複項）
│   ├── ViewModels/                 # 設定頁與側邊欄的畫面邏輯
│   ├── Models/                     # 資料模型（DataItem、SidebarItem）
│   ├── Services/                   # 設定檔、路徑、版本檢查與自動更新、說明、改版資訊
│   ├── Helpers/                    # 主題、語言、字型、圖示、Toast、貼上與分隔處理
│   ├── Controls/                   # 自訂控制項（導覽遮罩 TourOverlay）
│   ├── Converters/                 # Binding 轉換器
│   ├── Assets/                     # Themes、Styles、Fonts（Noto Sans TC 一般／粗體）、Help、Images、Logos
│   ├── Resources/                  # Languages/*.json、Changelog.json
│   └── Properties/                 # Resources.resx、PublishProfiles/（四平台發佈設定）
│
├── tests/
│   ├── ExcelPasteTool.Tests/       # 單元測試（xUnit）
│   ├── ExcelPasteTool.UITests/     # Avalonia Headless 畫面測試與基準圖
│   └── run-ui-tests.ps1            # UI 測試一鍵執行與報告
│
├── packaging/
│   ├── publish.sh                  # 單一平台發佈＋壓 zip（release.yml 呼叫）
│   ├── config.json                 # 預設設定範本（打包時放進 zip）
│   └── macos/                      # .app 的 Info.plist、圖示、相容啟動腳本
│
├── docs/                           # 補充文件
└── .github/workflows/              # build.yml（建置＋單元測試）、release.yml（打包發佈）
```

其他文件提到的原始碼路徑（`Views/`、`Helpers/` 等）都相對於 `src/ExcelPasteTool/`。

## 相關文件

- [專案配置說明書](docs/SETUP.md)：設定檔欄位、主題與字型機制、新增主題或語言的步驟、常見問題。
- [專案結構說明](docs/project_structure.md)：目錄配置、常用指令、搬檔時要一起改的地方。
- [自動更新說明](docs/AutoUpdate.md)
- [UI 自動測試](docs/UI-TESTING.md)
