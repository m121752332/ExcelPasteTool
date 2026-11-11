using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ExcelPasteTool;

/// <summary>說明的開啟方式。列舉名稱會存進 config.json，請勿更名。</summary>
public enum HelpViewerMode
{
    /// <summary>在 App 內用系統內建的 WebView 顯示。</summary>
    InApp,
    /// <summary>用使用者的預設瀏覽器開啟。</summary>
    Browser
}

/// <summary>
/// 使用說明：把 Assets/Help 內的內容（各語言一份）套上目前主題的配色，組成一份獨立的 HTML。
/// App 內的 WebView 與預設瀏覽器共用同一份輸出檔。
/// </summary>
public static class HelpService
{
    private const string HelpFolder = "Assets/Help/";

    /// <summary>CSS 變數名稱 → 主題配色的資源鍵值（Assets/Themes/*Palette.axaml）。</summary>
    private static readonly (string CssVar, string ResourceKey, string Fallback)[] ThemeColors =
    {
        ("--bg", "WindowBackgroundBrush", "#0D1117"),
        ("--panel", "PanelBackgroundBrush", "#161B22"),
        ("--border", "PanelBorderBrush", "#30363D"),
        ("--input", "InputBackgroundBrush", "#0D1117"),
        ("--text", "TextPrimaryBrush", "#E6EDF3"),
        ("--muted", "TextSecondaryBrush", "#8B949E"),
        ("--accent", "AccentBrush", "#238636"),
        ("--on-accent", "TextOnAccentBrush", "#FFFFFF"),
        ("--link", "AccentTextBrush", "#58A6FF"),
        ("--sql", "SqlOutputBrush", "#3FB950"),
        ("--danger", "DangerBrush", "#F85149"),
        ("--control", "ControlBackgroundBrush", "#21262D"),
        ("--control-border", "ControlBorderBrush", "#363B42"),
        ("--table-head", "TableHeaderBackgroundBrush", "#161B22"),
        ("--row-even", "TableEvenRowBrush", "#161B22"),
        ("--grid-line", "TableGridLineBrush", "#21262D"),
        ("--sidebar", "SidebarBackgroundBrush", "#010409"),
        ("--marker", "SidebarIndicatorBrush", "#F78166"),
    };

    public static HelpViewerMode ViewerMode
    {
        get => Enum.TryParse<HelpViewerMode>(ConfigServices.Config.HelpViewer, out var mode) ? mode : HelpViewerMode.InApp;
        set
        {
            ConfigServices.Config.HelpViewer = value.ToString();
            ConfigServices.Save();
        }
    }

    public static string GetViewerModeName(HelpViewerMode mode) => mode switch
    {
        HelpViewerMode.Browser => LanguageManager.GetText("HelpViewerBrowser", "預設瀏覽器"),
        _ => LanguageManager.GetText("HelpViewerInApp", "內建視窗")
    };

    public static List<(HelpViewerMode Mode, string Name)> GetAllViewerModes() =>
        new[] { HelpViewerMode.InApp, HelpViewerMode.Browser }
            .Select(m => (m, GetViewerModeName(m)))
            .ToList();

    /// <summary>
    /// 這台電腦是否有可用的內嵌瀏覽器引擎
    /// （Windows：WebView2；macOS：WKWebView；Linux：WebKitGTK 或 WPE WebKit）。
    /// </summary>
    public static bool IsEmbeddedViewerAvailable()
    {
        try
        {
            WebViewAdapterType[] candidates =
                OperatingSystem.IsWindows() ? new[] { WebViewAdapterType.WebView2 }
                : OperatingSystem.IsMacOS() ? new[] { WebViewAdapterType.WkWebView }
                : new[] { WebViewAdapterType.WebKitGtk, WebViewAdapterType.WpeWebKit };

            return candidates.Any(type =>
            {
                var info = WebViewAdapterInfo.GetAdapterInfo(type);
                return info.IsSupported && info.IsInstalled;
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Help] 無法偵測 WebView：{ex.Message}");
            return false;
        }
    }

    /// <summary>WebView2 的使用者資料夾：放在使用者的本機資料夾，不要寫在執行檔旁邊。</summary>
    public static string WebViewUserDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Global.AssemblyName, "WebView");

    /// <summary>依目前的語言、主題與字體大小組出完整的 HTML。</summary>
    public static string BuildHtml()
    {
        var (langCode, fileSuffix) = LanguageManager.CurrentLanguage switch
        {
            AppLanguage.SimplifiedChinese => ("zh-Hans", "zh-CN"),
            AppLanguage.English => ("en", "en"),
            _ => ("zh-Hant", "zh-TW")
        };

        var css = ReadAsset("help.css");
        var body = ReadAsset($"help.{fileSuffix}.html");
        var title = LanguageManager.GetText("HelpTitle", "說明");

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html>\n<html lang=\"").Append(langCode).Append("\">\n<head>\n");
        sb.Append("<meta charset=\"utf-8\">\n");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        sb.Append("<title>").Append(System.Net.WebUtility.HtmlEncode($"{title} - DataSplitter Pro")).Append("</title>\n");
        sb.Append("<style>\n").Append(BuildThemeVariables()).Append(css).Append("\n</style>\n");
        sb.Append("</head>\n<body>\n").Append(body).Append("\n</body>\n</html>\n");
        return sb.ToString();
    }

    /// <summary>把說明寫成暫存資料夾裡的 HTML 檔，回傳檔案路徑。</summary>
    public static string WriteHelpFile()
    {
        var folder = Path.Combine(Path.GetTempPath(), Global.AssemblyName, "help");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "help.html");
        File.WriteAllText(path, BuildHtml(), new UTF8Encoding(false));
        return path;
    }

    /// <summary>用預設瀏覽器開啟說明；失敗時回傳 false。</summary>
    public static bool OpenInBrowser()
    {
        try
        {
            return OpenExternal(WriteHelpFile());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Help] 無法產生說明檔：{ex.Message}");
            return false;
        }
    }

    /// <summary>用系統預設程式開啟檔案或網址。</summary>
    public static bool OpenExternal(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Help] 無法開啟 {target}：{ex.Message}");
            return false;
        }
    }

    private static string ReadAsset(string fileName)
    {
        var uri = new Uri($"avares://{Global.AssemblyName}/{HelpFolder}{fileName}");
        using var stream = AssetLoader.Open(uri);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string BuildThemeVariables()
    {
        var app = Application.Current;
        var sb = new StringBuilder(":root {\n");
        foreach (var (cssVar, resourceKey, fallback) in ThemeColors)
        {
            var value = fallback;
            if (app != null && app.TryGetResource(resourceKey, app.ActualThemeVariant, out var resource)
                && resource is ISolidColorBrush brush)
            {
                var c = brush.Color;
                value = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            }
            sb.Append("  ").Append(cssVar).Append(": ").Append(value).Append(";\n");
        }
        var fontSize = (FontManager.CurrentFontSize + 1).ToString("0.#", CultureInfo.InvariantCulture);
        sb.Append("  --font-size: ").Append(fontSize).Append("px;\n");
        // 使用者在設定頁選的字型排在最前面；留空時用樣式表內的預設字型
        var family = new string(FontManager.CurrentFontFamily.Where(ch => ch is not ('"' or '\\' or ';' or '<' or '>' or '{' or '}')).ToArray()).Trim();
        sb.Append("  --font-family: ").Append(family.Length > 0 ? $"\"{family}\"," : "").Append(";\n");
        sb.Append("  color-scheme: ").Append(ThemeManager.IsDark(ThemeManager.CurrentTheme) ? "dark" : "light").Append(";\n");
        sb.Append("}\n");
        return sb.ToString();
    }
}
