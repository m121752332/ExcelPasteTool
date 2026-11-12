namespace ExcelPasteTool.UITests.Infrastructure;

/// <summary>
/// UI 測試用到的資料夾。
/// - 基準圖：tests/ExcelPasteTool.UITests/Baselines/（進版控）
/// - 產出物：TestResults/ui/（已在 .gitignore），可用環境變數 UI_TEST_RESULTS_DIR 改位置
/// </summary>
public static class UiTestPaths
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string BaselineDir => Path.Combine(RepoRoot, "tests", "ExcelPasteTool.UITests", "Baselines");

    public static string ResultsDir
    {
        get
        {
            var custom = Environment.GetEnvironmentVariable("UI_TEST_RESULTS_DIR");
            return string.IsNullOrWhiteSpace(custom)
                ? Path.Combine(RepoRoot, "TestResults", "ui")
                : Path.GetFullPath(custom);
        }
    }

    /// <summary>每次擷取的實際畫面。</summary>
    public static string ActualDir => Ensure(Path.Combine(ResultsDir, "actual"));

    /// <summary>比對失敗時的差異圖（不同的像素標紅）。</summary>
    public static string DiffDir => Ensure(Path.Combine(ResultsDir, "diff"));

    /// <summary>每一筆視覺比對的結構化結果（一筆一個 JSON，由 run-ui-tests.ps1 彙整）。</summary>
    public static string VisualResultDir => Ensure(Path.Combine(ResultsDir, "visual"));

    /// <summary>UPDATE_BASELINES=1 時，把這次的畫面直接寫成新的基準圖（確認 UI 改動是預期的才用）。</summary>
    public static bool UpdateBaselines =>
        Environment.GetEnvironmentVariable("UPDATE_BASELINES") is "1" or "true" or "TRUE";

    private static string Ensure(string dir)
    {
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExcelPasteTool.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("找不到專案根目錄（含 ExcelPasteTool.csproj 的資料夾）。");
    }
}
