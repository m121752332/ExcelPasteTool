using Avalonia;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExcelPasteTool;

/// <summary>
/// 字型與字體大小管理。設定存在 config.json（FontFamily / FontSize），
/// 套用方式是改寫 Application.Resources 中的 AppFontFamily / AppFontSize* 資源，
/// 所有樣式以 DynamicResource 綁定，所以改完立即生效、不必重開 App。
/// </summary>
public static class FontManager
{
    public const double MinFontSize = 10;
    public const double MaxFontSize = 24;
    public const double DefaultFontSize = 14;

    /// <summary>config.json 中 FontFamily 為空字串時代表使用內建字型。</summary>
    public const string DefaultFontKey = "";

    public static event Action? FontChanged;

    private static FontFamily BuiltInFont =>
        new($"avares://{Global.AssemblyName}/Assets/Fonts#Noto Sans TC");

    public static string BuiltInFontDisplayName =>
        LanguageManager.GetText("FontDefault", "Noto Sans TC（內建）");

    public static string CurrentFontFamily { get; private set; } = DefaultFontKey;
    public static double CurrentFontSize { get; private set; } = DefaultFontSize;

    public static double ClampSize(double size)
    {
        if (double.IsNaN(size) || size <= 0) return DefaultFontSize;
        return Math.Round(Math.Clamp(size, MinFontSize, MaxFontSize));
    }

    /// <summary>套用字型與大小（不存檔）。</summary>
    public static void Apply(string? fontFamily, double fontSize)
    {
        CurrentFontFamily = fontFamily ?? DefaultFontKey;
        CurrentFontSize = ClampSize(fontSize);

        var app = Application.Current;
        if (app != null)
        {
            var family = string.IsNullOrWhiteSpace(CurrentFontFamily)
                ? BuiltInFont
                : new FontFamily(CurrentFontFamily);
            var size = CurrentFontSize;

            app.Resources["AppFontFamily"] = family;
            app.Resources["AppFontSize"] = size;
            app.Resources["AppFontSizeSmall"] = Math.Max(MinFontSize - 1, size - 2);
            app.Resources["AppFontSizeLarge"] = size + 2;
            app.Resources["AppFontSizeTitle"] = Math.Round(size * 1.8);
            // Fluent 內建控制項（Button、ComboBox、TextBox…）的預設字型與大小
            app.Resources["ContentControlThemeFontFamily"] = family;
            app.Resources["ControlContentThemeFontSize"] = size;
        }

        FontChanged?.Invoke();
    }

    /// <summary>套用並寫入 config.json。</summary>
    public static void ApplyAndSave(string? fontFamily, double fontSize)
    {
        Apply(fontFamily, fontSize);
        ConfigServices.Config.FontFamily = CurrentFontFamily;
        ConfigServices.Config.FontSize = CurrentFontSize;
        ConfigServices.Save();
    }

    /// <summary>系統已安裝的字型名稱（排序後），第一項由呼叫端放內建字型。</summary>
    public static List<string> GetSystemFontFamilies()
    {
        try
        {
            return Avalonia.Media.FontManager.Current.SystemFonts
                .Select(f => f.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n) && !n.StartsWith('.') && !n.StartsWith('@'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    // 保留舊介面：語言切換時通知 UI 重繪
    public static void ApplyLanguageFont(AppLanguage language)
    {
        FontChanged?.Invoke();
    }

    public static string GetLanguageFontFamily(AppLanguage language)
    {
        return LanguageManager.GetLanguageFontFamily(language);
    }
}
