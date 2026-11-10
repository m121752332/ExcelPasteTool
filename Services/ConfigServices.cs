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
}

public static class ConfigServices
{
    /// <summary>
    /// 設定檔固定放在執行檔旁邊，避免從不同工作目錄啟動時讀到不同設定。
    /// </summary>
    public static readonly string ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.json");

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
