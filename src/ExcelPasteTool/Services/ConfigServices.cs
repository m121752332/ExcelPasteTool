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
    /// <summary>工具頁的「SQL 條件」區塊是否預設顯示（預設顯示；舊版設定檔沒有此欄位時也是顯示）。</summary>
    public bool ShowSqlPanel { get; set; } = true;
    /// <summary>應用程式內通知出現的位置：BottomRight（預設）、TopRight、BottomLeft。</summary>
    public string ToastPosition { get; set; } = "BottomRight";
    /// <summary>同時最多顯示幾則通知（1 ~ 5，預設 3），超過時最舊的先關閉。</summary>
    public int ToastMaxVisible { get; set; } = 3;
    /// <summary>SQL 條件的欄位名稱（例如 ima01）；空白時只輸出括號內容。</summary>
    public string SqlColumnName { get; set; } = "";
    /// <summary>SQL 條件：全部都是數字時不加單引號。</summary>
    public bool SqlUnquoteNumbers { get; set; }
    /// <summary>SQL 條件：括號內每行幾個值，0 代表不換行。</summary>
    public int SqlItemsPerLine { get; set; }
    /// <summary>工具頁左側「分隔資料」與「SQL 條件」的高度比例（分隔資料所佔比例，0.2 ~ 0.8；0 代表預設各半）。</summary>
    public double LeftPanelSplitRatio { get; set; }
    /// <summary>開發者模式：開啟後按 F12 連線到 Avalonia Developer Tools（預設關閉；關閉時 F12 沒有作用）。</summary>
    public bool DeveloperMode { get; set; }
    /// <summary>開發者工具視窗的文字大小：Small（8）、Medium（12，預設）、Large（16），跟 App 本身的字體大小分開。</summary>
    public string DevToolsFontSize { get; set; } = "Medium";
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
