using System.Text;

namespace ExcelPasteTool;

/// <summary>
/// 依分隔符號拆解與串接資料。
/// 逗號依 CSV 規則處理：欄位可用雙引號包住（含逗號、雙引號以 "" 跳脫）；其他符號維持單純切割。
/// </summary>
public static class SeparatorHelper
{
    public static IEnumerable<string> Split(string text, char separator)
        => separator == ',' ? SplitCsv(text) : text.Split(separator);

    public static string Join(IEnumerable<string> items, char separator)
        => string.Join(separator, separator == ',' ? items.Select(QuoteCsv) : items);

    /// <summary>貼上的一列資料取第一個非空欄位：有 Tab 先用 Tab，其次逗號（CSV）、分號、豎線。</summary>
    public static string GetFirstColumn(string line)
    {
        foreach (var sep in new[] { '\t', ',', ';', '|' })
        {
            if (!line.Contains(sep)) continue;
            var first = Split(line, sep).Select(p => p.Trim()).FirstOrDefault(p => p.Length > 0);
            if (first != null) return first;
        }
        return line.Trim();
    }

    private static IEnumerable<string> SplitCsv(string text)
    {
        var field = new StringBuilder();
        var inQuotes = false;
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else field.Append(c);
            }
            else if (c == '"' && field.ToString().Trim().Length == 0)
            {
                // 只有欄位開頭的引號才算「包住欄位」，中間的引號（如 5" 螺絲）當一般字元
                field.Clear();
                inQuotes = true;
            }
            else if (c == ',')
            {
                yield return field.ToString();
                field.Clear();
            }
            else field.Append(c);
        }
        yield return field.ToString();
    }

    private static string QuoteCsv(string value)
        => value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0 || value != value.Trim()
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
}
