using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;

namespace ExcelPasteTool;

/// <summary>
/// MainWindow 的標題列與快捷鍵：
/// - 設定頁「精簡標題列」開啟時，隱藏系統標題列，改用自訂的精簡標題列
///   （只留側邊欄開關與最小化 / 最大化 / 關閉，空白處可拖曳、雙擊放大）。
/// - 快捷鍵：Alt+F4 強制關閉、Alt+Q 開關側邊欄、Alt+P 貼上剪貼簿資料到工具頁右側資料表。
///   快捷鍵只在對應按鈕的提示（ToolTip）中顯示。
/// </summary>
public partial class MainWindow
{
    private const string MaximizeGeometry = "M0.5,0.5 H9.5 V9.5 H0.5 Z";
    private const string RestoreGeometry = "M2.5,0.5 H9.5 V7.5 M0.5,2.5 H7.5 V9.5 H0.5 Z";

    private bool _compactTitleBar;

    private void InitTitleBar()
    {
        ApplyTitleBarMode(ConfigServices.Config.CompactTitleBar);
        SettingsViewModel.Instance.CompactTitleBarChanged += compact =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() => ApplyTitleBarMode(compact));
        LanguageManager.LanguageChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(UpdateTitleBarTexts);
        PropertyChanged += (_, e) =>
        {
            if (e.Property == WindowStateProperty || e.Property == OffScreenMarginProperty)
                UpdateWindowStateVisuals();
        };
        UpdateTitleBarTexts();
    }

    /// <summary>切換系統標題列 / 精簡標題列。</summary>
    private void ApplyTitleBarMode(bool compact)
    {
        _compactTitleBar = compact;
        ExtendClientAreaToDecorationsHint = compact;
        ExtendClientAreaChromeHints = compact ? ExtendClientAreaChromeHints.NoChrome : ExtendClientAreaChromeHints.Default;
        ExtendClientAreaTitleBarHeightHint = compact ? 0 : -1;

        if (this.FindControl<Grid>("CompactTitleBar") is { } bar)
            bar.IsVisible = compact;
        // 側邊欄開關移到標題列，側邊欄頂端的那顆就收起來
        if (this.FindControl<Button>("SidebarToggleButton") is { } toggle)
            toggle.IsVisible = !compact;
        // Toast 往下移，避免蓋到右上角的視窗按鈕
        if (this.FindControl<Border>("ToastPanel") is { } toast)
            toast.Margin = new Thickness(0, compact ? 66 : 30, 30, 0);
        UpdateWindowStateVisuals();
    }

    /// <summary>最大化時內容要避開螢幕外的邊框，並切換「最大化 / 還原」圖示與提示。</summary>
    private void UpdateWindowStateVisuals()
    {
        var maximized = WindowState == WindowState.Maximized;
        if (this.FindControl<Panel>("RootPanel") is { } root)
            root.Margin = _compactTitleBar && maximized ? OffScreenMargin : default;
        if (this.FindControl<Avalonia.Controls.Shapes.Path>("MaximizeIcon") is { } icon)
            icon.Data = Geometry.Parse(maximized ? RestoreGeometry : MaximizeGeometry);
        if (this.FindControl<Button>("MaximizeButton") is { } button)
            ToolTip.SetTip(button, maximized
                ? LanguageManager.GetText("WindowRestoreTooltip", "還原")
                : LanguageManager.GetText("WindowMaximizeTooltip", "最大化"));
    }

    private void UpdateTitleBarTexts()
    {
        var sidebarTip = LanguageManager.GetText("SidebarToggleTooltip", "開關側邊欄 (Alt+Q)");
        if (this.FindControl<Button>("SidebarToggleButton") is { } toggle)
            ToolTip.SetTip(toggle, sidebarTip);
        if (this.FindControl<Button>("TitleBarSidebarButton") is { } barToggle)
            ToolTip.SetTip(barToggle, sidebarTip);
        if (this.FindControl<Button>("MinimizeButton") is { } min)
            ToolTip.SetTip(min, LanguageManager.GetText("WindowMinimizeTooltip", "最小化"));
        if (this.FindControl<Button>("CloseButton") is { } close)
            ToolTip.SetTip(close, LanguageManager.GetText("WindowCloseTooltip", "關閉 (Alt+F4)"));
        UpdateWindowStateVisuals();
    }

    private void TitleBarDragArea_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (e.ClickCount == 2)
            ToggleMaximize();
        else
            BeginMoveDrag(e);
        e.Handled = true;
    }

    private void ToggleMaximize() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void MinimizeButton_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object? sender, RoutedEventArgs e) => ToggleMaximize();

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();

    // ===== 快捷鍵 =====

    /// <summary>處理 Alt 快捷鍵；有處理回傳 true。</summary>
    private bool HandleAltShortcut(KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.Alt) return false;

        // Alt+F4：強制關閉（導覽中、有對話框時也一律結束程式）
        if (e.Key == Key.F4)
        {
            e.Handled = true;
            ForceClose();
            return true;
        }

        // 導覽進行中不處理其他快捷鍵，交給導覽本身
        if (Tour.IsActive) return false;

        switch (e.Key)
        {
            case Key.Q:
                e.Handled = true;
                Sidebar.Toggle();
                return true;
            case Key.P:
                e.Handled = true;
                PasteToDataTool();
                return true;
        }
        return false;
    }

    private void ForceClose()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
        else
            Close();
    }

    /// <summary>Alt+P：切到「工具」頁，把剪貼簿內容貼到右側資料表（等同按「資料貼上」）。</summary>
    private async void PasteToDataTool()
    {
        if (Sidebar.SelectedPage != SidebarPage.Tools)
            OnSidebarItemClick(SidebarPage.Tools);
        if (this.FindControl<ContentControl>("MainContent")?.Content is DataToolView tool)
            await tool.PasteFromClipboardAsync(Clipboard);
    }
}
