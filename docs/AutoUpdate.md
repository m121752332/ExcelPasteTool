# 版本檢查與自動更新

## 使用者看到的流程

1. 程式啟動約 2 秒後，背景向 GitHub 查詢最新正式版 Release（`/releases/latest`）。
2. 若 Release 版本大於目前版本，跳出「發現新版本」視窗，顯示版本、發佈日期、檔案大小與更新說明。
3. 按「立即更新」→ 下載（進度條、已下載量、速度，可取消）→ 解壓縮並驗證。
4. 顯示「更新已準備完成」，按「重新啟動」→ 程式關閉，更新腳本覆蓋檔案後自動重開。
5. 重開後顯示「🎉 更新完成，已成功更新至 vX.Y.Z」。

「關於」頁另有「🔄 檢查更新」按鈕可手動檢查。離線或 GitHub 限流時，啟動檢查會靜默略過。

## 推版規範

| 項目 | 規則 |
| --- | --- |
| 版本號 | `ExcelPasteTool.csproj` 的 `<Version>`，例如 `1.1.0` |
| tag | `1.1.0` 或 `v1.1.0`（兩種都能被辨識） |
| Release | 必須是正式版，Draft / Pre-release 不會被偵測 |
| 附件 | `.zip`，檔名含平台與架構：`DataSplitter_win_x64.zip`、`DataSplitter_linux_x64.zip`、`DataSplitter_osx_x64.zip`、`DataSplitter_osx_arm64.zip` |
| zip 內容 | 直接放檔案，或只包一層資料夾；必須包含與目前同名的執行檔。macOS 另見下方說明 |

推 tag 後，`.github/workflows/release.yml` 會自動打包四個平台的 zip 並建立 Release，不需要手動上傳。
`.rar` 附件無法自動更新，只會引導使用者到下載頁。

## 實作位置

| 檔案 | 說明 |
| --- | --- |
| `Services/UpdateService.cs` | 版本比較、挑選附件、下載、解壓驗證、產生更新腳本 |
| `Views/UpdateDialog.axaml(.cs)` | 更新對話框與各狀態 |
| `Views/MainWindow.Update.cs` | 啟動時檢查、重開後顯示更新結果 |
| `Views/AboutView` | 手動「檢查更新」按鈕 |

## 注意事項

- 程式資料夾必須可寫入（例如放在 `C:\Program Files` 底下需要系統管理員權限），否則會提示改為手動下載。
- 更新時會保留使用者的 `config.json`，不會被新版覆蓋。
- Windows 用 PowerShell + robocopy 覆蓋檔案；Linux（以及 macOS 舊版裸執行檔）用 `/bin/sh` + `cp`。
- macOS（1.4.2 起）zip 根目錄為 `DataSplitter.app`、相容啟動腳本 `DataSplitter`、`README_macOS.txt`：
  - 從 `.app` 執行時，更新腳本會把新版 `.app` 複製到原位置旁、對調後用 `open` 重新啟動，失敗會還原舊版；
    設定檔在 `~/Library/Application Support/DataSplitter/`，不受替換影響。
  - 1.4.1 以前的舊版只認根目錄的 `DataSplitter`，會把整包複製到原資料夾並執行這個腳本，腳本再轉交給 `DataSplitter.app`；
    新版第一次從 `.app` 啟動時，會把 `.app` 旁的舊 `config.json` 複製到新位置。
  - 這三個檔名都被舊版或新版更新流程依賴，請勿任意更名。
- 暫存與記錄檔位於系統暫存資料夾的 `DataSplitter_update/`，失敗時可查看 `update.log`。
