using System.Text.Json;
using System.Xml.Linq;

namespace ExcelPasteTool.Tests;

/// <summary>
/// 每次發版都要手動改的檔案：Changelog.json 與 csproj 的版本號。
/// 這裡直接讀倉庫裡的原始檔（建置時複製到 RepoFiles/）。
/// </summary>
public class ReleaseFilesTests
{
    private static readonly string RepoFiles = Path.Combine(AppContext.BaseDirectory, "RepoFiles");

    private static List<ChangelogEntry> LoadChangelog()
    {
        var json = File.ReadAllText(Path.Combine(RepoFiles, "Changelog.json"));
        // 與 ChangelogService 相同的讀法：欄位名稱不分大小寫
        return JsonSerializer.Deserialize<List<ChangelogEntry>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private static string ProjectVersion() =>
        XDocument.Load(Path.Combine(RepoFiles, "ExcelPasteTool.csproj"))
            .Descendants("Version").Single().Value.Trim();

    [Fact]
    public void Changelog_EveryEntryHasVersionDateAndNotes()
    {
        var entries = LoadChangelog();
        Assert.NotEmpty(entries);
        foreach (var entry in entries)
        {
            Assert.True(UpdateService.TryParseVersion(entry.Version, out _), $"版本號無法解析: '{entry.Version}'");
            Assert.True(DateOnly.TryParseExact(entry.Date, "yyyy-MM-dd", out _), $"v{entry.Version} 的日期格式不是 yyyy-MM-dd: '{entry.Date}'");
            // 每個語言最後都會退回繁體中文或英文，所以至少要有其中一種
            Assert.NotEmpty(ChangelogService.GetNotes(entry, AppLanguage.TraditionalChinese));
        }
    }

    [Fact]
    public void Changelog_VersionsAreNewestFirstWithoutRepeats()
    {
        var versions = LoadChangelog()
            .Select(e => { UpdateService.TryParseVersion(e.Version, out var v); return v; })
            .ToList();
        Assert.Equal(versions.OrderByDescending(v => v).Distinct(), versions);
    }

    [Fact]
    public void Changelog_LatestEntryMatchesProjectVersion()
    {
        Assert.Equal(ProjectVersion(), LoadChangelog()[0].Version);
    }

    [Fact]
    public void Changelog_LatestEntryIsTranslatedIntoEveryLanguage()
    {
        var latest = LoadChangelog()[0];
        foreach (var language in Enum.GetValues<AppLanguage>())
        {
            Assert.True(latest.Notes.TryGetValue(language.ToString(), out var notes) && notes.Count > 0,
                $"v{latest.Version} 缺少 {language} 的改版說明");
            Assert.All(notes!, n => Assert.False(string.IsNullOrWhiteSpace(n)));
        }
    }

    [Fact]
    public void Project_AllVersionFieldsAgree()
    {
        var doc = XDocument.Load(Path.Combine(RepoFiles, "ExcelPasteTool.csproj"));
        string Field(string name) => doc.Descendants(name).Single().Value.Trim();

        var version = Field("Version");
        Assert.Equal(version, Field("InformationalVersion"));
        Assert.Equal(version + ".0", Field("AssemblyVersion"));
        Assert.Equal(version + ".0", Field("FileVersion"));
    }
}
