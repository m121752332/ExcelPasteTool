using System.Text.Json;

namespace ExcelPasteTool;

public class AppConfig
{
    public string Language { get; set; } = "TraditionalChinese";
    public string Theme { get; set; } = "Dark";
    /// <summary>字型名稱；空字串代表使用內建 Noto Sans TC。</summary>
    public string FontFamily { get; set; } = "";
    /// <summary>基本字體大小（10 ~ 24）。</summary>
    public double FontSize { get; set; } = 14;
    /// <summary>說明的開啟方式：InApp（內建視窗，預設）或 Browser（預設瀏覽器）。</summary>
    public string HelpViewer { get; set; } = "InApp";
    /// <summary>是否已看過 App 導覽；false（或舊版設定檔沒有此欄位）時，啟動後會自動播放一次。</summary>
    public bool TourCompleted { get; set; }
    /// <summary>是否使用精簡標題列（隱藏系統標題列，只留側邊欄開關與最小化 / 最大化 / 關閉）。</summary>
    public bool CompactTitleBar { get; set; }
}

public static class ConfigServices
{
    /// <summary>
    /// 設定檔固定放在執行檔旁邊，避免從不同工作目錄啟動時讀到不同設定。
    /// macOS 的 .app 例外：放在 ~/Library/Application Support/DataSplitter/（見 AppPaths）。
    /// </summary>
    public static readonly string ConfigPath = AppPaths.ResolveConfigPath();

    public static AppConfig Config { get; private set; } = new();

    public static void Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config != null)
                    Config = config;
            }
            catch { }
        }
        else
        {
            Console.WriteLine($"[警告] 找不到設定檔，將使用預設值: {ConfigPath}");
        }
    }

    public static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }
}
