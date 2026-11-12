using System.Runtime.InteropServices;

namespace ExcelPasteTool.Tests;

public class UpdateServiceTests
{
    [Theory]
    [InlineData("1.6.0", "1.6.0.0")]
    [InlineData("v1.6.0", "1.6.0.0")]
    [InlineData("V1.6.0", "1.6.0.0")]
    [InlineData("1.6.0-beta", "1.6.0.0")]
    [InlineData("1.6.0+abc123", "1.6.0.0")]
    [InlineData("1.6", "1.6.0.0")]
    [InlineData("2", "2.0.0.0")]
    [InlineData(" 1.6.0 ", "1.6.0.0")]
    public void TryParseVersion_AcceptsTagFormats(string text, string expected)
    {
        Assert.True(UpdateService.TryParseVersion(text, out var version));
        Assert.Equal(Version.Parse(expected), version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("latest")]
    public void TryParseVersion_RejectsNonVersions(string? text)
    {
        Assert.False(UpdateService.TryParseVersion(text, out _));
    }

    [Fact]
    public void TryParseVersion_NewerTagComparesGreater()
    {
        UpdateService.TryParseVersion("v1.6.0", out var newer);
        UpdateService.TryParseVersion("1.5.5", out var older);
        UpdateService.TryParseVersion("1.10.0", out var twoDigitMinor);

        Assert.True(newer > older);
        Assert.True(twoDigitMinor > newer); // 不能用字串比較，否則 1.10 會小於 1.6
    }

    private static UpdateService.GitHubAsset Asset(string name) =>
        new() { Name = name, BrowserDownloadUrl = "https://example.invalid/" + name };

    private static string CurrentOsToken =>
        OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";

    private static string CurrentArchToken => RuntimeInformation.OSArchitecture switch
    {
        Architecture.Arm64 => "arm64",
        Architecture.X86 => "x86",
        _ => "x64",
    };

    [Fact]
    public void SelectAsset_PicksTheZipForThisPlatformAndArchitecture()
    {
        // 涵蓋三個平台 × 兩種架構，不論測試在哪台機器上跑都要選到自己的那一個
        var assets = new[] { "win", "linux", "osx" }
            .SelectMany(os => new[] { "x64", "arm64" }.Select(arch => Asset($"DataSplitter_{os}_{arch}.zip")))
            .ToList();

        var selected = UpdateService.SelectAsset(assets);

        Assert.NotNull(selected);
        Assert.Equal($"DataSplitter_{CurrentOsToken}_{CurrentArchToken}.zip", selected!.Name);
    }

    [Fact]
    public void SelectAsset_IgnoresNonZipFilesAndOtherPlatforms()
    {
        var otherOs = CurrentOsToken == "linux" ? "win" : "linux";
        var assets = new[]
        {
            Asset($"DataSplitter_{CurrentOsToken}_{CurrentArchToken}.txt"),
            Asset($"DataSplitter_{otherOs}_{CurrentArchToken}.zip"),
        };

        Assert.Null(UpdateService.SelectAsset(assets));
    }

    [Fact]
    public void SelectAsset_NoAssets_ReturnsNull()
    {
        Assert.Null(UpdateService.SelectAsset(null));
        Assert.Null(UpdateService.SelectAsset(Array.Empty<UpdateService.GitHubAsset>()));
    }
}
