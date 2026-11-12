using System.Text.Encodings.Web;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Headless;

namespace ExcelPasteTool.UITests.Infrastructure;

/// <summary>
/// 擷取畫面並與基準圖比對。
/// - 實際畫面一律存到 TestResults/ui/actual/&lt;name&gt;.png（AI Agent 可直接打開看）
/// - 比對結果寫成 TestResults/ui/visual/&lt;name&gt;.json
/// - 沒有基準圖時測試失敗（避免沒比對就算過）；確認畫面正確後用 UPDATE_BASELINES=1 重跑建立基準
/// </summary>
public static class VisualAssert
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>只擷取畫面存成 PNG，不做比對；回傳檔案路徑。</summary>
    public static string Capture(TopLevel topLevel, string name)
    {
        using var frame = topLevel.CaptureRenderedFrame()
            ?? throw new InvalidOperationException("擷取不到畫面：請確認 TestAppBuilder 有 UseSkia() 且 UseHeadlessDrawing = false。");
        var path = Path.Combine(UiTestPaths.ActualDir, name + ".png");
        frame.Save(path);
        return path;
    }

    public static VisualCompareResult MatchesBaseline(TopLevel topLevel, string name, VisualCompareOptions? options = null)
    {
        options ??= VisualCompareOptions.Default;
        var actualPath = Capture(topLevel, name);
        var baselinePath = Path.Combine(UiTestPaths.BaselineDir, name + ".png");
        var diffPath = Path.Combine(UiTestPaths.DiffDir, name + ".diff.png");
        if (File.Exists(diffPath)) File.Delete(diffPath);

        VisualCompareResult result;
        if (UiTestPaths.UpdateBaselines)
        {
            Directory.CreateDirectory(UiTestPaths.BaselineDir);
            File.Copy(actualPath, baselinePath, overwrite: true);
            result = new VisualCompareResult
            {
                Name = name, Status = "baseline-updated", Baseline = baselinePath, Actual = actualPath,
                ChannelTolerance = options.ChannelTolerance, MaxDiffRatio = options.MaxDiffRatio,
                Message = "UPDATE_BASELINES=1：已用本次畫面覆寫基準圖"
            };
        }
        else if (!File.Exists(baselinePath))
        {
            result = new VisualCompareResult
            {
                Name = name, Status = "baseline-missing", Baseline = baselinePath, Actual = actualPath,
                ChannelTolerance = options.ChannelTolerance, MaxDiffRatio = options.MaxDiffRatio,
                Message = $"沒有基準圖 {baselinePath}。確認 {actualPath} 畫面正確後，以 UPDATE_BASELINES=1 重跑建立。"
            };
        }
        else
        {
            result = ImageComparer.Compare(name, baselinePath, actualPath, diffPath, options);
        }

        WriteResult(result);
        if (!result.Passed)
            Assert.Fail($"[視覺比對:{result.Status}] {name}：{result.Message}");
        return result;
    }

    private static void WriteResult(VisualCompareResult result)
    {
        var relative = result with
        {
            Baseline = ToRepoRelative(result.Baseline),
            Actual = ToRepoRelative(result.Actual),
            Diff = ToRepoRelative(result.Diff)
        };
        var json = JsonSerializer.Serialize(relative, JsonOptions);
        File.WriteAllText(Path.Combine(UiTestPaths.VisualResultDir, result.Name + ".json"), json);
    }

    private static string? ToRepoRelative(string? path) =>
        path == null ? null : Path.GetRelativePath(UiTestPaths.RepoRoot, path).Replace('\\', '/');
}
