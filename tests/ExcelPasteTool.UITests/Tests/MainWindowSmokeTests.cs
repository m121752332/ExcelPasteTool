using Avalonia.Controls;
using Avalonia.Interactivity;
using ExcelPasteTool.UITests.Infrastructure;

namespace ExcelPasteTool.UITests.Tests;

/// <summary>主視窗冒煙測試：能開起來、預設頁面正確、側邊欄可以收合與切頁。</summary>
public class MainWindowSmokeTests
{
    [AvaloniaFact]
    public void MainWindow_Opens_OnToolsPage()
    {
        using var host = MainWindowHost.Open();

        Assert.True(host.Window.IsVisible);
        Assert.Equal(SidebarPage.Tools, host.Window.Sidebar.SelectedPage);
        Assert.IsType<DataToolView>(host.Find<ContentControl>("MainContent").Content);
        Assert.True(host.Find<Button>("TourButton").IsVisible);
    }

    [AvaloniaFact]
    public void SidebarToggle_ChangesSidebarWidth()
    {
        using var host = MainWindowHost.Open();
        var sidebar = host.Find<Border>("SidebarBorder");
        var before = sidebar.Bounds.Width;

        host.Find<Button>("SidebarToggleButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        MainWindowHost.Pump();

        Assert.NotEqual(before, sidebar.Bounds.Width);
    }

    [AvaloniaFact]
    public void SidebarCommand_SwitchesToSettingsPage_AndHidesTourButton()
    {
        using var host = MainWindowHost.Open();

        host.Window.SidebarItemClickCommand.Execute(SidebarPage.Settings);
        MainWindowHost.Pump();

        Assert.Equal(SidebarPage.Settings, host.Window.Sidebar.SelectedPage);
        Assert.IsType<SettingsView>(host.Find<ContentControl>("MainContent").Content);
        Assert.False(host.Find<Button>("TourButton").IsVisible);
    }
}
