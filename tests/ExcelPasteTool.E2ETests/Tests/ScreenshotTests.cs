using ExcelPasteTool.E2ETests.Infrastructure;
using FlaUI.Core.WindowsAPI;

namespace ExcelPasteTool.E2ETests.Tests;

/// <summary>
/// 端到端：在真實的 DataSplitter.exe 按 Alt+F5 截圖，確認真的存出 PNG（真實視窗、DPI 縮放、Skia 繪製），
/// 再到「截圖」頁按「複製」，確認 Windows 剪貼簿裡真的有圖片。截圖存在 TestResults/e2e/_app/Screenshots。
/// </summary>
[Trait("Category", "E2E")]
public class ScreenshotTests
{
    [Fact]
    public void AltF5_SavesPng_AndCopyPutsImageOnClipboard()
    {
        E2EGuard.SkipUnlessEnabled();
        using var s = AppSession.Start(nameof(AltF5_SavesPng_AndCopyPutsImageOnClipboard));

        s.Step("工具頁出現", () => s.Find("PasteDataButton"));
        s.Step("按 Alt+F5", () => s.PressKeys(VirtualKeyShort.ALT, VirtualKeyShort.F5));

        var file = s.Step("截圖資料夾出現一張 PNG", () =>
        {
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < deadline)
            {
                var files = Directory.Exists(s.ScreenshotFolder) ? Directory.GetFiles(s.ScreenshotFolder, "DataSplitter_*.png", SearchOption.AllDirectories) : Array.Empty<string>();
                if (files.Length == 1 && new FileInfo(files[0]).Length > 0) return files[0];
                Thread.Sleep(100);
            }
            throw new TimeoutException($"10 秒內 {s.ScreenshotFolder} 沒有出現截圖");
        });

        var size = s.Step("PNG 可以讀取，尺寸不小於視窗最小尺寸", () =>
        {
            using var image = System.Drawing.Image.FromFile(file);
            s.Log($"   {Path.GetFileName(file)}：{image.Width}×{image.Height}");
            if (image.Width < 900 || image.Height < 560)
                throw new InvalidOperationException($"截圖尺寸 {image.Width}×{image.Height} 比視窗最小尺寸 900×560 還小");
            return image.Size;
        });

        s.Step("存在月份資料夾（yyyyMM）裡", () =>
        {
            var month = Path.GetFileName(Path.GetDirectoryName(file))!;
            if (month != DateTime.Now.ToString("yyyyMM"))
                throw new InvalidOperationException($"截圖應該存在 {DateTime.Now:yyyyMM} 資料夾，實際在 {month}");
        });

        s.Step("切到「截圖」頁", () => s.Invoke("Sidebar_Screenshots"));
        s.Step("清單與預覽出現", () => s.Find("ScreenshotList"));
        s.Step("摘要顯示 1 張", () => s.WaitForText("ScreenshotsSummaryText", t => t.Contains("共 1 張")));
        s.Screenshot("screenshots-page");

        s.Step("清空剪貼簿", ClipboardHelper.Clear);
        s.Step("按「複製」", () => s.Invoke("CopyScreenshotButton"));
        s.Step("剪貼簿裡有同尺寸的圖片", () =>
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            System.Drawing.Size? copied = null;
            while (DateTime.UtcNow < deadline && (copied = ClipboardHelper.GetImageSize()) == null)
                Thread.Sleep(100);
            if (copied == null) throw new InvalidOperationException("剪貼簿裡沒有圖片");
            s.Log($"   剪貼簿圖片：{copied.Value.Width}×{copied.Value.Height}");
            if (copied.Value != size)
                throw new InvalidOperationException($"剪貼簿圖片 {copied.Value} 與截圖檔 {size} 尺寸不同");
        });

        s.Complete();
    }
}
