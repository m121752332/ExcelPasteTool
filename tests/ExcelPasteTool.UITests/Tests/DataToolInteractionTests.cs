using Avalonia.VisualTree;
using Avalonia.Input.Platform;
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

    [AvaloniaFact]
    public void ToggleSqlPanelButton_HidesAndShowsSqlCondition()
    {
        using var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        var button = host.Find<Button>("ToggleSqlPanelButton");
        Assert.True(host.Find<TextBox>("BottomBox").IsVisible);

        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        MainWindowHost.Pump();
        Assert.False(view.IsSqlPanelVisible);
        Assert.False(host.Find<TextBox>("BottomBox").IsVisible);
        // 隱藏後仍會照常組 SQL 條件，再打開時內容是最新的；眼睛按鈕不會改設定
        host.TypeInto(host.Find<TextBox>("TopBox"), "A01|B01");
        Assert.True(ConfigServices.Config.ShowSqlPanel);
        VisualAssert.MatchesBaseline(host.Window, "tools-sql-hidden-dark");

        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        MainWindowHost.Pump();
        Assert.True(host.Find<TextBox>("BottomBox").IsVisible);
        Assert.Equal("('A01', 'B01')", host.Find<TextBox>("BottomBox").Text);
    }

    [AvaloniaFact]
    public void ShowSqlPanelSetting_Off_HidesSqlConditionImmediately()
    {
        using var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        try
        {
            SettingsViewModel.Instance.ShowSqlPanel = false;
            MainWindowHost.Pump();
            Assert.False(view.IsSqlPanelVisible);
            Assert.False(host.Find<TextBox>("BottomBox").IsVisible);
        }
        finally
        {
            SettingsViewModel.Instance.ShowSqlPanel = true;
        }
        Assert.True(view.IsSqlPanelVisible);
    }

    private static TextBox EditorFor(MainWindowHost host, DataItem item) =>
        host.Window.GetVisualDescendants().OfType<TextBox>()
            .First(t => t.Classes.Contains("cell-editor") && ReferenceEquals(t.DataContext, item));

    [AvaloniaFact]
    public void EditCell_UpdatesSeparatedDataAndSql_DuplicateIsRejected_EmptyRemovesRow()
    {
        using var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        host.TypeInto(host.Find<TextBox>("TopBox"), "A01|B01|C01");

        // 修改第 2 筆
        var item = view.DataItems[1];
        var editor = EditorFor(host, item);
        view.BeginEdit(item, editor);
        MainWindowHost.Pump();
        Assert.True(editor.IsVisible);
        Assert.Equal("B01", editor.Text);
        view.CommitEdit(item, " B02 ");
        MainWindowHost.Pump();
        Assert.False(item.IsEditing);
        Assert.Equal("A01|B02|C01", host.Find<TextBox>("TopBox").Text);
        Assert.Equal("('A01', 'B02', 'C01')", host.Find<TextBox>("BottomBox").Text);
        Assert.Same(item, view.DataItems[1]); // 修改後不會整個重建資料表

        // 改成和別筆重複：不修改
        view.BeginEdit(item, editor);
        view.CommitEdit(item, "A01");
        MainWindowHost.Pump();
        Assert.Equal("B02", item.Value);
        Assert.Equal("A01|B02|C01", host.Find<TextBox>("TopBox").Text);

        // 清空：刪除這筆，行號重排
        view.BeginEdit(item, editor);
        view.CommitEdit(item, "");
        MainWindowHost.Pump();
        Assert.Equal(new[] { "A01", "C01" }, view.DataItems.Select(x => x.Value));
        Assert.Equal(2, view.DataItems[1].RowNumber);
        Assert.Equal("('A01', 'C01')", host.Find<TextBox>("BottomBox").Text);
    }

    [AvaloniaFact]
    public async Task AppendPaste_AddsAfterExisting_SkipsDuplicates()
    {
        using var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        var clipboard = host.Window.Clipboard!;
        host.TypeInto(host.Find<TextBox>("TopBox"), "A01|B01");

        await clipboard.SetTextAsync("B01\r\nC01\r\nC01\r\nD01");
        await view.AppendFromClipboardAsync(clipboard);
        MainWindowHost.Pump();

        Assert.Equal(new[] { "A01", "B01", "C01", "D01" }, view.DataItems.Select(x => x.Value));
        Assert.Equal(new[] { 1, 2, 3, 4 }, view.DataItems.Select(x => x.RowNumber));
        Assert.Equal("A01|B01|C01|D01", host.Find<TextBox>("TopBox").Text);
        Assert.Equal("('A01', 'B01', 'C01', 'D01')", host.Find<TextBox>("BottomBox").Text);
        Assert.True(host.Find<Button>("DuplicatesButton").IsEnabled); // B01、C01 被略過
    }

    [AvaloniaFact]
    public void InsertBelowSelected_AddsEditableRow_CommitSyncs_EmptyOrDuplicateIsDropped()
    {
        using var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        host.TypeInto(host.Find<TextBox>("TopBox"), "A01|B01|C01");

        view.SelectRow(view.DataItems[0]);
        var item = view.InsertBelowSelected()!;
        MainWindowHost.Pump();
        MainWindowHost.Pump();
        Assert.Same(item, view.DataItems[1]);
        Assert.True(item.IsEditing);
        Assert.True(item.IsSelected);
        // 暫存列還沒輸入時，分隔資料與 SQL 不受影響
        Assert.Equal("A01|B01|C01", host.Find<TextBox>("TopBox").Text);

        view.CommitEdit(item, "A02");
        MainWindowHost.Pump();
        Assert.Equal(new[] { "A01", "A02", "B01", "C01" }, view.DataItems.Select(x => x.Value));
        Assert.Equal(new[] { 1, 2, 3, 4 }, view.DataItems.Select(x => x.RowNumber));
        Assert.Equal("A01|A02|B01|C01", host.Find<TextBox>("TopBox").Text);

        // 新增後輸入重複值或空白：暫存列直接拿掉
        var dup = view.InsertBelowSelected()!;
        MainWindowHost.Pump();
        MainWindowHost.Pump();
        view.CommitEdit(dup, "C01");
        var empty = view.AddNewRow(view.DataItems.Count)!;
        MainWindowHost.Pump();
        MainWindowHost.Pump();
        view.CommitEdit(empty, "  ");
        MainWindowHost.Pump();
        Assert.Equal(new[] { "A01", "A02", "B01", "C01" }, view.DataItems.Select(x => x.Value));
        Assert.Equal("('A01', 'A02', 'B01', 'C01')", host.Find<TextBox>("BottomBox").Text);
    }

    [AvaloniaFact]
    public void AddRowButton_AddsRowAtEnd_AndMatchesBaseline()
    {
        using var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        host.TypeInto(host.Find<TextBox>("TopBox"), "A01|B01|C01");

        host.Find<Button>("AddRowButton").RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        MainWindowHost.Pump();
        MainWindowHost.Pump();
        var item = view.DataItems[^1];
        Assert.True(item.IsNew);
        Assert.True(item.IsEditing);
        Assert.Equal(4, item.RowNumber);
        VisualAssert.MatchesBaseline(host.Window, "tools-add-row-dark");
    }
    [AvaloniaFact]
    public void AddRowButton_PressedRepeatedly_KeepsSinglePendingRow_AndDoesNotCountIt()
    {
        using var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        host.TypeInto(host.Find<TextBox>("TopBox"), "A01|B01|C01");
        var button = host.Find<Button>("AddRowButton");

        // 連按多次（包含新的一列還沒進入編輯前就再按）
        for (int i = 0; i < 4; i++)
        {
            button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            if (i % 2 == 1) MainWindowHost.Pump();
        }
        MainWindowHost.Pump();
        MainWindowHost.Pump();

        Assert.Equal(4, view.DataItems.Count);
        var pending = Assert.Single(view.DataItems, x => x.IsNew);
        Assert.Same(pending, view.DataItems[^1]);
        Assert.True(pending.IsEditing);
        Assert.Equal("共 3 筆", host.Find<TextBlock>("RowCountText").Text);

        // 輸入後才算一筆
        view.CommitEdit(pending, "D01");
        MainWindowHost.Pump();
        Assert.Equal("共 4 筆", host.Find<TextBlock>("RowCountText").Text);
        Assert.Equal("A01|B01|C01|D01", host.Find<TextBox>("TopBox").Text);
    }

    [AvaloniaFact]
    public void Insert_PressedRepeatedly_MovesSinglePendingRow()
    {
        using var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        host.TypeInto(host.Find<TextBox>("TopBox"), "A01|B01|C01");

        view.SelectRow(view.DataItems[0]);
        var first = view.InsertBelowSelected()!;
        var again = view.InsertBelowSelected()!; // 選取的是暫存列本身：沿用
        MainWindowHost.Pump();
        MainWindowHost.Pump();
        Assert.Same(first, again);
        Assert.Equal(4, view.DataItems.Count);

        // 換到別列再按 Insert：暫存列移過去，不會多一列
        view.SelectRow(view.DataItems[3]);
        var moved = view.InsertBelowSelected()!;
        MainWindowHost.Pump();
        MainWindowHost.Pump();
        Assert.Equal(4, view.DataItems.Count);
        Assert.Single(view.DataItems, x => x.IsNew);
        Assert.Same(moved, view.DataItems[^1]);
        Assert.Equal(new[] { 1, 2, 3, 4 }, view.DataItems.Select(x => x.RowNumber));
        Assert.Equal("A01|B01|C01", host.Find<TextBox>("TopBox").Text);
        Assert.Equal("共 3 筆", host.Find<TextBlock>("RowCountText").Text);
    }
}
