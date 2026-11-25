using Avalonia.Controls;
using ExcelPasteTool.Helpers;

namespace ExcelPasteTool;

/// <summary>
/// MainWindow 的截圖：Alt+F5（macOS ⌥F5）或截圖管理頁的「拍攝截圖」按鈕。
/// 拍下整個 App 視窗的內容（不含系統標題列），存到截圖資料夾，完成後用通知卡片提示檔名。
/// 畫面上正在顯示的通知卡片不會被拍進去。
/// </summary>
public partial class MainWindow
{
    private ToastQueueHelper? _screenshotToast;

    /// <summary>拍下目前的 App 畫面；成功回傳檔案路徑，失敗回傳 null（兩種情況都會跳通知）。</summary>
    public string? TakeScreenshot()
    {
        _screenshotToast ??= new ToastQueueHelper(this);
        var toastHost = this.FindControl<StackPanel>(ToastQueueHelper.HostName);
        var previousOpacity = toastHost?.Opacity ?? 1;
        try
        {
            // 通知卡片先透明，避免上一則通知被拍進截圖；繪製是同步的，畫面上不會閃
            if (toastHost != null) toastHost.Opacity = 0;
            var path = ScreenshotService.Capture(this);
            if (toastHost != null) toastHost.Opacity = previousOpacity;
            _screenshotToast.EnqueueToast(string.Format(
                LanguageManager.GetText("ScreenshotSavedToast", "已儲存截圖：{0}"), Path.GetFileName(path)));
            return path;
        }
        catch (Exception ex)
        {
            if (toastHost != null) toastHost.Opacity = previousOpacity;
            _screenshotToast.EnqueueToast(string.Format(
                LanguageManager.GetText("ScreenshotFailedToast", "截圖失敗：{0}"), ex.Message), ToastSeverity.Error);
            return null;
        }
    }
}
