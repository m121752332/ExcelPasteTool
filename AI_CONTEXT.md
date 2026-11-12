<!-- AI_CONTEXT: System Design, Coding Guidelines, API Reference -->
# 專案設計說明
本專案是用於轉換資料分隔使用的工具
# 專案目錄結構

# UI 修改後的驗證（AI Agent 必做）
改了任何畫面（axaml、Assets/Themes 配色、Assets/Styles、Views/Controls 程式）之後：

1. 執行 `pwsh tests/run-ui-tests.ps1`（建置 → Avalonia Headless 無畫面測試 → 擷取 PNG → 視覺比對）。
2. 讀 `TestResults/ui/summary.json`：結束代碼 0 = 通過、1 = 測試或視覺比對失敗、2 = 建置失敗（`build.errors` 有檔案與行號）。
3. 視覺差異請打開 `TestResults/ui/actual/*.png` 與 `TestResults/ui/diff/*.diff.png` 確認；確定是預期的改動才用 `-UpdateBaselines` 重建基準圖，並在回報中說明。
4. 單元測試另外跑：`dotnet test tests/ExcelPasteTool.Tests`。

詳細說明：`docs/UI-TESTING.md`。