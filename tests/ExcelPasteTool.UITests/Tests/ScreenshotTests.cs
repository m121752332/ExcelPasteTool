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
/// - Alt+F5 存到「截圖資料夾\yyyyMM\」，PNG 是整個視窗的大小，並跳出通知
/// - 截圖管理頁依月份分組：月份列（張數、大小）可展開 / 收合，最新的月份預設展開，根目錄的舊截圖列為「未分類」
/// - 選月份：顯示張數與資料夾，可刪除整個月份；選截圖：預覽、刪除、複製到剪貼簿
/// - 設定 › 一般設置 › 截圖資料夾：變更與還原預設
/// 截圖一律存在 MainWindowHost.ScreenshotTestFolder（暫存資料夾），不會寫到使用者的「圖片」資料夾。
/// </summary>
public class ScreenshotTests
{
    private static string Folder => MainWindowHost.ScreenshotTestFolder;

    private static string[] PngFiles() =>
        Directory.Exists(Folder) ? Directory.GetFiles(Folder, "*.png", SearchOption.AllDirectories) : Array.Empty<string>();

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

    /// <summary>
    /// 做一張假的截圖：上方深色標題列、下方單色內容，固定修改時間，讓清單順序與基準圖可重現。
    /// month 為 null 時放在截圖資料夾根目錄（分月份之前的舊截圖）。
    /// </summary>
    private static string CreateSample(string? month, string fileName, DateTime modified, uint bodyColor, uint barColor = 0xFF161B22)
    {
        var folder = month == null ? Folder : Path.Combine(Folder, month);
        Directory.CreateDirectory(folder);
        const int width = 320, height = 200;
        var path = Path.Combine(folder, fileName);
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

    /// <summary>11 月兩張、10 月一張、根目錄（未分類）一張。</summary>
    private static (string nov25, string nov24, string oct, string root) SeedMonths() => (
        CreateSample("202611", "DataSplitter_20261125_090000.png", new DateTime(2026, 11, 25, 9, 0, 0), 0xFFD29922),
        CreateSample("202611", "DataSplitter_20261124_090000.png", new DateTime(2026, 11, 24, 9, 0, 0), 0xFF238636),
        CreateSample("202610", "DataSplitter_20261030_090000.png", new DateTime(2026, 10, 30, 9, 0, 0), 0xFF1F6FEB),
        CreateSample(null, "DataSplitter_20260901_090000.png", new DateTime(2026, 9, 1, 9, 0, 0), 0xFF8957E5));

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

    private static string RowText(object row) => row switch
    {
        ScreenshotMonthGroup g => $"[{g.Title}]",
        ScreenshotItem i => i.FileName,
        _ => row.ToString() ?? ""
    };

    [AvaloniaFact]
    public void AltF5_SavesWholeWindowAsPng_InMonthFolder_AndShowsToast()
    {
        using var host = MainWindowHost.Open();
        Assert.Empty(PngFiles());

        host.PressKey(Key.F5, RawInputModifiers.Alt);

        var file = Assert.Single(PngFiles());
        var name = Path.GetFileName(file);
        Assert.Matches(@"^DataSplitter_\d{8}_\d{6}\.png$", name);
        // 存在「截圖資料夾\yyyyMM\」，資料夾名稱跟檔名的年月一致
        var month = Path.GetFileName(Path.GetDirectoryName(file))!;
        Assert.Equal(Path.Combine(Folder, month), Path.GetDirectoryName(file));
        Assert.Equal(name.Substring("DataSplitter_".Length, 6), month);
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
    public void ScreenshotsPage_GroupsByMonth_NewestMonthExpanded_AndPreviewsNewest()
    {
        var files = default((string nov25, string nov24, string oct, string root));
        using var host = OpenScreenshotsPage(out var view, () => files = SeedMonths());
        var vm = view.ViewModel;

        Assert.Equal(new[] { "202611", "202610", "" }, vm.Groups.Select(g => g.Key));
        Assert.Equal(new[] { 2, 1, 1 }, vm.Groups.Select(g => g.Month.Count));
        Assert.True(vm.Groups[2].IsUnsorted);
        // 只有最新的月份展開
        Assert.Equal(new[] { "[202611]", "DataSplitter_20261125_090000.png", "DataSplitter_20261124_090000.png", "[202610]", "[未分類]" },
            vm.Rows.Select(RowText));
        Assert.Equal(files.nov25, vm.SelectedItem!.FilePath);
        Assert.Equal(new PixelSize(320, 200), vm.Preview!.PixelSize);
        Assert.Contains("共 4 張", host.Find<TextBlock>("ScreenshotsSummaryText").Text);
        Assert.StartsWith("2026/11 · 2 張 · ", vm.Groups[0].DetailText);
        VisualAssert.MatchesBaseline(host.Window, "screenshots-list-dark");

        // 展開 10 月
        vm.ToggleGroup(vm.Groups[1]);
        MainWindowHost.Pump();
        Assert.Contains("DataSplitter_20261030_090000.png", vm.Rows.Select(RowText));
        // 收合 11 月：選取的截圖在裡面，選取改成 11 月
        vm.ToggleGroup(vm.Groups[0]);
        MainWindowHost.Pump();
        Assert.DoesNotContain("DataSplitter_20261125_090000.png", vm.Rows.Select(RowText));
        Assert.Same(vm.Groups[0], vm.SelectedGroup);
    }

    [AvaloniaFact]
    public void SelectingMonth_ShowsMonthPanel_AndDeleteMonthRemovesFolder()
    {
        var files = default((string nov25, string nov24, string oct, string root));
        using var host = OpenScreenshotsPage(out var view, () => files = SeedMonths());
        var vm = view.ViewModel;

        host.Find<ListBox>("ScreenshotList").SelectedItem = vm.Groups[0];
        MainWindowHost.Pump();
        Assert.True(host.Find<StackPanel>("MonthDetailPanel").IsVisible);
        Assert.Equal("2026/11（202611）", host.Find<TextBlock>("SelectedMonthTitle").Text);
        Assert.StartsWith("2 張 · ", host.Find<TextBlock>("SelectedMonthDetail").Text);
        Assert.Null(vm.Preview);
        VisualAssert.MatchesBaseline(host.Window, "screenshots-month-dark");

        view.DeleteSelectedMonth();
        MainWindowHost.Pump();

        Assert.False(Directory.Exists(Path.Combine(Folder, "202611")));
        Assert.True(File.Exists(files.oct));
        Assert.True(File.Exists(files.root));
        Assert.Equal(new[] { "202610", "" }, vm.Groups.Select(g => g.Key));
        Assert.Contains(host.Window.GetVisualDescendants().OfType<ToastCard>(), c => c.Message.Contains("202611") && c.Message.Contains('2'));
    }

    [AvaloniaFact]
    public void DeleteUnsortedGroup_OnlyRemovesRootPngs()
    {
        var files = default((string nov25, string nov24, string oct, string root));
        using var host = OpenScreenshotsPage(out var view, () => files = SeedMonths());
        var vm = view.ViewModel;

        vm.SelectedRow = vm.Groups.Single(g => g.IsUnsorted);
        view.DeleteSelectedMonth();
        MainWindowHost.Pump();

        Assert.False(File.Exists(files.root));
        Assert.True(Directory.Exists(Folder));
        Assert.Equal(3, PngFiles().Length);
        Assert.DoesNotContain(vm.Groups, g => g.IsUnsorted);
    }

    [AvaloniaFact]
    public void DeleteSelected_RemovesFile_AndSelectsNextInSameMonth()
    {
        var files = default((string nov25, string nov24, string oct, string root));
        using var host = OpenScreenshotsPage(out var view, () => files = SeedMonths());

        view.DeleteSelected();
        MainWindowHost.Pump();

        Assert.False(File.Exists(files.nov25));
        Assert.Equal(3, view.ViewModel.AllItems.Count());
        Assert.Equal(files.nov24, view.ViewModel.SelectedItem!.FilePath);
        Assert.Contains(host.Window.GetVisualDescendants().OfType<ToastCard>(), c => c.Message.Contains("DataSplitter_20261125_090000.png"));
    }

    [AvaloniaFact]
    public void DeleteLastScreenshot_ShowsEmptyState()
    {
        using var host = OpenScreenshotsPage(out var view, () =>
            CreateSample("202611", "DataSplitter_20261125_090000.png", new DateTime(2026, 11, 25, 9, 0, 0), 0xFFD29922));

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
            CreateSample("202611", "DataSplitter_20261101_090000.png", new DateTime(2026, 11, 1, 9, 0, 0), 0xFFD29922));

        host.Find<Button>("CaptureScreenshotButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        MainWindowHost.Pump();

        Assert.Equal(2, view.ViewModel.AllItems.Count());
        var captured = PngFiles().Single(f => !f.EndsWith("_20261101_090000.png"));
        Assert.Equal(captured, view.ViewModel.SelectedItem!.FilePath);
        Assert.Contains(view.ViewModel.SelectedItem, view.ViewModel.Rows);
        Assert.Same(view.ViewModel.SelectedItem.Group, view.ViewModel.Groups[0]);
    }

    [AvaloniaFact]
    public async Task CopySelected_PutsImageOnClipboard()
    {
        using var host = OpenScreenshotsPage(out var view, () =>
            CreateSample("202611", "DataSplitter_20261125_090000.png", new DateTime(2026, 11, 25, 9, 0, 0), 0xFFD29922));

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

            // 截圖改存到新資料夾（的月份資料夾）
            host.PressKey(Key.F5, RawInputModifiers.Alt);
            Assert.NotEmpty(Directory.GetFiles(custom, "*.png", SearchOption.AllDirectories));

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
