using System.Text.RegularExpressions;

namespace ExcelPasteTool.Tests;

/// <summary>
/// 五種語言的字串寫在 LanguageManager 裡，新增功能時很容易漏掉某個語言。
/// 以繁體中文為基準，檢查其他語言的 key 與格式參數是否一致。
/// </summary>
public class LanguageTextsTests
{
    private static readonly Dictionary<string, string> Reference =
        LanguageManager.GetTexts(AppLanguage.TraditionalChinese);

    public static IEnumerable<object[]> OtherLanguages =>
        Enum.GetValues<AppLanguage>()
            .Where(l => l != AppLanguage.TraditionalChinese)
            .Select(l => new object[] { l });

    [Theory]
    [MemberData(nameof(OtherLanguages))]
    public void Language_HasSameKeysAsTraditionalChinese(AppLanguage language)
    {
        var texts = LanguageManager.GetTexts(language);
        var missing = Reference.Keys.Except(texts.Keys).OrderBy(k => k).ToList();
        var extra = texts.Keys.Except(Reference.Keys).OrderBy(k => k).ToList();

        Assert.True(missing.Count == 0 && extra.Count == 0,
            $"{language} 缺少: [{string.Join(", ", missing)}]；多出: [{string.Join(", ", extra)}]");
    }

    [Theory]
    [MemberData(nameof(OtherLanguages))]
    public void Language_UsesSameFormatPlaceholders(AppLanguage language)
    {
        var texts = LanguageManager.GetTexts(language);
        var mismatched = Reference
            .Where(kv => texts.TryGetValue(kv.Key, out var other) && Placeholders(kv.Value) != Placeholders(other))
            .Select(kv => kv.Key)
            .OrderBy(k => k)
            .ToList();

        Assert.True(mismatched.Count == 0, $"{language} 的 {{0}} 參數與繁體中文不一致: [{string.Join(", ", mismatched)}]");
    }

    [Fact]
    public void EveryLanguage_HasNoEmptyText()
    {
        foreach (var language in Enum.GetValues<AppLanguage>())
        {
            var empty = LanguageManager.GetTexts(language)
                .Where(kv => string.IsNullOrWhiteSpace(kv.Value))
                .Select(kv => kv.Key)
                .ToList();
            Assert.True(empty.Count == 0, $"{language} 有空白字串: [{string.Join(", ", empty)}]");
        }
    }

    // 例如 "共 {0} 筆，保留 {1} 筆" → "0,1"
    private static string Placeholders(string text) =>
        string.Join(",", Regex.Matches(text, @"\{(\d+)[^}]*\}").Select(m => m.Groups[1].Value).Distinct().OrderBy(x => x));
}
