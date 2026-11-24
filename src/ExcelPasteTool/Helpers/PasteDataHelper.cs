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

    /// <summary>Oracle 單一 IN 清單最多 1000 個值（超過會出現 ORA-01795），超過時要拆成多組以 OR 串接。</summary>
    public const int OracleInListLimit = 1000;

    /// <summary>拆成多組 IN 但沒有填欄位名稱時，暫用的欄位名稱（使用者貼上後自行替換）。</summary>
    public const string ColumnPlaceholder = "col";

    // 「數值不加引號」只認單純的十進位數字；有前導零（例如料號 001）的值一律視為文字，避免被當成數字比對後少掉 0
    private static readonly System.Text.RegularExpressions.Regex PlainNumber =
        new(@"^-?(0|[1-9][0-9]*)(\.[0-9]+)?$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>所有值都是單純的數字（無前導零）時回傳 true；用來決定「數值不加引號」是否生效。</summary>
    public static bool AllPlainNumbers(IReadOnlyCollection<string> items) =>
        items.Count > 0 && items.All(x => PlainNumber.IsMatch(x));

    /// <summary>
    /// 組成 SQL IN 條件。沒有資料時回傳空字串。
    /// <list type="bullet">
    /// <item>沒填欄位名稱、筆數不超過 1000：只輸出括號內容，例如 ('A01', 'B01')（舊版行為）。</item>
    /// <item>有填欄位名稱：ima01 IN ('A01', 'B01')。</item>
    /// <item>超過 1000 筆：每 1000 筆一組，(ima01 IN (...) OR ima01 IN (...))；沒填欄位名稱時暫用 col。</item>
    /// <item>UnquoteNumbers：全部都是數字（無前導零）時不加引號；只要有一筆不是就全部加引號。</item>
    /// <item>ItemsPerLine &gt; 0：括號內每 N 個值換一行，方便貼進 4GL / SQL*Plus。</item>
    /// </list>
    /// 單引號一律以兩個單引號跳脫。
    /// </summary>
    public static string BuildSqlCondition(IEnumerable<string> items, SqlFormatOptions? options = null)
    {
        options ??= SqlFormatOptions.Default;
        var list = items.ToList();
        if (list.Count == 0) return string.Empty;

        var unquoted = options.UnquoteNumbers && AllPlainNumbers(list);
        var values = list.Select(x => unquoted ? x : $"'{x.Replace("'", "''")}'").ToList();
        var column = (options.ColumnName ?? string.Empty).Trim();
        var newLine = Environment.NewLine;

        var chunks = values.Chunk(OracleInListLimit).Select(c => "(" + JoinValues(c, options.ItemsPerLine, newLine) + ")").ToList();
        if (chunks.Count == 1)
            return column.Length == 0 ? chunks[0] : $"{column} IN {chunks[0]}";

        if (column.Length == 0) column = ColumnPlaceholder;
        return "(" + string.Join(newLine + " OR ", chunks.Select(c => $"{column} IN {c}")) + ")";
    }

    private static string JoinValues(IReadOnlyList<string> values, int itemsPerLine, string newLine)
    {
        if (itemsPerLine <= 0 || values.Count <= itemsPerLine) return string.Join(", ", values);
        return string.Join("," + newLine + " ", values.Chunk(itemsPerLine).Select(line => string.Join(", ", line)));
    }
}

/// <summary>SQL 條件的輸出格式（工具頁 SQL 條件區塊的「格式選項」，存在設定檔）。</summary>
/// <param name="ColumnName">欄位名稱，例如 ima01；空白時只輸出括號內容。</param>
/// <param name="UnquoteNumbers">全部都是數字時不加單引號。</param>
/// <param name="ItemsPerLine">括號內每行幾個值，0 代表不換行。</param>
public sealed record SqlFormatOptions(string ColumnName = "", bool UnquoteNumbers = false, int ItemsPerLine = 0)
{
    public static SqlFormatOptions Default { get; } = new();

    public static SqlFormatOptions FromConfig(AppConfig config) =>
        new(config.SqlColumnName ?? string.Empty, config.SqlUnquoteNumbers, Math.Clamp(config.SqlItemsPerLine, 0, MaxItemsPerLine));

    /// <summary>每行筆數上限（下拉 / 數字框的最大值）。</summary>
    public const int MaxItemsPerLine = 100;
}
