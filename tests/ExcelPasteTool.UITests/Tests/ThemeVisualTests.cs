using ExcelPasteTool.UITests.Infrastructure;

namespace ExcelPasteTool.UITests.Tests;

/// <summary>
/// 視覺回歸：每個主題（以及英文介面）各拍一張工具頁，和 Baselines/ 的基準圖比對。
/// 改了配色或版面，這裡就會失敗並在 TestResults/ui/diff/ 產生差異圖。
/// </summary>
public class ThemeVisualTests
{
    [AvaloniaTheory]
    [InlineData(AppTheme.Dark)]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.Tiffany)]
    [InlineData(AppTheme.Rainbow)]
    public void ToolsPage_EachTheme_MatchesBaseline(AppTheme theme)
    {
        using var host = MainWindowHost.Open(theme);
        VisualAssert.MatchesBaseline(host.Window, $"tools-{theme.ToString().ToLowerInvariant()}");
    }

    [AvaloniaFact]
    public void ToolsPage_English_MatchesBaseline()
    {
        using var host = MainWindowHost.Open(AppTheme.Dark, AppLanguage.English);
        VisualAssert.MatchesBaseline(host.Window, "tools-dark-en");
    }
}
