namespace ExcelPasteTool;

/// <summary>
/// 程式安裝位置相關的路徑判斷。
///
/// macOS 版從 1.4.2 起包成 DataSplitter.app，執行檔位於 DataSplitter.app/Contents/MacOS/。
/// 為了不破壞 .app 的簽章封裝，設定檔改放在 ~/Library/Application Support/DataSplitter/，
/// 其他平台（以及直接執行裸執行檔時）維持放在執行檔旁邊。
/// </summary>
public static class AppPaths
{
    /// <summary>執行檔所在資料夾（不含結尾斜線）。</summary>
    public static string ExeDirectory =>
        AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>在 macOS 從 .app 執行時，回傳 .app 的完整路徑；否則為 null。</summary>
    public static string? MacBundlePath { get; } = ResolveMacBundlePath();

    private static string? ResolveMacBundlePath()
    {
        if (!OperatingSystem.IsMacOS()) return null;
        var macOsDir = new DirectoryInfo(ExeDirectory);
        var contents = macOsDir.Parent;
        var bundle = contents?.Parent;
        if (bundle == null) return null;
        if (!string.Equals(macOsDir.Name, "MacOS", StringComparison.Ordinal)) return null;
        if (!string.Equals(contents!.Name, "Contents", StringComparison.Ordinal)) return null;
        if (!bundle.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase)) return null;
        return bundle.FullName.TrimEnd(Path.DirectorySeparatorChar);
    }

    /// <summary>.app 內設定檔所在的資料夾：~/Library/Application Support/DataSplitter。</summary>
    public static string MacSupportDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "Application Support", "DataSplitter");

    /// <summary>
    /// 設定檔路徑。從 .app 執行時若新位置還沒有設定檔，
    /// 會把舊版（裸執行檔時期）放在 .app 旁邊的 config.json 複製過來，保留使用者設定。
    /// </summary>
    public static string ResolveConfigPath()
    {
        var besideExe = Path.Combine(AppContext.BaseDirectory, "config.json");
        if (MacBundlePath == null) return besideExe;

        try
        {
            var dir = MacSupportDirectory;
            Directory.CreateDirectory(dir);
            var target = Path.Combine(dir, "config.json");
            if (!File.Exists(target))
            {
                var legacy = Path.Combine(Path.GetDirectoryName(MacBundlePath) ?? dir, "config.json");
                if (File.Exists(legacy))
                    File.Copy(legacy, target);
            }
            return target;
        }
        catch
        {
            return besideExe;
        }
    }
}
