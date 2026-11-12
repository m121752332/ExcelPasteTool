# UI 自動測試（Avalonia Headless）

讓 AI Agent（或人）改完 UI 後，一個指令就能完成：

**建置 → 無畫面測試 → 擷取 PNG → 與基準圖比對 → 輸出結構化結果**

| 項目 | 採用 |
|---|---|
| Runtime | .NET 10（`global.json`） |
| UI Framework | Avalonia 11.3.x |
| 測試框架 | xUnit 2.9 |
| 無畫面測試 | `Avalonia.Headless.XUnit`（`[AvaloniaFact]` / `[AvaloniaTheory]`） |
| 畫面擷取 | `Avalonia.Skia` 真實繪製 + `CaptureRenderedFrame()` |
| 視覺回歸 | `SixLabors.ImageSharp` 3.1 逐像素比對 |
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
   │  ├─ ImageComparer.cs               ImageSharp 逐像素比對 + 差異圖
   │  └─ UiTestPaths.cs                 基準圖與產出物路徑
   ├─ Tests/
   │  ├─ MainWindowSmokeTests.cs        主視窗、側邊欄收合、切頁
   │  ├─ DataToolInteractionTests.cs    打字 → 資料表 / SQL 條件；剪貼簿貼上去重複
   │  └─ ThemeVisualTests.cs            四個主題 + 英文介面的截圖比對
   └─ Baselines/*.png                   基準圖（進版控）
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
| `Helpers/Global.cs` | `AssemblyName` 改用 `typeof(Global).Assembly` | 測試時 EntryAssembly 是 testhost，`avares://testhost/...` 會找不到主題資源（正式執行結果不變，仍是 DataSplitter） |
| `Converters/SidebarPageToContentConverter.cs` | 新增 `internal ResetCache()` | 頁面是靜態快取，每個測試開新視窗前要清掉，避免同一頁面掛到兩個視窗 |
| `Views/MainWindow.Update.cs` | 新增 `internal SkipStartupChecks` | 測試時不檢查 GitHub 更新、不自動播導覽，畫面才穩定 |
| `ExcelPasteTool.csproj` | `InternalsVisibleTo ExcelPasteTool.UITests` | 讓測試用上面兩個 internal 成員 |

## 注意事項

- **基準圖以 Windows 產生**。字型用內建 Noto Sans TC 並由 Skia 繪製，同一台機器重跑差異為 0；換到 Linux / macOS 可能有反鋸齒差異，若要在 CI（ubuntu）跑視覺比對，建議用 `windows-latest` runner，或另外產生該平台的基準圖。目前 `.github/workflows/build.yml` 尚未加入 UI 測試。
- 改了 `Assets/` 內的檔案卻用「還原舊檔」的方式（例如 Copy-Item 備份檔）時，檔案時間可能比建置輸出舊，MSBuild 會判定不必重建；遇到結果怪怪的，先 `dotnet build` 加 `--no-incremental` 或更新檔案時間。
- 靜態狀態（主題、語言、字型）會跨測試保留，所以整個 UI 測試組件關閉了平行執行；每個測試請透過 `MainWindowHost.Open(...)` 指定需要的狀態。
- ImageSharp 停在 3.1.x（Six Labors Split License：開源專案或年營收未達 100 萬美元免費）。
- 說明頁（`HelpView`）用原生 WebView，Headless 下不支援，目前不在測試範圍。
