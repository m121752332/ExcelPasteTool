using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExcelPasteTool;

public class AppConfig
{
    public string Language { get; set; } = "TraditionalChinese";
    public string Theme { get; set; } = "Dark";
}

public static class ConfigServices
{
    private static readonly string ConfigPath = "config.json";
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
            Console.WriteLine($"[警告] 未找到設定檔: {ConfigPath}");
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
