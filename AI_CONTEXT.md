<!-- AI_CONTEXT: System Design, Coding Guidelines, API Reference -->
# 專案設計說明
本專案是用於轉換資料分隔使用的工具
# 專案目錄結構
- `src/ExcelPasteTool/`：主程式（Avalonia，組件名稱 DataSplitter）；文件中 `Views/`、`Helpers/`、`Assets/` 等路徑都相對於這裡。
- `tests/ExcelPasteTool.Tests/`：單元測試；`tests/ExcelPasteTool.UITests/`：Headless 畫面測試；`tests/run-ui-tests.ps1`：UI 測試一鍵執行；`tests/ExcelPasteTool.E2ETests/` + `tests/run-e2e-tests.ps1`：用 FlaUI 啟動真實 exe 的端到端 smoke 測試（只在 Windows 本機）。
- `packaging/`：`publish.sh`（四平台打包）、`config.json`（預設設定範本）、`macos/`。
- 完整說明：`docs/project_structure.md`。

# UI 修改後的驗證（AI Agent 必做）
改了任何畫面（`src/ExcelPasteTool/` 底下的 axaml、Assets/Themes 配色、Assets/Styles、Views/Controls 程式）之後：

1. 執行 `pwsh tests/run-ui-tests.ps1`（建置 → Avalonia Headless 無畫面測試 → 擷取 PNG → 視覺比對）。
2. 讀 `TestResults/ui/summary.json`：結束代碼 0 = 通過、1 = 測試或視覺比對失敗、2 = 建置失敗（`build.errors` 有檔案與行號）。
3. 視覺差異請打開 `TestResults/ui/actual/*.png` 與 `TestResults/ui/diff/*.diff.png` 確認；確定是預期的改動才用 `-UpdateBaselines` 重建基準圖，並在回報中說明。
4. 單元測試另外跑：`dotnet test tests/ExcelPasteTool.Tests`。
5. 在 Windows 本機、需要確認「真實程式」的行為（Headless 測不到的視窗、剪貼簿、UI Automation）時：執行 `pwsh tests/run-e2e-tests.ps1`，讀 `TestResults/e2e/summary.json`，再看各測試資料夾的 `steps.log`、`screenshots/*.png`、`trees/*.txt`、`app.log`。

詳細說明：`docs/UI-TESTING.md`。