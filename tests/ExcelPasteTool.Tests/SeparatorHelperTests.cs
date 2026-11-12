namespace ExcelPasteTool.Tests;

public class SeparatorHelperTests
{
    [Theory]
    [InlineData('|')]
    [InlineData(';')]
    [InlineData(':')]
    [InlineData(' ')]
    public void Split_SimpleSeparator_SplitsOnEveryOccurrence(char separator)
    {
        var text = string.Join(separator, "A01", "B01", "C01");
        Assert.Equal(new[] { "A01", "B01", "C01" }, SeparatorHelper.Split(text, separator));
    }

    [Fact]
    public void Split_Comma_TreatsQuotedFieldAsOneValue()
    {
        Assert.Equal(new[] { "A01", "B,01", "C01" }, SeparatorHelper.Split("A01,\"B,01\",C01", ','));
    }

    [Fact]
    public void Split_Comma_UnescapesDoubledQuotes()
    {
        Assert.Equal(new[] { "say \"hi\"", "B01" }, SeparatorHelper.Split("\"say \"\"hi\"\"\",B01", ','));
    }

    [Theory]
    [InlineData('|')]
    [InlineData(',')]
    [InlineData(';')]
    public void JoinThenSplit_ReturnsOriginalItems(char separator)
    {
        var items = new[] { "A01", "B01", "C 01" };
        var joined = SeparatorHelper.Join(items, separator);
        Assert.Equal(items, SeparatorHelper.Split(joined, separator));
    }

    [Fact]
    public void JoinThenSplit_Comma_KeepsValuesContainingCommaAndQuote()
    {
        var items = new[] { "A,01", "5\" pipe", "plain" };
        var joined = SeparatorHelper.Join(items, ',');
        Assert.Equal(items, SeparatorHelper.Split(joined, ','));
    }

    [Theory]
    [InlineData("A01\tB01\tC01", "A01")]   // Excel 多欄複製：Tab 分隔，只取第一欄
    [InlineData("A01,B01", "A01")]
    [InlineData("A01;B01", "A01")]
    [InlineData("A01|B01", "A01")]
    [InlineData("\tB01", "B01")]            // 第一欄是空的就取下一個非空欄位
    [InlineData("  A01  ", "A01")]
    [InlineData("A01", "A01")]
    public void GetFirstColumn_ReturnsFirstNonEmptyColumn(string line, string expected)
    {
        Assert.Equal(expected, SeparatorHelper.GetFirstColumn(line));
    }

    [Fact]
    public void GetFirstColumn_TabWinsOverComma()
    {
        // 有 Tab 時先用 Tab 切，欄位內的逗號要保留
        Assert.Equal("A,01", SeparatorHelper.GetFirstColumn("A,01\tB01"));
    }
}
