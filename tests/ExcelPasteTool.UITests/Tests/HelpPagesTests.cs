using ExcelPasteTool.UITests.Infrastructure;

namespace ExcelPasteTool.UITests.Tests;

/// <summary>側邊欄的「說明」與「GitHub」是兩個獨立的項目，各自對應一個說明頁。</summary>
public class HelpPagesTests
{
    [AvaloniaFact]
    public void Sidebar_HasGitHubItem_RightBelowHelp()
    {
        using var host = MainWindowHost.Open();
        var pages = host.Window.SidebarItems.Select(i => i.Page).ToList();

        Assert.Equal(pages.IndexOf(SidebarPage.Help) + 1, pages.IndexOf(SidebarPage.GitHub));
        var gitHub = host.Window.SidebarItems.Single(i => i.Page == SidebarPage.GitHub);
        // 名稱固定為 GitHub，不做多語系；圖示是 GitHub 的貓
        Assert.Equal("GitHub", gitHub.Name);
        Assert.Equal(IconManager.GitHubIcon, gitHub.Icon);
    }

    [AvaloniaFact]
    public void HelpAndGitHubPages_AreSeparateViews()
    {
        SidebarPageToContentConverter.ResetCache();
        var converter = new SidebarPageToContentConverter();

        var help = Assert.IsType<HelpView>(converter.Convert(SidebarPage.Help, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture));
        var gitHub = Assert.IsType<HelpView>(converter.Convert(SidebarPage.GitHub, typeof(object), null, System.Globalization.CultureInfo.InvariantCulture));

        Assert.NotSame(help, gitHub);
        Assert.Equal(HelpSource.Local, help.Source);
        Assert.Equal(HelpSource.GitHub, gitHub.Source);
        SidebarPageToContentConverter.ResetCache();
    }
}