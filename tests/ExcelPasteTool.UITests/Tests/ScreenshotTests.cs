using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using ExcelPasteTool.Helpers;
using ExcelPasteTool.UITests.Infrastructure;

namespace ExcelPasteTool.UITests.Tests;

/// <summary>
/// 截圖（Alt+F5）與截圖管理頁：
/// - Alt+F5 存出整個視窗大小的 PNG、跳出通知
/// - 截圖管理頁：空白狀態、清單由新到舊、預覽、刪除、複製到剪貼簿、在頁面上拍照後清單即時更新
/// - 設定 › 一般設置 › 截圖資料夾：變更與還原預設
/// 截圖一律存在 MainWindowHost.ScreenshotTestFolder（暫存資料夾），不會寫到使用者的「圖片」資料夾。
/// </summary>
public class ScreenshotTests
{
    private static string Folder => MainWindowHost.ScreenshotTestFolder;

    private static string[] PngFiles() =>
        Directory.Exists(Folder) ? Directory.GetFiles(Folder, "*.png") : Array.Empty<string>();

    /// <summary>開主視窗、放好假截圖（MainWindowHost.Open 會先清空截圖資料夾，所以要在開窗之後才放），再切到截圖管理頁。</summary>
    private static MainWindowHost OpenScreenshotsPage(out ScreenshotsView view, Action? seed = null)
    {
        var host = MainWindowHost.Open();
        seed?.Invoke();
        host.Window.SidebarItemClickCommand.Execute(SidebarPage.Screenshots);
        MainWindowHost.Pump();
        view = (ScreenshotsView)host.Find<ContentControl>("MainContent").Content!;
        return host;
    }

    /// <summary>做一張假的截圖：上方深色標題列、下方單色內容，固定修改時間，讓清單順序與基準圖可重現。</summary>
    private static string CreateSample(string fileName, DateTime modified, uint bodyColor, uint barColor = 0xFF161B22)
    {
        Directory.CreateDirectory(Folder);
        const int width = 320, height = 200;
        var path = Path.Combine(Folder, fileName);
        using (var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul))
        {
            using (var frame = bitmap.Lock())
            {
                var row = new int[width];
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                        row[x] = unchecked((int)(y < 28 || x < 40 ? barColor : bodyColor));
                    Marshal.Copy(row, 0, frame.Address + y * frame.RowBytes, width);
                }
            }
            bitmap.Save(path, PngBitmapEncoderOptions.Default);
        }
        File.SetLastWriteTime(path, modified);
        return path;
    }

    private static int CountDistinctPixels(Bitmap bitmap)
    {
        var size = bitmap.PixelSize;
        var pixels = new int[size.Width * size.Height];
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new PixelRect(size), handle.AddrOfPinnedObject(), pixels.Length * 4, size.Width * 4);
        }
        finally
        {
            handle.Free();
        }
        return pixels.Distinct().Take(50).Count();
    }

    [AvaloniaFact]
    public void AltF5_SavesWholeWindowAsPng_AndShowsToast()
    {
        using var host = MainWindowHost.Open();
        Assert.Empty(PngFiles());

        host.PressKey(Key.F5, RawInputModifiers.Alt);

        var file = Assert.Single(PngFiles());
        var name = Path.GetFileName(file);
        Assert.Matches(@"^DataSplitter_\d{8}_\d{6}\.png$", name);
        using (var bitmap = new Bitmap(file))
        {
            Assert.Equal(new PixelSize((int)MainWindowHost.DefaultWidth, (int)MainWindowHost.DefaultHeight), bitmap.PixelSize);
            Assert.True(CountDistinctPixels(bitmap) > 10, "截圖看起來是一片空白");
        }
        Assert.Contains(host.Window.GetVisualDescendants().OfType<ToastCard>(), c => c.Message.Contains(name));
    }

    [AvaloniaFact]
    public void AltF5_Twice_KeepsBothFiles()
    {
        using var host = MainWindowHost.Open();
        host.PressKey(Key.F5, RawInputModifiers.Alt);
        host.PressKey(Key.F5, RawInputModifiers.Alt);
        Assert.Equal(2, PngFiles().Length);
    }

    [AvaloniaFact]
    public void SidebarTooltip_ShowsShortcut()
    {
        using var host = MainWindowHost.Open();
        var item = host.Window.SidebarItems.Single(i => i.Page == SidebarPage.Screenshots);
        Assert.Contains(OperatingSystem.IsMacOS() ? "⌥F5" : "Alt+F5", item.ToolTipText);
    }

    [AvaloniaFact]
    public void ScreenshotsPage_Empty_MatchesBaseline()
    {
        using var host = OpenScreenshotsPage(out var view);
        Assert.True(view.ViewModel.IsEmpty);
        Assert.True(host.Find<Border>("ScreenshotsEmptyPanel").IsVisible);
        Assert.False(host.Find<Grid>("ScreenshotsBrowser").IsVisible);
        Assert.Contains(MainWindowHost.ScreenshotDisplayFolder, host.Find<TextBlock>("ScreenshotsSummaryText").Text);
        VisualAssert.MatchesBaseline(host.Window, "screenshots-empty-dark");
    }

    [AvaloniaFact]
    public void ScreenshotsPage_ListsNewestFirst_AndPreviewsSelected()
    {
        string oldest = "", middle = "", newest = "";
        using var host = OpenScreenshotsPage(out var view, () =>
        {
            oldest = CreateSample("DataSplitter_20261123_090000.png", new DateTime(2026, 11, 23, 9, 0, 0), 0xFF1F6FEB);
            middle = CreateSample("DataSplitter_20261124_090000.png", new DateTime(2026, 11, 24, 9, 0, 0), 0xFF238636);
            newest = CreateSample("DataSplitter_20261125_090000.png", new DateTime(2026, 11, 25, 9, 0, 0), 0xFFD29922);
        });
        var vm = view.ViewModel;

        Assert.Equal(new[] { newest, middle, oldest }, vm.Items.Select(i => i.FilePath));
        Assert.Same(vm.Items[0], vm.SelectedItem);
        Assert.NotNull(vm.Preview);
        Assert.Equal(new PixelSize(320, 200), vm.Preview!.PixelSize);
        Assert.Equal("DataSplitter_20261125_090000.png", host.Find<TextBlock>("SelectedScreenshotName").Text);
        Assert.Contains("共 3 張", host.Find<TextBlock>("ScreenshotsSummaryText").Text);
        VisualAssert.MatchesBaseline(host.Window, "screenshots-list-dark");

        host.Find<ListBox>("ScreenshotList").SelectedIndex = 2;
        MainWindowHost.Pump();
        Assert.Equal(oldest, vm.SelectedItem!.FilePath);
        Assert.Equal("DataSplitter_20261123_090000.png", host.Find<TextBlock>("SelectedScreenshotName").Text);
    }

    [AvaloniaFact]
    public void DeleteSelected_RemovesFile_AndSelectsNext()
    {
        string middle = "", newest = "";
        using var host = OpenScreenshotsPage(out var view, () =>
        {
            CreateSample("DataSplitter_20261123_090000.png", new DateTime(2026, 11, 23, 9, 0, 0), 0xFF1F6FEB);
            middle = CreateSample("DataSplitter_20261124_090000.png", new DateTime(2026, 11, 24, 9, 0, 0), 0xFF238636);
            newest = CreateSample("DataSplitter_20261125_090000.png", new DateTime(2026, 11, 25, 9, 0, 0), 0xFFD29922);
        });
        view.DeleteSelected();
        MainWindowHost.Pump();

        Assert.False(File.Exists(newest));
        Assert.Equal(2, view.ViewModel.Items.Count);
        Assert.Equal(middle, view.ViewModel.SelectedItem!.FilePath);
        Assert.Contains(host.Window.GetVisualDescendants().OfType<ToastCard>(), c => c.Message.Contains("DataSplitter_20261125_090000.png"));
    }

    [AvaloniaFact]
    public void DeleteLastScreenshot_ShowsEmptyState()
    {
        using var host = OpenScreenshotsPage(out var view, () =>
            CreateSample("DataSplitter_20261125_090000.png", new DateTime(2026, 11, 25, 9, 0, 0), 0xFFD29922));

        view.DeleteSelected();
        MainWindowHost.Pump();

        Assert.True(view.ViewModel.IsEmpty);
        Assert.Null(view.ViewModel.Preview);
        Assert.True(host.Find<Border>("ScreenshotsEmptyPanel").IsVisible);
    }

    [AvaloniaFact]
    public void CaptureOnScreenshotsPage_AddsNewestOnTop_AndSelectsIt()
    {
        using var host = OpenScreenshotsPage(out var view, () =>
            CreateSample("DataSplitter_20261125_090000.png", new DateTime(2026, 11, 25, 9, 0, 0), 0xFFD29922));

        host.Find<Button>("CaptureScreenshotButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        MainWindowHost.Pump();

        Assert.Equal(2, view.ViewModel.Items.Count);
        var captured = PngFiles().Single(f => !f.EndsWith("_20261125_090000.png"));
        Assert.Equal(captured, view.ViewModel.Items[0].FilePath);
        Assert.Same(view.ViewModel.Items[0], view.ViewModel.SelectedItem);
    }

    [AvaloniaFact]
    public async Task CopySelected_PutsImageOnClipboard()
    {
        using var host = OpenScreenshotsPage(out var view, () =>
            CreateSample("DataSplitter_20261125_090000.png", new DateTime(2026, 11, 25, 9, 0, 0), 0xFFD29922));

        await view.CopySelectedAsync();
        MainWindowHost.Pump();

        using var copied = await host.Window.Clipboard!.TryGetBitmapAsync();
        Assert.NotNull(copied);
        Assert.Equal(new PixelSize(320, 200), copied!.PixelSize);
        Assert.Contains(host.Window.GetVisualDescendants().OfType<ToastCard>(), c => c.Severity == ToastSeverity.Success);
    }

    [AvaloniaFact]
    public void SettingsGeneral_ScreenshotFolder_ChangeAndReset()
    {
        using var host = MainWindowHost.Open();
        host.Window.SidebarItemClickCommand.Execute(SidebarPage.Settings);
        MainWindowHost.Pump();
        host.Find<Button>("GeneralCategoryButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        MainWindowHost.Pump();

        var desc = host.Find<TextBlock>("ScreenshotFolderDescText");
        Assert.Contains(MainWindowHost.ScreenshotDisplayFolder, desc.Text);
        Assert.Contains("預設", desc.Text);
        Assert.False(host.Find<Button>("ResetScreenshotFolderButton").IsEnabled);

        var custom = Path.Combine(Path.GetTempPath(), "DataSplitter.UITests", "CustomShots");
        var vm = SettingsViewModel.Instance;
        ScreenshotService.FolderDisplayOverride = null;
        try
        {
            vm.SetScreenshotFolder(custom);
            MainWindowHost.Pump();
            Assert.Equal(custom, ScreenshotService.Folder);
            Assert.Contains(custom, desc.Text);
            Assert.True(host.Find<Button>("ResetScreenshotFolderButton").IsEnabled);

            // 截圖改存到新資料夾
            host.PressKey(Key.F5, RawInputModifiers.Alt);
            Assert.NotEmpty(Directory.GetFiles(custom, "*.png"));

            host.Find<Button>("ResetScreenshotFolderButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            MainWindowHost.Pump();
            Assert.Equal("", ConfigServices.Config.ScreenshotFolder);
            Assert.Equal(MainWindowHost.ScreenshotTestFolder, ScreenshotService.Folder);
            Assert.False(host.Find<Button>("ResetScreenshotFolderButton").IsEnabled);
        }
        finally
        {
            ConfigServices.Config.ScreenshotFolder = "";
            try { Directory.Delete(custom, recursive: true); } catch { }
        }
    }
}
