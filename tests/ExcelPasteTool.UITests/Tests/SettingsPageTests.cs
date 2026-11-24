using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using ExcelPasteTool.UITests.Infrastructure;

namespace ExcelPasteTool.UITests.Tests;

/// <summary>
/// 設定頁三層式導覽（設定 › 分類 › 參數）：
/// - 視覺回歸：設定首頁 × 四個主題 + 英文，三個分類頁，以及字體大小展開狀態
/// - 互動：點分類進入、麵包屑與 Alt+← 返回、離開再回來記住分類、切換主題立即換色
/// - 更新操作已從「關於」頁移到「更新設置」
/// </summary>
public class SettingsPageTests
{
    private static MainWindowHost OpenSettings(AppTheme theme = AppTheme.Dark, AppLanguage language = AppLanguage.TraditionalChinese)
    {
        var host = MainWindowHost.Open(theme, language);
        host.Window.SidebarItemClickCommand.Execute(SidebarPage.Settings);
        MainWindowHost.Pump();
        return host;
    }

    private static void Click(MainWindowHost host, string buttonName)
    {
        host.Find<Button>(buttonName).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        MainWindowHost.Pump();
    }

    private static bool IsShown(MainWindowHost host, string panelName) => host.Find<StackPanel>(panelName).IsVisible;

    [AvaloniaTheory]
    [InlineData(AppTheme.Dark)]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Tiffany)]
    [InlineData(AppTheme.Rainbow)]
    public void SettingsHome_EachTheme_MatchesBaseline(AppTheme theme)
    {
        using var host = OpenSettings(theme);
        Assert.True(IsShown(host, "HomePanel"));
        VisualAssert.MatchesBaseline(host.Window, $"settings-home-{theme.ToString().ToLowerInvariant()}");
    }

    [AvaloniaFact]
    public void SettingsHome_English_MatchesBaseline()
    {
        using var host = OpenSettings(AppTheme.Dark, AppLanguage.English);
        VisualAssert.MatchesBaseline(host.Window, "settings-home-dark-en");
    }

    [AvaloniaTheory]
    [InlineData("GeneralCategoryButton", "GeneralPanel", "settings-general-dark")]
    [InlineData("PreferencesCategoryButton", "PreferencesPanel", "settings-preferences-dark")]
    [InlineData("UpdateCategoryButton", "UpdatePanel", "settings-update-dark")]
    public void CategoryPage_MatchesBaseline(string categoryButton, string panel, string baseline)
    {
        using var host = OpenSettings();
        Click(host, categoryButton);

        Assert.True(IsShown(host, panel));
        Assert.False(IsShown(host, "HomePanel"));
        VisualAssert.MatchesBaseline(host.Window, baseline);
    }

    [AvaloniaFact]
    public void FontSizeRow_ExpandsToShowSliderPreviewAndReset()
    {
        using var host = OpenSettings();
        Click(host, "PreferencesCategoryButton");
        Assert.False(host.Find<Border>("FontSizeDetail").IsVisible);

        Click(host, "FontSizeExpandButton");
        Assert.True(host.Find<Border>("FontSizeDetail").IsVisible);
        Assert.True(host.Find<Slider>("FontSizeSlider").IsVisible);
        Assert.True(host.Find<Button>("ResetFontButton").IsVisible);
        VisualAssert.MatchesBaseline(host.Window, "settings-preferences-expanded-dark");

        Click(host, "FontSizeExpandButton");
        Assert.False(host.Find<Border>("FontSizeDetail").IsVisible);
    }

    [AvaloniaFact]
    public void Breadcrumb_ShowsCategory_AndReturnsHome()
    {
        using var host = OpenSettings();
        Assert.True(host.Find<TextBlock>("SettingsHomeTitle").IsVisible);
        Assert.False(host.Find<Button>("BreadcrumbRootButton").IsVisible);

        Click(host, "GeneralCategoryButton");
        Assert.True(host.Find<Button>("BreadcrumbRootButton").IsVisible);
        Assert.Equal("一般設置", host.Find<TextBlock>("BreadcrumbCurrentText").Text);

        Click(host, "BreadcrumbRootButton");
        Assert.True(IsShown(host, "HomePanel"));
        Assert.False(IsShown(host, "GeneralPanel"));
    }

    [AvaloniaFact]
    public void AltLeft_ReturnsToSettingsHome()
    {
        using var host = OpenSettings();
        Click(host, "UpdateCategoryButton");
        host.Find<Button>("ChangelogButton").Focus();
        MainWindowHost.Pump();

        host.PressKey(Key.Left, RawInputModifiers.Alt);

        Assert.True(IsShown(host, "HomePanel"));
        Assert.False(IsShown(host, "UpdatePanel"));
    }

    [AvaloniaFact]
    public void LeavingAndReturning_RemembersCategory()
    {
        using var host = OpenSettings();
        Click(host, "UpdateCategoryButton");

        host.Window.SidebarItemClickCommand.Execute(SidebarPage.Tools);
        MainWindowHost.Pump();
        host.Window.SidebarItemClickCommand.Execute(SidebarPage.Settings);
        MainWindowHost.Pump();

        Assert.True(IsShown(host, "UpdatePanel"));
    }

    [AvaloniaFact]
    public void CategorySummaries_ShowCurrentValues()
    {
        using var host = OpenSettings();
        Assert.Contains("繁體中文", host.Find<TextBlock>("GeneralSummaryText").Text);
        Assert.Contains("14 pt", host.Find<TextBlock>("PreferencesSummaryText").Text);
        Assert.Contains("v1.0.0", host.Find<TextBlock>("UpdateSummaryText").Text);
    }

    [AvaloniaFact]
    public void ChangingTheme_InPreferences_RecolorsImmediately()
    {
        using var host = OpenSettings();
        Click(host, "PreferencesCategoryButton");
        var row = host.Find<ComboBox>("ThemeComboBox").GetVisualAncestors().OfType<Border>()
            .First(b => b.Classes.Contains("setting-row"));
        var before = ((ISolidColorBrush)row.Background!).Color;

        host.Find<ComboBox>("ThemeComboBox").SelectedItem = ThemeManager.GetThemeName(AppTheme.Light);
        MainWindowHost.Pump();
        MainWindowHost.Pump();

        Assert.Equal(AppTheme.Light, ThemeManager.CurrentTheme);
        Assert.NotEqual(before, ((ISolidColorBrush)row.Background!).Color);
        Assert.True(IsShown(host, "PreferencesPanel"));
    }

    [AvaloniaFact]
    public void UpdateActions_MovedFromAboutPage()
    {
        using var host = OpenSettings();
        Click(host, "UpdateCategoryButton");
        Assert.True(host.Find<Button>("CheckUpdateButton").IsVisible);
        Assert.True(host.Find<Button>("ChangelogButton").IsVisible);
        Assert.Equal("v1.0.0", host.Find<TextBlock>("CurrentVersionText").Text);

        host.Window.SidebarItemClickCommand.Execute(SidebarPage.About);
        MainWindowHost.Pump();
        var about = (Control)host.Find<ContentControl>("MainContent").Content!;
        Assert.IsType<AboutView>(about);
        Assert.DoesNotContain(about.GetVisualDescendants().OfType<Button>(),
            b => b.Name is "CheckUpdateButton" or "ChangelogButton");
    }

    [AvaloniaFact]
    public void AboutVersionButton_JumpsToUpdateSettings()
    {
        using var host = MainWindowHost.Open();
        host.Window.SidebarItemClickCommand.Execute(SidebarPage.About);
        MainWindowHost.Pump();

        Click(host, "VersionButton");

        Assert.Equal(SidebarPage.Settings, host.Window.Sidebar.SelectedPage);
        Assert.IsType<SettingsView>(host.Find<ContentControl>("MainContent").Content);
        Assert.True(IsShown(host, "UpdatePanel"));
        Assert.Equal("更新設置", host.Find<TextBlock>("BreadcrumbCurrentText").Text);
    }
}
