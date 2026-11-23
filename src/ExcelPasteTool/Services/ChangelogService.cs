using System.Text.Json;
using Avalonia.Platform;

namespace ExcelPasteTool;

/// <summary>一個版本的改版資訊。</summary>
public sealed class ChangelogEntry
{
    public string Version { get; set; } = "";
    public string Date { get; set; } = "";
    /// <summary>key 為 AppLanguage 名稱（例如 English），value 為該語言的條列內容。</summary>
    public Dictionary<string, List<string>> Notes { get; set; } = new();
}

/// <summary>
/// 讀取內嵌資源 Resources/Changelog.json（由新到舊）。
/// 新增版本時只要在該檔最前面加一筆；某語言沒有翻譯時依序退回英文、繁體中文。
/// </summary>
public static class ChangelogService
{
    private static List<ChangelogEntry>? _cache;

    public static IReadOnlyList<ChangelogEntry> Load()
    {
        if (_cache != null) return _cache;
        try
        {
            using var stream = AssetLoader.Open(new Uri($"avares://{Global.AssemblyName}/Resources/Changelog.json"));
            // Changelog.json 的欄位是小寫（version / date / notes），需不分大小寫對應到屬性
            _cache = JsonSerializer.Deserialize<List<ChangelogEntry>>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch
        {
            _cache = new();
        }
        return _cache;
    }

    public static IReadOnlyList<string> GetNotes(ChangelogEntry entry, AppLanguage language)
    {
        var order = language switch
        {
            AppLanguage.TraditionalChinese => new[] { AppLanguage.TraditionalChinese, AppLanguage.English },
            AppLanguage.SimplifiedChinese => new[] { AppLanguage.SimplifiedChinese, AppLanguage.TraditionalChinese, AppLanguage.English },
            _ => new[] { language, AppLanguage.English, AppLanguage.TraditionalChinese }
        };
        foreach (var lang in order)
            if (entry.Notes.TryGetValue(lang.ToString(), out var notes) && notes.Count > 0)
                return notes;
        return Array.Empty<string>();
    }
}
