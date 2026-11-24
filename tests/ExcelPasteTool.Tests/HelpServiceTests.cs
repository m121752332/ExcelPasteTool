namespace ExcelPasteTool.Tests;

/// <summary>GitHub 專案頁：哪些網址留在 App 內的 WebView，哪些交給預設瀏覽器。</summary>
public class HelpServiceTests
{
    [Theory]
    [InlineData("https://github.com/m121752332/ExcelPasteTool", true)]
    [InlineData("https://github.com/m121752332/ExcelPasteTool#readme", true)]
    [InlineData("https://github.com/m121752332/ExcelPasteTool/blob/master/README.en.md", true)]
    [InlineData("https://GitHub.com/M121752332/excelpastetool/releases", true)]
    [InlineData("https://github.com/m121752332/ExcelPasteToolX", false)]
    [InlineData("https://github.com/m121752332", false)]
    [InlineData("https://github.com/login", false)]
    [InlineData("http://github.com/m121752332/ExcelPasteTool", false)]
    [InlineData("https://avaloniaui.net/", false)]
    [InlineData("https://github.com.evil.example/m121752332/ExcelPasteTool", false)]
    public void IsGitHubHelpUri_OnlyAllowsThisRepository(string url, bool expected)
    {
        Assert.Equal(expected, HelpService.IsGitHubHelpUri(new Uri(url)));
    }

    [Fact]
    public void GitHubHelpUrl_PointsToThisRepository()
    {
        Assert.True(HelpService.IsGitHubHelpUri(new Uri(HelpService.GitHubHelpUrl)));
    }
}