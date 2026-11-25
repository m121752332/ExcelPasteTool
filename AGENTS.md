# AGENTS.md

給 AI Agent（Claude Code、Codex 等）的工作指引。專案設計與目錄說明見 `AI_CONTEXT.md`，完整結構見 `docs/project_structure.md`。

## 專案概要

- 主程式：`src/ExcelPasteTool/`（Avalonia 12、.NET 11，組件名稱 / 執行檔為 `DataSplitter`）。
- 測試：
  - `tests/ExcelPasteTool.Tests/`：單元測試。
  - `tests/ExcelPasteTool.UITests/`：Avalonia Headless 畫面測試與基準圖。
  - `tests/ExcelPasteTool.E2ETests/`：FlaUI 端到端 smoke 測試，啟動真實 exe，只在 Windows 本機執行。

## 常用指令

| 目的 | 指令 |
|---|---|
| 建置 | `dotnet build` |
| 執行程式 | `dotnet run --project src/ExcelPasteTool` |
| 單元測試 | `dotnet test tests/ExcelPasteTool.Tests` |
| UI 畫面測試（Headless + 視覺比對） | `pwsh tests/run-ui-tests.ps1` |
| 端到端 smoke 測試（真實 exe，Windows） | `pwsh tests/run-e2e-tests.ps1` |

## 改完畫面後的驗證（必做）

1. 執行 `pwsh tests/run-ui-tests.ps1`，讀 `TestResults/ui/summary.json`。
2. 結束代碼：0 = 通過，1 = 測試或視覺比對失敗，2 = 建置失敗（`build.errors` 有檔案與行號）。
3. 視覺差異請看 `TestResults/ui/actual/*.png` 與 `TestResults/ui/diff/*.diff.png`。確定是預期的改動，才用 `-UpdateBaselines` 重建基準圖，並在回報中說明原因。
4. 另外跑單元測試：`dotnet test tests/ExcelPasteTool.Tests`。

## 用真實程式除錯：端到端 smoke 測試（FlaUI）

Headless 測試看不到「真的執行起來」才會發生的問題，例如啟動例外、系統剪貼簿、視窗、主題資源載入。遇到這類問題，或改了啟動流程、剪貼簿、視窗相關程式時，在 Windows 本機執行：

```powershell
pwsh tests/run-e2e-tests.ps1
pwsh tests/run-e2e-tests.ps1 -Filter "FullyQualifiedName~SmokeTests"
```

- 會建置主程式，把它複製到 `TestResults/e2e/_app/`，再以 `DataSplitter.exe --ui-test` 啟動。之後點「資料貼上」、檢查分隔資料 / SQL 條件 / 重複項，最後按清除。
- 約 5 秒，會跳出真實視窗；測試期間會暫時改寫剪貼簿，結束後還原。
- 用獨立的 `config.json`（淺色主題、繁中），不會動到使用者的設定。

### 讀結果

結束代碼：0 = 通過（非 Windows 時為略過），1 = 測試失敗，2 = 建置失敗。

```
TestResults/e2e/
├─ summary.json                    ← 先讀這個：build.errors、tests、runs[]（每個測試的步驟摘要）
└─ <測試名稱>/
   ├─ summary.json                 每一步的 status / durationMs / error，appLogErrors
   ├─ steps.log                    時間軸（▶ 開始、✔ 通過、✖ 失敗）
   ├─ screenshots/NN-*.png         依序截圖；失敗時多一張 NN-failure.png
   ├─ trees/NN-*.txt               UI Automation 控制項樹（AutomationId、Name、位置、TextBox 的 value）
   ├─ app.log                      程式本身的 Avalonia Trace 與未處理例外（[ERROR] / [FATAL]）
   └─ exception.txt                失敗時的完整例外
```

除錯步驟：

1. 在 `summary.json` 的 `runs[]` 找 `status = failed` 的測試，看哪一步失敗。
2. 打開失敗截圖，確認畫面（例如被對話框擋住、資料沒出現）。
3. 看 `trees/*-failure.txt`，確認控制項是否存在、AutomationId 是否正確、是否停用。
4. 看 `app.log` 的 `[ERROR]` / `[FATAL]` 與 `exception.txt`。
5. 修正後重跑，結束代碼為 0 才算完成。

### 規則

- 新增要讓測試操作的控制項時，要加上 `AutomationProperties.AutomationId`（和 `Name` 同名）。現有清單見 `docs/UI-TESTING.md`。
- 新測試放在 `tests/ExcelPasteTool.E2ETests/Tests/`：開頭呼叫 `E2EGuard.SkipUnlessEnabled()`，用 `AppSession.Start(...)` 啟動，每個動作包在 `s.Step(...)` 裡，最後呼叫 `s.Complete()`。
- 測試在 CI 會自動略過（偵測 `CI` / `GITHUB_ACTIONS` / `TF_BUILD`）。`DATASPLITTER_E2E=1` 強制執行，`DATASPLITTER_E2E=0` 強制略過。
- `--ui-test` 只給測試用：略過更新檢查與導覽，並把記錄寫到環境變數 `DATASPLITTER_UI_TEST_LOG` 指定的檔案。不要在一般功能裡依賴它。
- 這組是冒煙測試，不做像素比對；視覺回歸仍由 Headless 測試負責。

完整說明：`docs/UI-TESTING.md`。

## 截圖功能（Alt+F5）

- `Alt+F5`（macOS `⌥F5`）用 `RenderTargetBitmap` 拍下主視窗存成 PNG，側邊欄「截圖」頁管理；說明見 `docs/Screenshots.md`。
- 預設存到「圖片」資料夾底下的 `DataSplitter\yyyyMM\`（依年月分資料夾），`config.json` 的 `ScreenshotFolder` 可改根目錄。根目錄裡的舊 PNG 列為「未分類」，不自動搬動。
- 測試不可寫到使用者的「圖片」資料夾：UI 測試由 `MainWindowHost.ResetAppState` 改到暫存資料夾並固定顯示路徑；E2E 由 `AppSession` 在 `config.json` 指定 `TestResults/e2e/_app/Screenshots`。新增測試時沿用這兩個機制。
- E2E 測試類別之間不平行執行（共用 `_app` 與剪貼簿），新增測試類別不必另外處理。

## 發版

- 版本號在 `src/ExcelPasteTool/ExcelPasteTool.csproj`，改版資訊在 `src/ExcelPasteTool/Resources/Changelog.json`（五種語言都要寫，測試會檢查）。
- 推送 `X.Y.Z` 標籤才會觸發 `release.yml` 發版。未經使用者同意不要推標籤。
