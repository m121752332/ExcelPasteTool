namespace ExcelPasteTool;

/// <summary>一個重複值：資料內容與它在貼上內容中出現的總次數（保留 1 筆，其餘被移除）。</summary>
public sealed record DuplicateEntry(string Value, int Count)
{
    public int Removed => Count - 1;
}

/// <summary>
/// 工具頁的資料運算（不依賴畫面，方便單元測試）：解析貼上的文字、統計重複值、組 SQL 條件。
/// </summary>
public static class PasteDataHelper
{
    /// <summary>把剪貼簿文字拆成一筆一筆的值：每列取第一個非空欄位，略過空白列。不去重複。</summary>
    public static List<string> ParsePastedText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();
        return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Select(SeparatorHelper.GetFirstColumn)
            .Where(v => v.Length > 0)
            .ToList();
    }

    /// <summary>
    /// 找出出現兩次以上的值。次數多的排前面；次數相同時維持第一次出現的先後順序
    /// （GroupBy 依首次出現排序，OrderByDescending 是穩定排序）。比對方式與資料表去重複一致：文字完全相同。
    /// </summary>
    public static List<DuplicateEntry> FindDuplicates(IEnumerable<string> values) =>
        values
            .GroupBy(v => v)
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateEntry(g.Key, g.Count()))
            .OrderByDescending(d => d.Count)
            .ToList();

    /// <summary>組成 SQL IN 條件的括號內容，例如 ('A01', 'B01')；單引號以兩個單引號跳脫。沒有資料時回傳空字串。</summary>
    public static string BuildSqlCondition(IEnumerable<string> items)
    {
        var quoted = items.Select(x => $"'{x.Replace("'", "''")}'").ToList();
        return quoted.Count == 0 ? string.Empty : $"({string.Join(", ", quoted)})";
    }
}
