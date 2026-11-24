using Avalonia.Input.Platform;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ExcelPasteTool.UITests.Infrastructure;

namespace ExcelPasteTool.UITests.Tests;

/// <summary>
/// 右欄按鈕列在不同寬度下都不能被裁切：
/// 寬時「重複項」「清空資料」顯示圖示＋文字；右欄低於 <see cref="DataToolView.CompactToolbarThreshold"/> 時只顯示圖示，重複數改角標。
/// 窄的情境用拖曳分隔線的方式（把左欄撐寬）重現使用者截圖裡的狀況。
/// </summary>
public class ToolbarLayoutTests
{
    private static async Task<(MainWindowHost Host, DataToolView View)> OpenWithDuplicatesAsync(double? rightPanelWidth)
    {
        var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        if (rightPanelWidth is { } width)
        {
            // 等同把中間的分隔線往右拖，讓右欄變窄（GridSplitter 拖曳後也是把欄寬改成固定像素）
            var grid = (Grid)host.Find<Border>("RightPanel").Parent!;
            grid.ColumnDefinitions[0].Width = new GridLength(Math.Max(0, grid.Bounds.Width - 6 - width));
            grid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
        }
        var clipboard = host.Window.Clipboard!;
        await clipboard.SetTextAsync("A01\r\nB01\r\nA01\r\nC01\r\nB01\r\nD01");
        await view.PasteFromClipboardAsync(clipboard);
        MainWindowHost.Pump();
        return (host, view);
    }

    /// <summary>按鈕列裡每個看得見的按鈕，都必須完整落在右欄面板的內容範圍內。</summary>
    private static void AssertToolbarNotClipped(MainWindowHost host)
    {
        var panel = host.Find<Border>("RightPanel");
        var content = new Rect(panel.Bounds.Size).Deflate(panel.Padding);
        foreach (var name in new[] { "PasteDataButton", "DuplicatesButton", "ClearDataButton" })
        {
            var button = host.Find<Button>(name);
            var topLeft = button.TranslatePoint(new Point(0, 0), panel)!.Value;
            var rect = new Rect(topLeft, button.Bounds.Size);
            Assert.True(content.Contains(rect),
                $"{name} 超出右欄範圍：按鈕 {rect}，可用 {content}（右欄寬 {panel.Bounds.Width:0}）");
        }
    }

    [AvaloniaFact]
    public async Task WideRightPanel_ShowsTextLabels_AndMatchesBaseline()
    {
        var (host, view) = await OpenWithDuplicatesAsync(null);
        using var _ = host;

        Assert.True(host.Find<Border>("RightPanel").Bounds.Width >= DataToolView.CompactToolbarThreshold);
        Assert.False(view.IsToolbarCompact);
        Assert.True(host.Find<TextBlock>("ClearButtonText").IsVisible);
        Assert.True(host.Find<TextBlock>("DuplicatesButtonText").IsVisible);
        Assert.False(host.Find<Border>("DuplicatesCountBadge").IsVisible);
        AssertToolbarNotClipped(host);
        VisualAssert.MatchesBaseline(host.Window, "toolbar-wide-dark");
    }

    [AvaloniaFact]
    public async Task NarrowRightPanel_ShowsIconsWithCornerBadge_AndMatchesBaseline()
    {
        var (host, view) = await OpenWithDuplicatesAsync(380);
        using var _ = host;

        Assert.True(host.Find<Border>("RightPanel").Bounds.Width < DataToolView.CompactToolbarThreshold);
        Assert.True(view.IsToolbarCompact);
        Assert.False(host.Find<TextBlock>("ClearButtonText").IsVisible);
        Assert.False(host.Find<TextBlock>("DuplicatesButtonText").IsVisible);
        Assert.True(host.Find<Border>("DuplicatesCountBadge").IsVisible);
        Assert.Equal("2", host.Find<TextBlock>("DuplicatesCountText").Text);
        Assert.True(host.Find<TextBlock>("PasteButtonText").IsVisible);
        Assert.NotNull(ToolTip.GetTip(host.Find<Button>("ClearDataButton")));
        AssertToolbarNotClipped(host);
        VisualAssert.MatchesBaseline(host.Window, "toolbar-narrow-dark");
    }

    [AvaloniaFact]
    public async Task NarrowestRightPanel_English_ToolbarNotClipped()
    {
        // 右欄最小寬度 320 ＋ 英文（文字較長）是最擠的情況
        var (host, _) = await OpenWithDuplicatesAsync(320);
        using var __ = host;
        LanguageManager.CurrentLanguage = AppLanguage.English;
        MainWindowHost.Pump();
        MainWindowHost.Pump();
        Assert.True(host.Find<Border>("RightPanel").Bounds.Width <= 330);
        AssertToolbarNotClipped(host);
    }

    [AvaloniaFact]
    public void RowCountBadge_IsInTableHeader()
    {
        using var host = MainWindowHost.Open();
        var badge = host.Find<Border>("RowCountBadge");
        Assert.Contains(badge.GetVisualAncestors(), a => a is Border { Name: "TableContainer" });
    }
}
