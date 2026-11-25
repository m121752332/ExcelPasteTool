# UI 自動測試（Avalonia Headless）

> 另有啟動真實 exe 的端到端 smoke 測試（FlaUI，僅 Windows 本機）：見文末〈[端到端 smoke 測試](#端到端-smoke-測試flaui啟動真實-exe)〉。

讓 AI Agent（或人）改完 UI 後，一個指令就能完成：

**建置 → 無畫面測試 → 擷取 PNG → 與基準圖比對 → 輸出結構化結果**

| 項目 | 採用 |
|---|---|
| Runtime | .NET 11（`global.json`） |
| UI Framework | Avalonia 12.1.x（Avalonia.Headless.XUnit 建立在 xUnit v3 上） |
| 測試框架 | xUnit 2.9 |
| 無畫面測試 | `Avalonia.Headless.XUnit`（`[AvaloniaFact]` / `[AvaloniaTheory]`） |
| 畫面擷取 | `Avalonia.Skia` 真實繪製 + `CaptureRenderedFrame()` |
| 視覺回歸 | `SkiaSharp`（Avalonia.Skia 帶入）逐像素比對 |
| 報告 | TRX + `summary.json` |
| Agent 整合 | `tests/run-ui-tests.ps1`（結束代碼 + JSON） |

## 一鍵執行

```powershell
pwsh tests/run-ui-tests.ps1                     # 全部
pwsh tests/run-ui-tests.ps1 -Filter "FullyQualifiedName~ThemeVisualTests"
pwsh tests/run-ui-tests.ps1 -UpdateBaselines    # UI 改動是預期的 → 重建基準圖
```

Windows PowerShell 5.1（`powershell -File ...`）也可以跑。

### 結束代碼

| 代碼 | 意義 | 下一步 |
|---|---|---|
| 0 | 全部通過 | — |
| 1 | 有測試失敗（功能斷言、視覺差異、缺基準圖） | 看 `summary.json` 的 `tests.failures` 與 `visual` |
| 2 | 建置失敗 | 看 `summary.json` 的 `build.errors`（檔案、行號、錯誤碼） |

### 產出物（`TestResults/ui/`，已在 .gitignore）

```
TestResults/ui/
├─ summary.json        ← Agent 只要讀這個
├─ ui-tests.trx        ← 原始 TRX（VS / CI 可直接讀）
├─ actual/*.png        ← 本次擷取的畫面
├─ diff/*.diff.png     ← 比對失敗才有：紅色 = 不同的像素，其餘淡化成灰階
└─ visual/*.json       ← 每張圖的比對數據
```

`summary.json` 範例（視覺比對失敗時）：

```json
{
  "status": "failed",
  "exitCode": 1,
  "build": { "succeeded": true, "errors": [], "warnings": 0 },
  "tests": {
    "total": 11, "passed": 8, "failed": 3, "skipped": 0,
    "failures": [
      { "name": "ExcelPasteTool.UITests.Tests.ThemeVisualTests.ToolsPage_EachTheme_MatchesBaseline(theme: Dark)",
        "message": "[視覺比對:failed] tools-dark：與基準圖差異 0.958%（7374 個像素，上限 0.200%），差異圖：...",
        "stackTrace": "..." }
    ],
    "trx": "TestResults/ui/ui-tests.trx"
  },
  "visual": [
    { "name": "tools-dark", "status": "failed",
      "baseline": "tests/ExcelPasteTool.UITests/Baselines/tools-dark.png",
      "actual": "TestResults/ui/actual/tools-dark.png",
      "diff": "TestResults/ui/diff/tools-dark.diff.png",
      "diffPixels": 7374, "diffRatio": 0.009577, "maxChannelDelta": 207,
      "channelTolerance": 16, "maxDiffRatio": 0.002 }
  ]
}
```

建置失敗時 `build.errors` 長這樣：

```json
{ "severity": "error", "code": "CS0103",
  "file": "tests/ExcelPasteTool.UITests/Tests/ThemeVisualTests.cs", "line": 30, "column": 35,
  "message": "The name 'undefinedThing' does not exist in the current context" }
```

視覺比對的 `status`：`passed`、`failed`、`size-mismatch`、`baseline-missing`、`baseline-updated`。

## AI Agent 的標準流程

1. 修改 UI（axaml / 配色 / 程式碼）。
2. 執行 `pwsh tests/run-ui-tests.ps1`。
3. 讀 `TestResults/ui/summary.json`：
   - `exitCode = 2`：依 `build.errors` 修正後回到 2。
   - `exitCode = 1` 且是功能斷言失敗：修程式。
   - `exitCode = 1` 且是視覺差異：打開 `actual/*.png` 與 `diff/*.diff.png` 確認。
     - **非預期**的差異 → 修 UI。
     - **預期**的差異（本來就要改外觀）→ 跑 `-UpdateBaselines`，並在回報中說明哪些基準圖更新了、為什麼。
4. `exitCode = 0` 才算完成。

> 不要為了讓測試變綠而直接更新基準圖；更新基準圖必須是「確認新畫面正確」之後的動作，且基準圖變更要一起 commit 讓人審。

## 專案結構

```
tests/
├─ run-ui-tests.ps1                     一鍵執行 + 報告彙整
└─ ExcelPasteTool.UITests/
   ├─ TestAppBuilder.cs                 Headless + Skia，沿用正式 App（同一份 App.axaml）
   ├─ Infrastructure/
   │  ├─ MainWindowHost.cs              開主視窗、重設主題/語言/字型、找控制項、模擬輸入
   │  ├─ VisualAssert.cs                擷取 PNG、比對基準、寫 JSON
   │  ├─ ImageComparer.cs               SkiaSharp 逐像素比對 + 差異圖
   │  └─ UiTestPaths.cs                 基準圖與產出物路徑
   ├─ Tests/
   │  ├─ MainWindowSmokeTests.cs        主視窗、側邊欄收合、切頁
   │  ├─ DataToolInteractionTests.cs    打字 → 資料表 / SQL 條件；剪貼簿貼上去重複
   │  ├─ ThemeVisualTests.cs            四個主題 + 英文介面的截圖比對
   │  └─ ScreenshotTests.cs             Alt+F5 截圖、截圖管理頁（清單 / 預覽 / 刪除 / 複製）、截圖資料夾設定
   └─ Baselines/*.png                   基準圖（進版控）

tests/run-e2e-tests.ps1                 端到端測試一鍵執行 + 報告彙整
tests/ExcelPasteTool.E2ETests/          FlaUI 端到端 smoke 測試（見文末）
├─ Infrastructure/
│  ├─ AppSession.cs                     準備 _app、啟動 exe、找控制項 / 點擊 / 讀值 / 截圖 / 控制項樹 / 記錄
│  ├─ E2EGuard.cs                       只在 Windows 本機執行（CI 略過）
│  ├─ E2EPaths.cs                       exe 與產出物路徑
│  └─ ClipboardHelper.cs                STA 執行緒讀寫系統剪貼簿
└─ Tests/
   ├─ SmokeTests.cs                     啟動 → 資料貼上 → 檢查分隔資料 / SQL / 重複項 → 清除
   └─ ScreenshotTests.cs                Alt+F5 → 截圖資料夾出現 PNG → 截圖頁 → 「複製」後剪貼簿有圖片
```

## 撰寫新測試

```csharp
public class MyTests
{
    [AvaloniaFact]   // 一定要用 AvaloniaFact / AvaloniaTheory，才會跑在 Avalonia 的 UI 執行緒
    public void Something_Works()
    {
        using var host = MainWindowHost.Open(AppTheme.Light, AppLanguage.English);

        host.TypeInto(host.Find<TextBox>("TopBox"), "A01|B01");   // 模擬打字
        host.Window.SidebarItemClickCommand.Execute(SidebarPage.Settings);
        MainWindowHost.Pump();                                    // 讓版面與繪製跟上

        Assert.Equal(...);
        VisualAssert.MatchesBaseline(host.Window, "my-screen");   // 第一次會因缺基準圖失敗 → -UpdateBaselines
    }
}
```

- `host.Find<T>(name)` 會往 UserControl 內找（依 `Name`）。
- 剪貼簿：`host.Window.Clipboard!.SetTextAsync(...)`，再呼叫 `DataToolView.PasteFromClipboardAsync`。
- 容許值可個別調整：`VisualAssert.MatchesBaseline(win, "x", new VisualCompareOptions { ChannelTolerance = 24, MaxDiffRatio = 0.005 })`。

## 為了可測試性所做的程式調整

| 位置 | 調整 | 原因 |
|---|---|---|
| `src/ExcelPasteTool/Helpers/Global.cs` | `AssemblyName` 改用 `typeof(Global).Assembly` | 測試時 EntryAssembly 是 testhost，`avares://testhost/...` 會找不到主題資源（正式執行結果不變，仍是 DataSplitter） |
| `src/ExcelPasteTool/Converters/SidebarPageToContentConverter.cs` | 新增 `internal ResetCache()` | 頁面是靜態快取，每個測試開新視窗前要清掉，避免同一頁面掛到兩個視窗 |
| `src/ExcelPasteTool/Views/MainWindow.Update.cs` | 新增 `internal SkipStartupChecks` | 測試時不檢查 GitHub 更新、不自動播導覽，畫面才穩定 |
| `src/ExcelPasteTool/ExcelPasteTool.csproj` | `InternalsVisibleTo ExcelPasteTool.UITests` | 讓測試用上面兩個 internal 成員 |

## 注意事項

- **基準圖以 Windows 產生**。字型用內建 Noto Sans TC 並由 Skia 繪製，同一台機器重跑差異為 0；換到 Linux / macOS 可能有反鋸齒差異，若要在 CI（ubuntu）跑視覺比對，建議用 `windows-latest` runner，或另外產生該平台的基準圖。目前 `.github/workflows/build.yml` 尚未加入 UI 測試。
- 改了 `Assets/` 內的檔案卻用「還原舊檔」的方式（例如 Copy-Item 備份檔）時，檔案時間可能比建置輸出舊，MSBuild 會判定不必重建；遇到結果怪怪的，先 `dotnet build` 加 `--no-incremental` 或更新檔案時間。
- 靜態狀態（主題、語言、字型）會跨測試保留，所以整個 UI 測試組件關閉了平行執行；每個測試請透過 `MainWindowHost.Open(...)` 指定需要的狀態。
- 不使用 ImageSharp：3.1.x 有已知弱點，3.2.0 起 Release 建置需要授權金鑰，所以視覺比對改用 Avalonia.Skia 本來就會帶入的 SkiaSharp，不需要額外套件或授權。
- 說明頁（`HelpView`）用原生 WebView，Headless 下不支援，目前不在測試範圍。

---

# 端到端 smoke 測試（FlaUI，啟動真實 exe）

Headless 測試不開實體視窗，測不到「真的執行起來」才會出現的問題（啟動例外、系統剪貼簿、視窗與 UI Automation、主題資源載入等）。
`tests/ExcelPasteTool.E2ETests` 用 [FlaUI](https://github.com/FlaUI/FlaUI)（UIA3）啟動建置好的 `DataSplitter.exe`，像使用者一樣點按鈕、讀文字、截圖，並把過程全部寫成檔案，讓 AI 可以自己跑、自己看結果除錯。

| 項目 | 說明 |
|---|---|
| 測試框架 | xUnit v3 + FlaUI.UIA3 5.0 |
| 執行環境 | **只在 Windows 本機**（需要可互動的桌面）；CI 預設自動略過 |
| 啟動方式 | `DataSplitter.exe --ui-test`（不檢查更新、不播導覽，並把程式內部記錄寫到 `app.log`） |
| 隔離 | 程式複製到 `TestResults/e2e/_app/`，放一份固定的 `config.json`（淺色主題、繁中），不會動到 `bin/` 或使用者的設定檔 |
| 剪貼簿 | 測試會暫時改寫系統剪貼簿，結束後還原原本的文字 |
| 截圖 | `config.json` 的 `ScreenshotFolder` 指到 `TestResults/e2e/_app/Screenshots`（每次啟動前清空），不會寫到使用者的「圖片」資料夾 |
| 平行執行 | 關閉（`E2EGuard.cs` 的 `CollectionBehavior`）：每個測試都啟動同一份 `_app` 的程式並共用剪貼簿，平行跑時後啟動的會把先啟動的程式關掉 |

## 一鍵執行

```powershell
pwsh tests/run-e2e-tests.ps1
pwsh tests/run-e2e-tests.ps1 -Filter "FullyQualifiedName~SmokeTests"
```

會先建置主程式與測試專案，再啟動程式（約 5 秒，會看到視窗跳出來又關掉，**期間不要操作滑鼠鍵盤**）。Windows PowerShell 5.1 也能跑。

| 結束代碼 | 意義 | 下一步 |
|---|---|---|
| 0 | 全部通過（非 Windows 時為略過，`status = skipped`） | — |
| 1 | 有測試失敗 | 看 `summary.json` 的 `runs[].steps` 找出失敗的那一步，再看截圖、控制項樹、`app.log` |
| 2 | 建置失敗 | 看 `summary.json` 的 `build.errors` |

## 產出物（固定在 `TestResults/e2e/`，已在 .gitignore）

```
TestResults/e2e/
├─ summary.json                         ← AI 先讀這個：建置、測試數、每個測試的步驟摘要
├─ e2e-tests.trx
├─ _app/                                程式副本（保留，下次只複製有變動的檔案）
└─ Launch_PasteData_FillsTable/         一個測試一個資料夾
   ├─ summary.json                      每一步的 status / durationMs / error、截圖與控制項樹清單、appLogErrors
   ├─ steps.log                         同上，純文字時間軸（▶ 開始、✔ 通過、✖ 失敗）
   ├─ screenshots/01-launched.png       依序編號的主視窗截圖
   ├─ screenshots/03-after-paste.png
   ├─ screenshots/NN-failure.png        失敗當下自動補拍
   ├─ trees/02-launched.txt             UI Automation 控制項樹
   ├─ trees/NN-failure.txt              失敗當下自動傾印
   ├─ app.log                           程式本身的記錄：Avalonia Trace、未處理例外（[ERROR] / [FATAL]）
   └─ exception.txt                     失敗時的完整例外與堆疊
```

`steps.log` 範例：

```
09:22:15.561 +  4173 ms  ▶ [5] 點「資料貼上」
09:22:15.657 +  4270 ms     Invoke "PasteDataButton"
09:22:15.658 +  4270 ms  ✔ [5] 點「資料貼上」（96 ms）
09:22:15.658 +  4270 ms  ▶ [6] 分隔資料出現貼上的內容
09:22:15.743 +  4356 ms     "TopBox" = "A01|B02|C03"
```

失敗時：

```
09:24:03.131 +  2352 ms  ▶ [7] SQL 條件同步產生
09:24:18.238 + 17459 ms  ✖ [7] SQL 條件同步產生（15107 ms）：TimeoutException: 等待 "BottomBox" 的文字逾時，最後讀到："('A01', 'B02', 'C03')"
09:24:18.460 + 17682 ms     截圖 TestResults/e2e/Launch_PasteData_FillsTable/screenshots/03-failure.png
09:24:18.587 + 17808 ms     控制項樹 TestResults/e2e/Launch_PasteData_FillsTable/trees/04-failure.txt
```

控制項樹（`trees/*.txt`）每行一個節點，格式為 `類型 #AutomationId "Name" (ClassName) [x,y 寬x高] value="..." {停用,畫面外}`：

```
Window #MainWindowInstance "DataSplitter" (MainWindow) [410,177 1100x700]
  List (ItemsControl) [410,241 55x240]
    Button #Sidebar_Tools "工具" (Button) [414,243 47x44]
  Custom (DataToolView) [466,177 1044x700]
    Edit #TopBox (TextBox) [495,309 372x263] value="A01|B02|C03"
    Button #PasteDataButton "資料貼上" (Button) [907,273 105x35]
    Button #DuplicatesButton "重複項 1" (Button) [1269,273 101x35]
```

## AI Agent 除錯流程

1. 執行 `pwsh tests/run-e2e-tests.ps1`。
2. 讀 `TestResults/e2e/summary.json`：
   - `exitCode = 2`：依 `build.errors` 修正。
   - `exitCode = 1`：在 `runs[]` 找 `status = failed` 的測試，看它的 `error` 與 `steps`（哪一步失敗）。
3. 打開該測試資料夾：
   - `screenshots/*.png`：用看圖的方式確認當下畫面（例如被對話框擋住、資料沒出現、版面跑掉）。
   - `trees/*.txt`：確認控制項是否存在、AutomationId 對不對、是否停用 / 在畫面外、TextBox 的 `value`。
   - `app.log`：程式內部的例外（`[ERROR]`、`[FATAL]`）與 Avalonia 的 Trace（綁定錯誤等）；`summary.json` 的 `appLogErrors` 已先挑出錯誤行。
   - `exception.txt`：測試端的完整堆疊。
4. 修正後重跑，`exitCode = 0` 才算完成。

> 這組測試是「冒煙測試」：只確認最核心的流程走得通，不做像素比對（視覺回歸仍由 Headless 測試負責）。

## 撰寫新測試

```csharp
[Trait("Category", "E2E")]
public class MyE2ETests
{
    [Fact]
    public void Something_Works()
    {
        E2EGuard.SkipUnlessEnabled();                              // 非 Windows / CI 時略過
        using var s = AppSession.Start(nameof(Something_Works));   // 準備 _app、啟動 exe、等主視窗

        s.Step("切到設定頁", () => s.Invoke("Sidebar_Settings"));  // 每個動作包在 Step 裡：失敗會自動截圖 + 傾印控制項樹
        s.Screenshot("settings");
        s.DumpTree("settings");

        s.Step("把資料放進剪貼簿", () => s.SetClipboard("X01\r\nX02"));
        var text = s.Step("等分隔資料", () => s.WaitForText("TopBox", t => t.Contains("X01")));

        s.Complete();                                              // 一定要在最後呼叫，沒呼叫視為失敗
    }
}
```

- `s.Find(id)` 依 AutomationId 找控制項（預設等 15 秒）；`s.Invoke(id)` 優先用 UIA Invoke（不搶滑鼠），不支援時才模擬點擊；`s.ReadText(id)` 讀 TextBox 的值或其他控制項的 Name。
- `s.MainWindow` 是 FlaUI 的 `Window`，需要更進階的操作（鍵盤、選單、ComboBox）可直接用 FlaUI API。
- `AppSession.Start(name, theme: "Dark", language: "English")` 可指定啟動時的主題與語言。

### AutomationId

Avalonia 沒設 `AutomationProperties.AutomationId` 時會用控制項的 `Name`，但重新命名就會讓測試壞掉，所以主要控制項都明確設定了：

| 位置 | AutomationId |
|---|---|
| `Views/MainWindow.axaml` | `SidebarToggleButton`、`TitleBarSidebarButton`、`TourButton`、`MainContent`、側邊欄 `Sidebar_Tools` / `Sidebar_Screenshots` / `Sidebar_Settings` / `Sidebar_Help` / `Sidebar_GitHub` / `Sidebar_About` |
| `Views/ScreenshotsView.axaml` | `CaptureScreenshotButton`、`OpenScreenshotFolderButton`、`RefreshScreenshotsButton`、`ScreenshotsSummaryText`、`ScreenshotList`、`CopyScreenshotButton`、`OpenScreenshotButton`、`RevealScreenshotButton`、`DeleteScreenshotButton`、`DeleteScreenshotConfirmButton` |
| `Views/SettingsView.axaml` | `ChangeScreenshotFolderButton`、`ResetScreenshotFolderButton` |
| `Views/DataToolView.axaml` | `PasteDataButton`、`AppendPasteButton`、`DuplicatesButton`、`ClearDataButton`、`TopBox`、`BottomBox`、`CopyTopBoxButton`、`CopyBottomBoxButton`、`SqlOptionsButton`、`ToggleSqlPanelButton`、`SeparatorComboBox`、`TableContainer`、`RowCountText` |

新增要被測試操作的控制項時，請一併加上 `AutomationProperties.AutomationId`（和 `Name` 同名即可）。

### 執行條件（`Infrastructure/E2EGuard.cs`）

| 情況 | 結果 |
|---|---|
| 非 Windows | 略過 |
| 環境變數 `CI`、`GITHUB_ACTIONS` 或 `TF_BUILD` 有值 | 略過（CI 機器通常沒有互動桌面） |
| `DATASPLITTER_E2E=1` | 強制執行（`run-e2e-tests.ps1` 會設） |
| `DATASPLITTER_E2E=0` | 強制略過（本機跑整個方案的 `dotnet test` 又不想跳出視窗時） |

其他環境變數：`E2E_RESULTS_DIR`（改產出物位置）、`DATASPLITTER_EXE`（改測別的 exe，例如發佈後的單一檔案）。

`.github/workflows/build.yml` 只建置主程式並跑 `tests/ExcelPasteTool.Tests`，不受這個專案影響；專案設了 `EnableWindowsTargeting`，在 Linux / macOS 建置整個方案也不會失敗。

### 端到端測試相關的程式調整

| 位置 | 調整 |
|---|---|
| `src/ExcelPasteTool/Helpers/UiTestMode.cs`（新增） | `--ui-test` 參數：設 `MainWindow.SkipStartupChecks`（不檢查更新、不播導覽）；若有環境變數 `DATASPLITTER_UI_TEST_LOG`，把 Trace 與未處理例外寫進該檔。一般執行不帶參數，行為不變 |
| `src/ExcelPasteTool/Program.cs` | 啟動時呼叫 `UiTestMode.EnableIfRequested(args)` |
| `Views/MainWindow.axaml`、`Views/DataToolView.axaml` | 補上 `AutomationProperties.AutomationId`（見上表） |
