using Avalonia.Controls;
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

    [AvaloniaFact]
    public void SqlOptions_ColumnAndUnquote_ChangeSqlCondition_AndAreSaved()
    {
        using var host = MainWindowHost.Open();
        Assert.False(host.Find<Border>("SqlOptionsPanel").IsVisible);
        Click(host.Find<Button>("SqlOptionsButton"));
        Assert.True(host.Find<Border>("SqlOptionsPanel").IsVisible);

        host.TypeInto(host.Find<TextBox>("TopBox"), "1|2|3");
        host.Find<TextBox>("SqlColumnBox").Text = "ima01";
        host.Find<CheckBox>("SqlUnquoteNumbersBox").IsChecked = true;
        MainWindowHost.Pump();

        Assert.Equal("ima01 IN (1, 2, 3)", host.Find<TextBox>("BottomBox").Text);
        Assert.Equal("ima01", ConfigServices.Config.SqlColumnName);
        Assert.True(ConfigServices.Config.SqlUnquoteNumbers);
        Assert.False(host.Find<TextBlock>("SqlWarningText").IsVisible);

        VisualAssert.MatchesBaseline(host.Window, "tools-sql-options-dark");
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
        host.Find<TextBox>("SqlColumnBox").Text = "ima01";
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
}
