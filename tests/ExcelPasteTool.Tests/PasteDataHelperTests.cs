namespace ExcelPasteTool.Tests;

public class PasteDataHelperTests
{
    [Fact]
    public void ParsePastedText_OneValuePerLine_KeepsDuplicatesAndOrder()
    {
        var values = PasteDataHelper.ParsePastedText("A01\r\nA01\r\nB01\r\n");
        Assert.Equal(new[] { "A01", "A01", "B01" }, values);
    }

    [Fact]
    public void ParsePastedText_SkipsBlankLinesAndTrims()
    {
        var values = PasteDataHelper.ParsePastedText("  A01  \n\n   \nB01\n");
        Assert.Equal(new[] { "A01", "B01" }, values);
    }

    [Fact]
    public void ParsePastedText_MultipleColumns_TakesFirstColumnOnly()
    {
        var values = PasteDataHelper.ParsePastedText("A01\tx\ty\r\nB01\tz");
        Assert.Equal(new[] { "A01", "B01" }, values);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \r\n ")]
    public void ParsePastedText_Empty_ReturnsEmptyList(string? text)
    {
        Assert.Empty(PasteDataHelper.ParsePastedText(text));
    }

    [Fact]
    public void FindDuplicates_CountsOccurrencesAndRemoved_MostFrequentFirst()
    {
        // 需求範例：A01 貼 2 次、B01 貼 4 次
        var duplicates = PasteDataHelper.FindDuplicates(new[] { "A01", "A01", "B01", "B01", "B01", "B01" });

        Assert.Equal(2, duplicates.Count);
        Assert.Equal(new DuplicateEntry("B01", 4), duplicates[0]);
        Assert.Equal(3, duplicates[0].Removed);
        Assert.Equal(new DuplicateEntry("A01", 2), duplicates[1]);
        Assert.Equal(1, duplicates[1].Removed);
    }

    [Fact]
    public void FindDuplicates_IgnoresValuesThatAppearOnce()
    {
        var duplicates = PasteDataHelper.FindDuplicates(new[] { "A01", "B01", "A01", "C01" });
        Assert.Equal(new[] { new DuplicateEntry("A01", 2) }, duplicates);
    }

    [Fact]
    public void FindDuplicates_SameCount_KeepsFirstSeenOrder()
    {
        var duplicates = PasteDataHelper.FindDuplicates(new[] { "B01", "A01", "A01", "B01" });
        Assert.Equal(new[] { "B01", "A01" }, duplicates.Select(d => d.Value));
    }

    [Fact]
    public void FindDuplicates_IsCaseSensitive_LikeTheTableDedup()
    {
        Assert.Empty(PasteDataHelper.FindDuplicates(new[] { "a01", "A01" }));
    }

    [Fact]
    public void FindDuplicates_NoDuplicates_ReturnsEmptyList()
    {
        Assert.Empty(PasteDataHelper.FindDuplicates(new[] { "A01", "B01" }));
        Assert.Empty(PasteDataHelper.FindDuplicates(Array.Empty<string>()));
    }

    [Fact]
    public void RemovedTotal_PlusDistinctCount_EqualsPastedCount()
    {
        var pasted = new[] { "A01", "A01", "B01", "B01", "B01", "B01", "C01" };
        var removed = PasteDataHelper.FindDuplicates(pasted).Sum(d => d.Removed);
        Assert.Equal(pasted.Length, pasted.Distinct().Count() + removed);
    }

    [Fact]
    public void BuildSqlCondition_QuotesEachItem()
    {
        Assert.Equal("('A01', 'B01')", PasteDataHelper.BuildSqlCondition(new[] { "A01", "B01" }));
    }

    [Fact]
    public void BuildSqlCondition_EscapesSingleQuote()
    {
        Assert.Equal("('O''Brien')", PasteDataHelper.BuildSqlCondition(new[] { "O'Brien" }));
    }

    [Fact]
    public void BuildSqlCondition_NoItems_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, PasteDataHelper.BuildSqlCondition(Array.Empty<string>()));
    }
}
