# 截圖（Alt+F5）與截圖管理頁

在 App 任何畫面按 `Alt+F5`（macOS 是 `⌥F5`，MacBook 鍵盤要再按住 `fn`），會拍下目前的 App 畫面存成 PNG，並用通知卡片顯示檔名。側邊欄的「截圖」頁可以瀏覽、預覽、複製、刪除拍好的截圖。

## 怎麼拍

用 Avalonia 12 的 `RenderTargetBitmap` 把主視窗重新繪製到一張點陣圖，再用 `Bitmap.Save(path, PngBitmapEncoderOptions.Default)` 存檔（`Services/ScreenshotService.cs`）：

```csharp
var scaling = TopLevel.GetTopLevel(window)?.RenderScaling ?? 1.0;
using var bitmap = new RenderTargetBitmap(PixelSize.FromSize(window.Bounds.Size, scaling), new Vector(96 * scaling, 96 * scaling));
bitmap.Render(window);
bitmap.Save(path, PngBitmapEncoderOptions.Default);
```

- 只拍 App 本身：不含系統標題列、其他視窗或桌面。各平台作法相同，也不需要 macOS 的「螢幕錄製」權限。
- 依螢幕縮放比例輸出實際像素：150% 縮放時，1100×700 的視窗會存成 1650×1050。
- 畫面上正在顯示的通知卡片不會被拍進去（拍之前先把 `ToastHost` 設成透明，`Views/MainWindow.Screenshot.cs`）。
- 導覽進行中也可以拍。

## 存在哪裡

| 項目 | 說明 |
| --- | --- |
| 預設資料夾 | 「圖片」資料夾底下的 `DataSplitter`：Windows 是 `%USERPROFILE%\Pictures\DataSplitter`，macOS / Linux 是 `~/Pictures/DataSplitter`。系統找不到「圖片」資料夾時，退回使用者資料夾下的 `Pictures` |
| 自訂資料夾 | 設定 › 一般設置 › 截圖資料夾 › 「變更…」，存在 `config.json` 的 `ScreenshotFolder`（空字串代表預設）；「還原預設」清回空字串 |
| 月份資料夾 | 截圖存在「截圖資料夾\yyyyMM\」，例如 `DataSplitter\202611\`。打開資料夾就知道那個月拍了幾張，整個資料夾刪掉就是刪除那個月 |
| 檔名 | `DataSplitter_yyyyMMdd_HHmmss.png`，同一秒拍多張時加 `_2`、`_3`，不會覆蓋 |
| 未分類 | 截圖資料夾根目錄裡的 PNG（改成月份資料夾之前拍的）列在「未分類」，不會自動搬動，以免動到使用者自己放的檔案 |

## 截圖管理頁（側邊欄「截圖」）

類似檔案總管的「詳細資料 + 預覽窗格」：

- 上方：「拍攝截圖」（同 Alt+F5）、「開啟資料夾」、重新整理；下一行是資料夾位置與張數。
- 左邊清單：依月份資料夾分組，月份由新到舊，最後是「未分類」。月份列顯示資料夾名稱、`2026/11 · 張數 · 總大小`，點箭頭、雙擊、`Enter` 或 `←` `→` 展開 / 收合；預設只展開最新的月份，展開狀態在重新整理後保留。
- 月份底下是截圖：縮圖、檔名、時間、大小，最新的在最上面。雙擊或按 `Enter` 用系統預設的看圖程式開啟；`Delete` 刪除（會先確認）；`Ctrl+C`（macOS `⌘C`）複製圖片。
- 選月份時右邊顯示張數、總大小與資料夾位置，可「開啟這個資料夾」或「刪除整個月份」（會先確認；`Delete` 鍵也可以）。整月刪除會刪掉資料夾裡所有 PNG，資料夾空了就連資料夾一起刪；裡面還有使用者自己放的其他檔案時保留資料夾。刪除「未分類」只刪根目錄的 PNG，不動月份資料夾。
- 右邊預覽：選取那張的原圖，以及「複製」（以圖片格式放進剪貼簿，可直接貼到聊天軟體、Word、小畫家）、「開啟」、「在資料夾中顯示」（Windows 檔案總管 / macOS Finder 會選取該檔；Linux 開啟所在資料夾）、「刪除」。
- 沒有截圖時顯示提示與目前平台的快捷鍵。
- 在別頁拍的截圖，回到這頁時會重新讀取；在這頁拍的會立即出現在最上面並選取。

## 相關檔案

| 檔案 | 內容 |
| --- | --- |
| `Services/ScreenshotService.cs` | 資料夾、月份資料夾、檔名、拍攝、依月份列出（`ListMonths`）、刪除單張與整月（`DeleteMonth`）、複製到剪貼簿、在資料夾中顯示 |
| `Views/MainWindow.Screenshot.cs` | `TakeScreenshot()`：拍攝並顯示通知 |
| `Views/MainWindow.TitleBar.cs` | `Alt+F5` 快捷鍵（Avalonia 把 macOS 的 Option 鍵也當成 `KeyModifiers.Alt`） |
| `Views/ScreenshotsView.axaml(.cs)`、`ViewModels/ScreenshotsViewModel.cs` | 截圖管理頁 |
| `Views/SettingsView.axaml(.cs)`、`ViewModels/SettingsViewModel.cs` | 設定 › 一般設置 › 截圖資料夾 |
| `Assets/Styles/AppStyles.axaml` | `ListBox.screenshot-list`、`Border.screenshot-thumb`、`Border.screenshot-preview` |

## 測試

| 測試 | 內容 |
| --- | --- |
| `tests/ExcelPasteTool.Tests/ScreenshotServiceTests.cs` | 檔名、月份資料夾名稱、同一秒不覆蓋、大小顯示、預設 / 自訂資料夾、依月份列出（未分類、略過非月份資料夾）、整月刪除、刪除 |
| `tests/ExcelPasteTool.UITests/Tests/ScreenshotTests.cs` | Alt+F5 存到月份資料夾、PNG 是視窗大小並跳通知、截圖管理頁（空白、月份清單、選取月份三張基準圖）、展開 / 收合、整月刪除與未分類刪除、刪除單張、複製到剪貼簿、設定變更資料夾 |
| `tests/ExcelPasteTool.E2ETests/Tests/ScreenshotTests.cs` | 真實 exe：Alt+F5 存出 PNG 到當月資料夾、截圖頁顯示 1 張、「複製」後 Windows 剪貼簿有同尺寸的圖片 |

UI 測試把截圖存到暫存資料夾（`MainWindowHost.ScreenshotTestFolder`），畫面上顯示固定路徑（`ScreenshotService.FolderDisplayOverride`），基準圖不會因電腦的使用者名稱不同而改變。端到端測試在 `config.json` 指定 `ScreenshotFolder` 為 `TestResults/e2e/_app/Screenshots`。兩者都不會寫到使用者的「圖片」資料夾。

## macOS 快捷鍵

- 用 `⌥F5`（Option + F5）。Avalonia 在 macOS 把 Option 鍵回報為 `KeyModifiers.Alt`，所以同一段程式碼即可；側邊欄提示與說明文字會自動把 `Alt+F5` 顯示成 `⌥F5`。
- MacBook 鍵盤的 F5 預設是媒體鍵，要按 `fn + ⌥ + F5`（或在系統設定把 F1～F12 設為標準功能鍵）。
- 沒有改用 `⌘⇧3` / `⌘⇧4` / `⌘⇧5`：這幾組是 macOS 系統截圖的快捷鍵，會被系統攔走。
