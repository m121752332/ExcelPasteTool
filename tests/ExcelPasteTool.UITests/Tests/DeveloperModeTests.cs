using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ExcelPasteTool.UITests.Infrastructure;

namespace ExcelPasteTool.UITests.Tests;

/// <summary>
/// 開發者模式：設定頁開關（預設關閉、存檔）、F12 開啟內建開發者工具（關閉時不可開）、
/// 視覺樹選取、屬性列表、在 App 上標出選取的控制項、Ctrl+Shift+點選直接選取。
/// </summary>
public class DeveloperModeTests
{
    private static DevToolsWindow PressF12AndGetDevTools(MainWindowHost host)
    {
        host.PressKey(Key.F12);
        return DeveloperModeService.Current ?? throw new Xunit.Sdk.XunitException("F12 沒有開啟開發者工具");
    }

    [AvaloniaFact]
    public void Toggle_DefaultsOff_AndSavesToConfig()
    {
        Assert.False(new AppConfig().DeveloperMode);

        using var host = MainWindowHost.Open();
        host.Window.SidebarItemClickCommand.Execute(SidebarPage.Settings);
        MainWindowHost.Pump();
        host.Find<Button>("GeneralCategoryButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        MainWindowHost.Pump();

        var toggle = host.Find<ToggleSwitch>("DeveloperModeToggle");
        Assert.True(toggle.IsVisible);
        Assert.False(toggle.IsChecked);

        toggle.IsChecked = true;
        MainWindowHost.Pump();
        Assert.True(ConfigServices.Config.DeveloperMode);
        Assert.True(SettingsViewModel.Instance.DeveloperMode);

        toggle.IsChecked = false;
        MainWindowHost.Pump();
        Assert.False(ConfigServices.Config.DeveloperMode);
    }

    [AvaloniaFact]
    public void F12_WhenDisabled_DoesNothing()
    {
        using var host = MainWindowHost.Open();
        host.PressKey(Key.F12);

        Assert.Null(DeveloperModeService.Current);
        Assert.Null(DeveloperModeService.Open(host.Window));
        Assert.Equal(SidebarPage.Tools, host.Window.Sidebar.SelectedPage);
    }

    [AvaloniaFact]
    public void F12_WhenEnabled_OpensDevToolsOnMainWindow_AndAgainReusesIt()
    {
        using var host = MainWindowHost.Open();
        ConfigServices.Config.DeveloperMode = true;

        var devTools = PressF12AndGetDevTools(host);
        Assert.True(devTools.IsVisible);
        Assert.Same(host.Window, devTools.Target);
        Assert.Same(host.Window, devTools.SelectedVisual);
        var root = Assert.IsType<TreeViewItem>(Assert.Single(devTools.Tree.Items));
        Assert.Same(host.Window, root.Tag);
        Assert.True(root.IsExpanded);
        Assert.Contains(root.Items.OfType<TreeViewItem>(), _ => true);

        host.PressKey(Key.F12);
        Assert.Same(devTools, DeveloperModeService.Current);
    }

    [AvaloniaFact]
    public void TurningOffDeveloperMode_ClosesDevTools()
    {
        using var host = MainWindowHost.Open();
        SettingsViewModel.Instance.DeveloperMode = true;
        var devTools = PressF12AndGetDevTools(host);

        SettingsViewModel.Instance.DeveloperMode = false;
        MainWindowHost.Pump();

        Assert.False(devTools.IsVisible);
        Assert.Null(DeveloperModeService.Current);
        host.PressKey(Key.F12);
        Assert.Null(DeveloperModeService.Current);
    }

    [AvaloniaFact]
    public void SelectingControl_ShowsProperties_AndHighlightsIt()
    {
        using var host = MainWindowHost.Open();
        ConfigServices.Config.DeveloperMode = true;
        var devTools = PressF12AndGetDevTools(host);
        var topBox = host.Find<TextBox>("TopBox");

        Assert.True(devTools.SelectVisual(topBox));
        MainWindowHost.Pump();

        Assert.Same(topBox, devTools.SelectedVisual);
        Assert.Same(topBox, (devTools.Tree.SelectedItem as TreeViewItem)?.Tag);
        string Value(string name) => devTools.VisibleProperties.First(p => p.Name == name).Value;
        Assert.Equal(typeof(TextBox).FullName, Value("Type"));
        Assert.Equal("TopBox", Value("Name"));
        Assert.Equal(topBox.Bounds.ToString(), Value("Bounds"));
        Assert.Equal(topBox.Margin.ToString(), Value("Margin"));
        Assert.Contains(devTools.VisibleProperties, p => p.Name == "AutomationId");
        Assert.Contains(devTools.VisibleProperties, p => p.Name == "Text");
        // 全部屬性區段同名屬性只列一次（AddOwner 的屬性不重複）
        var avaloniaProperties = devTools.VisibleProperties.SkipWhile(p => !p.IsHeader).Skip(1).Select(p => p.Name).ToList();
        Assert.Equal(avaloniaProperties.Count, avaloniaProperties.Distinct().Count());

        // 在 App 上標出：AdornerLayer 上有一個標示框，貼著選取的控制項
        var highlight = devTools.Highlight ?? throw new Xunit.Sdk.XunitException("沒有標示框");
        Assert.Same(topBox, AdornerLayer.GetAdornedElement(highlight));
        Assert.IsType<AdornerLayer>(highlight.Parent);
        Assert.False(highlight.IsHitTestVisible);

        // 篩選屬性
        devTools.FilterBox.Text = "margin";
        MainWindowHost.Pump();
        Assert.NotEmpty(devTools.VisibleProperties);
        Assert.All(devTools.VisibleProperties, p => Assert.Contains("margin", p.Name, StringComparison.OrdinalIgnoreCase));

        // 關閉開發者工具時標示框一併移除
        devTools.Close();
        MainWindowHost.Pump();
        Assert.Null(highlight.Parent);
    }

    [AvaloniaFact]
    public void CtrlShiftClick_InApp_SelectsControlUnderPointer()
    {
        using var host = MainWindowHost.Open();
        ConfigServices.Config.DeveloperMode = true;
        var devTools = PressF12AndGetDevTools(host);
        var button = host.Find<Button>("PasteDataButton");
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), host.Window)!.Value;
        var clicked = false;
        button.Click += (_, _) => clicked = true;

        host.Window.MouseDown(center, MouseButton.Left, RawInputModifiers.Control | RawInputModifiers.Shift);
        host.Window.MouseUp(center, MouseButton.Left, RawInputModifiers.Control | RawInputModifiers.Shift);
        MainWindowHost.Pump();

        var selected = devTools.SelectedVisual as Visual ?? throw new Xunit.Sdk.XunitException("沒有選到控制項");
        Assert.True(selected == button || button.IsVisualAncestorOf(selected), $"選到的是 {DevToolsWindow.Describe(selected)}");
        Assert.False(clicked);
    }

    [AvaloniaFact]
    public void DevToolsWindow_Screenshot()
    {
        using var host = MainWindowHost.Open();
        ConfigServices.Config.DeveloperMode = true;
        var devTools = PressF12AndGetDevTools(host);
        devTools.SelectVisual(host.Find<TextBox>("TopBox"));
        MainWindowHost.Pump();

        // 只存畫面給人看（不比對基準圖：視覺樹內容會隨 UI 調整變動）
        Assert.True(File.Exists(VisualAssert.Capture(devTools, "devtools-window-dark")));
        Assert.True(File.Exists(VisualAssert.Capture(host.Window, "devtools-highlight-dark")));
    }
}
