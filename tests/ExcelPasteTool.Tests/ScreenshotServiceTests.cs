namespace ExcelPasteTool.Tests;

/// <summary>截圖服務中不需要畫面的部分：檔名、避免覆蓋、大小顯示、資料夾設定、清單排序與刪除。實際截圖由 UI 測試（ScreenshotTests）負責。</summary>
public class ScreenshotServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "DataSplitter.Tests", "Screenshots-" + Guid.NewGuid().ToString("N"));
    private readonly string _savedFolderSetting = ConfigServices.Config.ScreenshotFolder;

    public ScreenshotServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        ConfigServices.Config.ScreenshotFolder = _savedFolderSetting;
        ScreenshotService.DefaultFolderOverride = null;
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void BuildFileName_UsesPrefixAndTimestamp()
    {
        Assert.Equal("DataSplitter_20261125_093005.png", ScreenshotService.BuildFileName(new DateTime(2026, 11, 25, 9, 30, 5)));
    }

    [Fact]
    public void GetUniquePath_AddsCounter_WhenSameSecondAlreadyExists()
    {
        var time = new DateTime(2026, 11, 25, 9, 30, 5);
        var first = ScreenshotService.GetUniquePath(_dir, time);
        Assert.Equal(Path.Combine(_dir, "DataSplitter_20261125_093005.png"), first);

        File.WriteAllBytes(first, new byte[] { 1 });
        var second = ScreenshotService.GetUniquePath(_dir, time);
        Assert.Equal(Path.Combine(_dir, "DataSplitter_20261125_093005_2.png"), second);

        File.WriteAllBytes(second, new byte[] { 1 });
        Assert.Equal(Path.Combine(_dir, "DataSplitter_20261125_093005_3.png"), ScreenshotService.GetUniquePath(_dir, time));
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(5 * 1024 * 1024 + 300 * 1024, "5.3 MB")]
    public void FormatSize_UsesBytesKbMb(long bytes, string expected)
    {
        Assert.Equal(expected, ScreenshotService.FormatSize(bytes));
    }

    [Fact]
    public void Folder_DefaultsToPicturesDataSplitter_AndFollowsSetting()
    {
        ConfigServices.Config.ScreenshotFolder = "";
        Assert.False(ScreenshotService.IsCustomFolder);
        Assert.Equal("DataSplitter", Path.GetFileName(ScreenshotService.DefaultFolder));
        Assert.Equal(ScreenshotService.DefaultFolder, ScreenshotService.Folder);

        ConfigServices.Config.ScreenshotFolder = _dir;
        Assert.True(ScreenshotService.IsCustomFolder);
        Assert.Equal(_dir, ScreenshotService.Folder);
    }

    [Fact]
    public void List_ReturnsNewestFirst_OnlyPng()
    {
        var older = Path.Combine(_dir, "DataSplitter_20261124_080000.png");
        var newer = Path.Combine(_dir, "DataSplitter_20261125_080000.png");
        File.WriteAllBytes(older, new byte[10]);
        File.WriteAllBytes(newer, new byte[2048]);
        File.WriteAllText(Path.Combine(_dir, "notes.txt"), "x");
        File.SetLastWriteTime(older, new DateTime(2026, 11, 24, 8, 0, 0));
        File.SetLastWriteTime(newer, new DateTime(2026, 11, 25, 8, 0, 0));

        var list = ScreenshotService.List(_dir);

        Assert.Equal(new[] { newer, older }, list.Select(f => f.FilePath));
        Assert.Equal("DataSplitter_20261125_080000.png", list[0].FileName);
        Assert.Equal("2.0 KB", list[0].SizeText);
        Assert.Equal("2026/11/25 08:00:00", list[0].ModifiedText);
    }

    [Fact]
    public void List_MissingFolder_ReturnsEmpty()
    {
        Assert.Empty(ScreenshotService.List(Path.Combine(_dir, "not-here")));
    }

    [Fact]
    public void Delete_RemovesFile_AndRaisesChanged()
    {
        var file = Path.Combine(_dir, "DataSplitter_20261125_080000.png");
        File.WriteAllBytes(file, new byte[10]);
        var raised = 0;
        void OnChanged() => raised++;
        ScreenshotService.Changed += OnChanged;
        try
        {
            Assert.True(ScreenshotService.Delete(file));
        }
        finally
        {
            ScreenshotService.Changed -= OnChanged;
        }
        Assert.False(File.Exists(file));
        Assert.Equal(1, raised);
    }
}
