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
    public void MonthFolderName_IsYearMonth()
    {
        Assert.Equal("202611", ScreenshotService.MonthFolderName(new DateTime(2026, 11, 25)));
        Assert.Equal("202601", ScreenshotService.MonthFolderName(new DateTime(2026, 1, 3)));
        Assert.Equal("2026/11", ScreenshotService.FormatMonth("202611"));
    }

    [Theory]
    [InlineData("202611", true)]
    [InlineData("202601", true)]
    [InlineData("202613", false)]
    [InlineData("202600", false)]
    [InlineData("2026-11", false)]
    [InlineData("backup", false)]
    public void IsMonthFolderName_AcceptsOnlyYyyyMm(string name, bool expected)
    {
        Assert.Equal(expected, ScreenshotService.IsMonthFolderName(name));
    }

    private string Png(string? month, string name, DateTime time, int bytes = 10)
    {
        var folder = month == null ? _dir : Path.Combine(_dir, month);
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, name);
        File.WriteAllBytes(path, new byte[bytes]);
        File.SetLastWriteTime(path, time);
        return path;
    }

    [Fact]
    public void ListMonths_NewestMonthFirst_UnsortedLast_SkipsOtherFolders()
    {
        Png("202610", "DataSplitter_20261030_080000.png", new DateTime(2026, 10, 30, 8, 0, 0));
        Png("202611", "DataSplitter_20261124_080000.png", new DateTime(2026, 11, 24, 8, 0, 0), 1024);
        Png("202611", "DataSplitter_20261125_080000.png", new DateTime(2026, 11, 25, 8, 0, 0), 2048);
        Png(null, "DataSplitter_20260901_080000.png", new DateTime(2026, 9, 1, 8, 0, 0));
        Png("backup", "old.png", new DateTime(2026, 1, 1));
        Directory.CreateDirectory(Path.Combine(_dir, "202609")); // 空的月份資料夾不列出

        var months = ScreenshotService.ListMonths(_dir);

        Assert.Equal(new string?[] { "202611", "202610", null }, months.Select(m => m.Key));
        Assert.Equal(2, months[0].Count);
        Assert.Equal(3072, months[0].TotalBytes);
        Assert.Equal("3.0 KB", months[0].TotalSizeText);
        Assert.Equal("DataSplitter_20261125_080000.png", months[0].Files[0].FileName);
        Assert.True(months[2].IsUnsorted);
        Assert.Equal(_dir, months[2].FolderPath);
    }

    [Fact]
    public void DeleteMonth_RemovesPngsAndEmptyFolder()
    {
        Png("202611", "DataSplitter_20261125_080000.png", new DateTime(2026, 11, 25, 8, 0, 0));
        Png("202611", "DataSplitter_20261124_080000.png", new DateTime(2026, 11, 24, 8, 0, 0));
        var month = ScreenshotService.ListMonths(_dir).Single();

        Assert.Equal(2, ScreenshotService.DeleteMonth(month));
        Assert.False(Directory.Exists(Path.Combine(_dir, "202611")));
    }

    [Fact]
    public void DeleteMonth_KeepsFolder_WhenUserPutOtherFilesThere()
    {
        Png("202611", "DataSplitter_20261125_080000.png", new DateTime(2026, 11, 25, 8, 0, 0));
        File.WriteAllText(Path.Combine(_dir, "202611", "notes.txt"), "x");
        var month = ScreenshotService.ListMonths(_dir).Single();

        Assert.Equal(1, ScreenshotService.DeleteMonth(month));
        Assert.True(File.Exists(Path.Combine(_dir, "202611", "notes.txt")));
    }

    [Fact]
    public void DeleteMonth_Unsorted_OnlyDeletesRootPngs()
    {
        var kept = Png("202611", "DataSplitter_20261125_080000.png", new DateTime(2026, 11, 25, 8, 0, 0));
        Png(null, "DataSplitter_20260901_080000.png", new DateTime(2026, 9, 1, 8, 0, 0));
        var unsorted = ScreenshotService.ListMonths(_dir).Single(m => m.IsUnsorted);

        Assert.Equal(1, ScreenshotService.DeleteMonth(unsorted));
        Assert.True(File.Exists(kept));
        Assert.True(Directory.Exists(_dir));
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
