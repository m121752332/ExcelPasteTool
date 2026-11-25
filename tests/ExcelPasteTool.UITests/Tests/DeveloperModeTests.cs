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

    private static void Click(MainWindowHost host, string buttonName)
    {
        host.Find<Button>(buttonName).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        MainWindowHost.Pump();
    }

    /// <summary>開到 設定 › 一般設置，並展開「開發者選項」群組。</summary>
    private static void OpenDeveloperGroup(MainWindowHost host)
    {
        host.Window.SidebarItemClickCommand.Execute(SidebarPage.Settings);
        MainWindowHost.Pump();
        Click(host, "GeneralCategoryButton");
        Click(host, "DeveloperGroupExpandButton");
    }

    [AvaloniaFact]
    public void DeveloperGroup_CollapsedByDefault_ExpandsLikeWin11()
    {
        using var host = MainWindowHost.Open();
        host.Window.SidebarItemClickCommand.Execute(SidebarPage.Settings);
        MainWindowHost.Pump();
        Click(host, "GeneralCategoryButton");

        var body = host.Find<Border>("DeveloperGroupBody");
        Assert.False(body.IsVisible);
        Assert.Equal("開發者模式 關閉", host.Find<TextBlock>("DeveloperGroupStatusText").Text);

        Click(host, "DeveloperGroupExpandButton");
        Assert.True(body.IsVisible);
        Assert.True(host.Find<ToggleSwitch>("DeveloperModeToggle").IsEffectivelyVisible);
        Assert.True(host.Find<ComboBox>("DevToolsFontSizeComboBox").IsEffectivelyVisible);
        host.Find<ScrollViewer>("SettingsScroll").ScrollToEnd();
        MainWindowHost.Pump();
        VisualAssert.MatchesBaseline(host.Window, "settings-developer-group-dark");

        Click(host, "DeveloperGroupExpandButton");
        Assert.False(body.IsVisible);
    }

    [AvaloniaFact]
    public void Toggle_DefaultsOff_AndSavesToConfig()
    {
        Assert.False(new AppConfig().DeveloperMode);

        using var host = MainWindowHost.Open();
        OpenDeveloperGroup(host);

        var toggle = host.Find<ToggleSwitch>("DeveloperModeToggle");
        Assert.True(toggle.IsEffectivelyVisible);
        Assert.False(toggle.IsChecked);

        toggle.IsChecked = true;
        MainWindowHost.Pump();
        Assert.True(ConfigServices.Config.DeveloperMode);
        Assert.True(SettingsViewModel.Instance.DeveloperMode);
        Assert.Equal("開發者模式 開啟", host.Find<TextBlock>("DeveloperGroupStatusText").Text);

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
    public void PickMode_HoverHighlights_ClickSelects_LikeEdge()
    {
        using var host = MainWindowHost.Open();
        ConfigServices.Config.DeveloperMode = true;
        var devTools = PressF12AndGetDevTools(host);
        var button = host.Find<Button>("PasteDataButton");
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), host.Window)!.Value;
        var clicked = false;
        button.Click += (_, _) => clicked = true;
        bool IsButtonOrInside(Visual? v) => v != null && (v == button || button.IsVisualAncestorOf(v));

        // Ctrl+Shift+C（macOS ⌘+Shift+C）在 App 上切換選取模式
        var modifier = (OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control) | RawInputModifiers.Shift;
        host.PressKey(Key.C, modifier);
        Assert.True(devTools.IsPicking);

        // 滑鼠移過去：即時標示，但還沒選定
        host.Window.MouseMove(center);
        MainWindowHost.Pump();
        Assert.True(IsButtonOrInside(devTools.HoveredVisual));
        Assert.True(IsButtonOrInside(AdornerLayer.GetAdornedElement(devTools.Highlight!)));
        Assert.Same(host.Window, devTools.SelectedVisual);

        // 點一下（不用按修飾鍵）：選定、結束選取模式、不觸發按鈕
        host.Window.MouseDown(center, MouseButton.Left);
        host.Window.MouseUp(center, MouseButton.Left);
        MainWindowHost.Pump();
        Assert.False(devTools.IsPicking);
        Assert.True(IsButtonOrInside(devTools.SelectedVisual));
        Assert.False(clicked);

        // 再進入選取模式後按 Esc 取消，選取不變
        var selected = devTools.SelectedVisual;
        host.PressKey(Key.C, modifier);
        Assert.True(devTools.IsPicking);
        host.PressKey(Key.Escape);
        Assert.False(devTools.IsPicking);
        Assert.Same(selected, devTools.SelectedVisual);
        Assert.Same(selected, AdornerLayer.GetAdornedElement(devTools.Highlight!));
    }

    [AvaloniaFact]
    public void DevToolsFontSize_IsSeparateFromAppFont_AndAppliesImmediately()
    {
        Assert.Equal("Medium", new AppConfig().DevToolsFontSize);
        Assert.Equal(8, DeveloperModeService.GetPointSize(DevToolsFontSize.Small));
        Assert.Equal(12, DeveloperModeService.GetPointSize(DevToolsFontSize.Medium));
        Assert.Equal(16, DeveloperModeService.GetPointSize(DevToolsFontSize.Large));

        using var host = MainWindowHost.Open();
        OpenDeveloperGroup(host);
        var combo = host.Find<ComboBox>("DevToolsFontSizeComboBox");

        // 開發者模式關閉時不能調
        Assert.False(combo.IsEnabled);
        host.Find<ToggleSwitch>("DeveloperModeToggle").IsChecked = true;
        MainWindowHost.Pump();
        Assert.True(combo.IsEnabled);
        Assert.Equal(3, combo.ItemCount);
        Assert.Equal("中（12 pt）", combo.SelectedItem);

        combo.SelectedItem = "小（8 pt）";
        MainWindowHost.Pump();
        Assert.Equal("Small", ConfigServices.Config.DevToolsFontSize);

        var devTools = PressF12AndGetDevTools(host);
        devTools.SelectVisual(host.Find<ComboBox>("DevToolsFontSizeComboBox"));
        MainWindowHost.Pump();
        Assert.Equal(8, devTools.AppliedFontSize);
        Assert.All(devTools.GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible && t is SelectableTextBlock),
            t => Assert.Equal(8, t.FontSize));
        // App 本身的字體大小不受影響
        Assert.Equal(14, FontManager.CurrentFontSize);
        Assert.Equal(14, host.Find<ToggleSwitch>("DeveloperModeToggle").FontSize);

        // 開著的時候改成「大」：立即套用
        combo.SelectedItem = "大（16 pt）";
        MainWindowHost.Pump();
        Assert.Equal(16, devTools.AppliedFontSize);
        Assert.All(devTools.GetVisualDescendants().OfType<SelectableTextBlock>().Where(t => t.IsEffectivelyVisible),
            t => Assert.Equal(16, t.FontSize));
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
