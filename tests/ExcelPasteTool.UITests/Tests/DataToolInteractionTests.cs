using Avalonia.Controls;
using ExcelPasteTool.UITests.Infrastructure;

namespace ExcelPasteTool.UITests.Tests;

/// <summary>工具頁的操作流程：打字、貼上、去重複，並驗證畫面上的結果。</summary>
public class DataToolInteractionTests
{
    [AvaloniaFact]
    public void TypingSeparatedData_FillsTable_AndBuildsSqlCondition()
    {
        using var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;

        host.TypeInto(host.Find<TextBox>("TopBox"), "A01|B01|C01");

        Assert.Equal(new[] { "A01", "B01", "C01" }, view.DataItems.Select(x => x.Value));
        Assert.Equal("('A01', 'B01', 'C01')", host.Find<TextBox>("BottomBox").Text);
        Assert.False(host.Find<TextBlock>("EmptyStateText").IsVisible);
    }

    [AvaloniaFact]
    public async Task PasteFromClipboard_RemovesDuplicates_AndEnablesDuplicatesButton()
    {
        using var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        var clipboard = host.Window.Clipboard!;

        await clipboard.SetTextAsync("A01\r\nB01\r\nA01\r\nC01");
        await view.PasteFromClipboardAsync(clipboard);
        MainWindowHost.Pump();

        Assert.Equal(new[] { "A01", "B01", "C01" }, view.DataItems.Select(x => x.Value));
        Assert.True(host.Find<Button>("DuplicatesButton").IsEnabled);
        Assert.Equal("A01|B01|C01", host.Find<TextBox>("TopBox").Text);
    }

    [AvaloniaFact]
    public async Task ToolsPage_WithData_MatchesBaseline()
    {
        using var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        var clipboard = host.Window.Clipboard!;

        await clipboard.SetTextAsync("A01\r\nB01\r\nC01\r\nB01\r\nD01");
        await view.PasteFromClipboardAsync(clipboard);
        MainWindowHost.Pump();

        VisualAssert.MatchesBaseline(host.Window, "tools-with-data-dark");
    }
}
