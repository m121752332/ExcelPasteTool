namespace ExcelPasteTool.E2ETests.Infrastructure;

/// <summary>端到端測試用到的路徑：要啟動的 exe、產出物資料夾。</summary>
public static class E2EPaths
{
#if DEBUG
    private const string Configuration = "Debug";
#else
    private const string Configuration = "Release";
#endif

    public static string RepoRoot { get; } = FindRepoRoot();

    /// <summary>
    /// 產出物根目錄，預設 TestResults/e2e（已在 .gitignore）。
    /// 可用環境變數 <c>E2E_RESULTS_DIR</c> 改位置（tests/run-e2e-tests.ps1 會設）。
    /// </summary>
    public static string ResultsDir
    {
        get
        {
            var custom = Environment.GetEnvironmentVariable("E2E_RESULTS_DIR");
            return string.IsNullOrWhiteSpace(custom)
                ? Path.Combine(RepoRoot, "TestResults", "e2e")
                : Path.GetFullPath(custom);
        }
    }

    /// <summary>
    /// 主程式的建置輸出資料夾（內含 DataSplitter.exe）。
    /// 可用環境變數 <c>DATASPLITTER_EXE</c> 指定別的 exe（例如發佈後的單一檔案）。
    /// </summary>
    public static string AppExe
    {
        get
        {
            var custom = Environment.GetEnvironmentVariable("DATASPLITTER_EXE");
            if (!string.IsNullOrWhiteSpace(custom)) return Path.GetFullPath(custom);
            return Path.Combine(RepoRoot, "src", "ExcelPasteTool", "bin", Configuration, "net11.0", "DataSplitter.exe");
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExcelPasteTool.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException($"找不到 ExcelPasteTool.sln（從 {AppContext.BaseDirectory} 往上找）");
    }
}
