using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ExcelPasteTool.Helpers;

namespace ExcelPasteTool;

/// <summary>截圖管理頁（側邊欄「截圖」）。畫面與狀態在 ScreenshotsViewModel，這裡只接按鈕與鍵盤。</summary>
public partial class ScreenshotsView : UserControl
{
    private readonly ToastQueueHelper _toast;
    private bool _deleting;

    public ScreenshotsViewModel ViewModel { get; } = new();

    public ScreenshotsView()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = ViewModel;
        _toast = new ToastQueueHelper(this);

        Get<Button>("CaptureScreenshotButton").Click += (_, _) => Capture();
        Get<Button>("OpenScreenshotFolderButton").Click += (_, _) => ScreenshotService.OpenFolder();
        Get<Button>("RefreshScreenshotsButton").Click += (_, _) => ViewModel.Refresh();
        Get<Button>("CopyScreenshotButton").Click += async (_, _) => await CopySelectedAsync();
        Get<Button>("OpenScreenshotButton").Click += (_, _) => OpenSelected();
        Get<Button>("RevealScreenshotButton").Click += (_, _) =>
        {
            if (ViewModel.SelectedItem is { } item) ScreenshotService.RevealInFolder(item.FilePath);
        };

        Get<Button>("OpenMonthFolderButton").Click += (_, _) =>
        {
            if (ViewModel.SelectedGroup is { } group) ScreenshotService.OpenFolder(group.FolderPath);
        };

        var list = Get<ListBox>("ScreenshotList");
        // 雙擊：截圖用預設程式開啟；月份展開 / 收合
        list.DoubleTapped += (_, _) =>
        {
            if (ViewModel.SelectedGroup is { } group) ViewModel.ToggleGroup(group);
            else OpenSelected();
        };
        list.KeyDown += OnListKeyDown;

        // 在其他頁按 Alt+F5 拍的截圖，回到這頁時要看得到；頁面顯示中拍的也立即出現在清單最上面
        ScreenshotService.Changed += OnScreenshotsChanged;
        SettingsViewModel.Instance.ScreenshotFolderChanged += OnScreenshotsChanged;
        AttachedToVisualTree += (_, _) => ViewModel.Refresh();
    }

    private T Get<T>(string name) where T : Control =>
        this.FindControl<T>(name) ?? throw new InvalidOperationException($"ScreenshotsView 找不到 {name}");

    private void OnScreenshotsChanged()
    {
        // 自己刪除時清單已經處理好選取位置，不要再跳回第一張
        if (_deleting || TopLevel.GetTopLevel(this) == null) return;
        Dispatcher.UIThread.Post(() => ViewModel.Refresh(selectFirst: true));
    }

    /// <summary>「拍攝截圖」按鈕：跟 Alt+F5 一樣拍下整個 App 畫面（包含這一頁）。</summary>
    internal void Capture()
    {
        if (TopLevel.GetTopLevel(this) is MainWindow window)
            window.TakeScreenshot();
    }

    internal async Task CopySelectedAsync()
    {
        if (ViewModel.SelectedItem is not { } item) return;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        var ok = clipboard != null && await ScreenshotService.CopyToClipboardAsync(clipboard, item.FilePath);
        _toast.EnqueueToast(ok
                ? LanguageManager.GetText("ScreenshotCopiedToast", "已將截圖複製到剪貼簿")
                : LanguageManager.GetText("ScreenshotCopyFailedToast", "無法把截圖複製到剪貼簿"),
            ok ? ToastSeverity.Success : ToastSeverity.Error);
    }

    private void OpenSelected()
    {
        if (ViewModel.SelectedItem is { } item) ScreenshotService.OpenFile(item.FilePath);
    }

    /// <summary>刪除選取的截圖（確認視窗的「刪除」按鈕、或測試直接呼叫）。</summary>
    internal void DeleteSelected()
    {
        var name = ViewModel.SelectedItem?.FileName;
        if (name == null) return;
        bool ok;
        _deleting = true;
        try { ok = ViewModel.DeleteSelected(); }
        finally { _deleting = false; }
        _toast.EnqueueToast(ok
                ? string.Format(LanguageManager.GetText("ScreenshotDeletedToast", "已刪除截圖：{0}"), name)
                : LanguageManager.GetText("ScreenshotDeleteFailedToast", "無法刪除截圖，檔案可能正在使用中"),
            ok ? ToastSeverity.Success : ToastSeverity.Error);
    }

    /// <summary>刪除選取的整個月份（確認視窗的「刪除整個月份」按鈕、或測試直接呼叫）。</summary>
    internal void DeleteSelectedMonth()
    {
        if (ViewModel.SelectedGroup is not { } group) return;
        var title = group.Title;
        _deleting = true;
        try
        {
            var count = ViewModel.DeleteSelectedMonth();
            _toast.EnqueueToast(string.Format(
                LanguageManager.GetText("ScreenshotMonthDeletedToast", "已刪除「{0}」的 {1} 張截圖"), title, count));
        }
        catch (Exception ex)
        {
            _toast.EnqueueToast(string.Format(
                LanguageManager.GetText("ScreenshotMonthDeleteFailedToast", "無法刪除整個月份：{0}"), ex.Message), ToastSeverity.Error);
        }
        finally
        {
            _deleting = false;
        }
    }

    private void DeleteMonthConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        Get<Button>("DeleteMonthButton").Flyout?.Hide();
        DeleteSelectedMonth();
    }

    /// <summary>月份列左邊的箭頭：展開 / 收合。</summary>
    private void MonthToggle_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is ScreenshotMonthGroup group)
            ViewModel.ToggleGroup(group);
        e.Handled = true;
    }

    private void DeleteConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        Get<Button>("DeleteScreenshotButton").Flyout?.Hide();
        DeleteSelected();
    }

    // 清單上：Enter 開啟、Delete 跳出刪除確認、Ctrl+C（macOS ⌘C）複製圖片；
    // 選到月份時：Enter / → / ← 展開或收合、Delete 跳出「刪除整個月份」確認
    private async void OnListKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel.SelectedGroup is { } group && e.KeyModifiers == KeyModifiers.None)
        {
            if (e.Key == Key.Enter || (e.Key == Key.Right && !group.IsExpanded) || (e.Key == Key.Left && group.IsExpanded))
            {
                e.Handled = true;
                ViewModel.ToggleGroup(group);
            }
            else if (e.Key == Key.Delete)
            {
                e.Handled = true;
                Get<Button>("DeleteMonthButton").Flyout?.ShowAt(Get<Button>("DeleteMonthButton"));
            }
            return;
        }
        if (ViewModel.SelectedItem == null) return;
        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None)
        {
            e.Handled = true;
            OpenSelected();
        }
        else if (e.Key == Key.Delete && e.KeyModifiers == KeyModifiers.None)
        {
            e.Handled = true;
            Get<Button>("DeleteScreenshotButton").Flyout?.ShowAt(Get<Button>("DeleteScreenshotButton"));
        }
        else if (e.Key == Key.C && e.KeyModifiers == MainWindow.PrimaryModifier)
        {
            e.Handled = true;
            await CopySelectedAsync();
        }
    }
}
