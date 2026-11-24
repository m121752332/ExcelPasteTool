using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Interactivity;
using Avalonia.Layout;
using ExcelPasteTool.Helpers;
using ExcelPasteTool.UITests.Infrastructure;

namespace ExcelPasteTool.UITests.Tests;

/// <summary>工具頁的 SQL 格式選項（欄位名稱、數值不加引號、每行筆數、超過 1000 筆拆組），以及應用程式內通知的位置設定。</summary>
public class SqlOptionsAndToastTests
{
    private static void Click(Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        MainWindowHost.Pump();
    }

    private static T InView<T>(DataToolView view, string name) where T : Control =>
        view.FindControl<T>(name) ?? throw new Xunit.Sdk.XunitException($"找不到 {name}");

    [AvaloniaFact]
    public void SqlOptions_ColumnAndUnquote_ChangeSqlCondition_AndAreSaved()
    {
        using var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        Assert.False(view.IsSqlOptionsOpen);
        view.ShowSqlOptions();
        MainWindowHost.Pump();
        Assert.True(view.IsSqlOptionsOpen);

        host.TypeInto(host.Find<TextBox>("TopBox"), "1|2|3");
        InView<TextBox>(view, "SqlColumnBox").Text = "ima01";
        InView<CheckBox>(view, "SqlUnquoteNumbersBox").IsChecked = true;
        MainWindowHost.Pump();

        Assert.Equal("ima01 IN (1, 2, 3)", host.Find<TextBox>("BottomBox").Text);
        Assert.Equal("ima01", ConfigServices.Config.SqlColumnName);
        Assert.True(ConfigServices.Config.SqlUnquoteNumbers);
        Assert.False(host.Find<TextBlock>("SqlWarningText").IsVisible);
        view.HideSqlOptions();
    }

    /// <summary>
    /// 使用者回報：視窗矮時展開格式選項會把 SQL 輸出框擠出左側面板。格式選項改成浮出選單後，
    /// 在最小視窗、選單開著、又有拆組提醒時，輸出框仍要完整留在左側面板內、保有最小高度。
    /// </summary>
    [AvaloniaFact]
    public void SqlOptionsOpen_OnSmallestWindow_DoesNotPushSqlBoxOutOfPanel()
    {
        using var host = MainWindowHost.Open(width: 900, height: 560);
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        host.TypeInto(host.Find<TextBox>("TopBox"), string.Join("|", Enumerable.Range(1, 1001).Select(i => $"P{i}")));
        view.ShowSqlOptions();
        MainWindowHost.Pump();
        Assert.True(view.IsSqlOptionsOpen);
        Assert.True(host.Find<TextBlock>("SqlWarningText").IsVisible);

        var bottomBox = host.Find<TextBox>("BottomBox");
        var leftGrid = host.Find<Grid>("LeftPanelGrid");
        var sqlSection = host.Find<Grid>("SqlSection");
        // Bounds 是相對父層：BottomBox 在 SqlSection 裡、SqlSection 在 LeftPanelGrid 裡
        Assert.True(bottomBox.Bounds.Height >= 60, $"SQL 輸出框高度只有 {bottomBox.Bounds.Height}");
        Assert.True(bottomBox.Bounds.Bottom <= sqlSection.Bounds.Height + 0.5, $"SQL 輸出框底部 {bottomBox.Bounds.Bottom} 超出 SQL 區塊 {sqlSection.Bounds.Height}");
        Assert.True(sqlSection.Bounds.Bottom <= leftGrid.Bounds.Height + 0.5, $"SQL 區塊底部 {sqlSection.Bounds.Bottom} 超出左側面板 {leftGrid.Bounds.Height}");

        VisualAssert.MatchesBaseline(host.Window, "tools-sql-options-small-dark");
        view.HideSqlOptions();
    }

    [AvaloniaFact]
    public void SqlCondition_Over1000Items_SplitsAndShowsWarning()
    {
        using var host = MainWindowHost.Open();
        host.TypeInto(host.Find<TextBox>("TopBox"), string.Join("|", Enumerable.Range(1, 1001).Select(i => $"P{i}")));

        var sql = host.Find<TextBox>("BottomBox").Text!;
        Assert.StartsWith("(col IN ('P1', ", sql);
        Assert.EndsWith("OR col IN ('P1001'))", sql);
        var warning = host.Find<TextBlock>("SqlWarningText");
        Assert.True(warning.IsVisible);
        Assert.Contains("1001", warning.Text);

        // 填了欄位名稱後，col 換成欄位名稱，提醒只剩拆組那一行
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        InView<TextBox>(view, "SqlColumnBox").Text = "ima01";
        MainWindowHost.Pump();
        Assert.StartsWith("(ima01 IN ('P1', ", host.Find<TextBox>("BottomBox").Text);
        Assert.DoesNotContain("\n", warning.Text);
    }

    [AvaloniaTheory]
    [InlineData(ToastPosition.BottomRight, HorizontalAlignment.Right, VerticalAlignment.Bottom)]
    [InlineData(ToastPosition.TopRight, HorizontalAlignment.Right, VerticalAlignment.Top)]
    [InlineData(ToastPosition.BottomLeft, HorizontalAlignment.Left, VerticalAlignment.Bottom)]
    public void ToastPositionSetting_MovesToastHost_AndShowsCard(ToastPosition position, HorizontalAlignment h, VerticalAlignment v)
    {
        using var host = MainWindowHost.Open();
        var vm = SettingsViewModel.Instance;
        var originalToast = vm.EnqueueToast;
        var toastHost = host.Find<StackPanel>(ToastQueueHelper.HostName);
        try
        {
            // 工具頁測試沒建設定頁，自己接上通知；先切到別的位置，確保這次一定是「變更」
            vm.EnqueueToast = new ToastQueueHelper(host.Window).EnqueueToast;
            vm.SelectedToastPosition = ToastQueueHelper.GetPositionName(
                position == ToastPosition.TopRight ? ToastPosition.BottomLeft : ToastPosition.TopRight);
            toastHost.Children.Clear();
            vm.SelectedToastPosition = ToastQueueHelper.GetPositionName(position);
            MainWindowHost.Pump();

            Assert.Equal(position.ToString(), ConfigServices.Config.ToastPosition);
            Assert.Equal(h, toastHost.HorizontalAlignment);
            Assert.Equal(v, toastHost.VerticalAlignment);

            // 確認訊息直接出現在新位置
            var card = Assert.Single(toastHost.Children.OfType<ToastCard>(), c => !c.IsClosing);
            Assert.Equal(ToastSeverity.Success, card.Severity);
        }
        finally
        {
            vm.SelectedToastPosition = ToastQueueHelper.GetPositionName(ToastPosition.BottomRight);
            vm.EnqueueToast = originalToast;
            toastHost.Children.Clear();
        }
    }

    [AvaloniaFact]
    public void Toasts_StackUpToThree_AndSameMessageIsNotDuplicated()
    {
        using var host = MainWindowHost.Open();
        var helper = new ToastQueueHelper(host.Window);
        var toastHost = host.Find<StackPanel>(ToastQueueHelper.HostName);
        toastHost.Children.Clear();

        helper.EnqueueToast("A");
        helper.EnqueueToast("A");
        MainWindowHost.Pump();
        Assert.Single(toastHost.Children.OfType<ToastCard>());

        helper.EnqueueToast("B", ToastSeverity.Warning);
        helper.EnqueueToast("C", ToastSeverity.Error);
        helper.EnqueueToast("D", ToastSeverity.Info);
        MainWindowHost.Pump();
        var open = toastHost.Children.OfType<ToastCard>().Where(c => !c.IsClosing).Select(c => c.Message);
        Assert.Equal(new[] { "B", "C", "D" }, open);
        toastHost.Children.Clear();
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(5)]
    public void ToastMaxVisibleSetting_LimitsStackedToasts(int limit)
    {
        using var host = MainWindowHost.Open();
        var vm = SettingsViewModel.Instance;
        var toastHost = host.Find<StackPanel>(ToastQueueHelper.HostName);
        try
        {
            vm.ToastMaxVisible = limit;
            toastHost.Children.Clear();
            var helper = new ToastQueueHelper(host.Window);
            foreach (var m in new[] { "1", "2", "3", "4", "5", "6" })
                helper.EnqueueToast(m);
            MainWindowHost.Pump();

            var open = toastHost.Children.OfType<ToastCard>().Where(c => !c.IsClosing).Select(c => c.Message).ToList();
            Assert.Equal(limit, open.Count);
            Assert.Equal("6", open[^1]); // 右下：最新的在最下面
        }
        finally
        {
            vm.ToastMaxVisible = ToastQueueHelper.DefaultMaxVisible;
            toastHost.Children.Clear();
        }
    }
}
