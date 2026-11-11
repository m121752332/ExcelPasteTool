using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExcelPasteTool;

/// <summary>
/// App 主題。列舉名稱會存進 config.json，請勿更名（Dark 保留給舊設定相容，現在是 GitHub Dark 配色）。
/// </summary>
public enum AppTheme
{
    Light,
    Dark,
    Tiffany,
    /// <summary>彩虹幻境（實驗性、純屬好玩的漸層主題）。</summary>
    Rainbow
}

/// <summary>
/// 主題管理：每個主題只是一份配色 ResourceDictionary（Assets/Themes/*Palette.axaml），
/// 切換時替換 Application.Resources.MergedDictionaries 中的配色，
/// 樣式（Assets/Styles/AppStyles.axaml）透過 DynamicResource 自動套用新顏色。
/// </summary>
public static class ThemeManager
{
    private const string PaletteFolder = "Assets/Themes/";

    private static AppTheme _currentTheme = AppTheme.Dark;

    public static AppTheme CurrentTheme
    {
        get => _currentTheme;
        set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                ApplyTheme(value);
                ThemeChanged?.Invoke();
            }
        }
    }

    public static event Action? ThemeChanged;

    private static string GetPaletteFile(AppTheme theme) => theme switch
    {
        AppTheme.Light => "LightPalette.axaml",
        AppTheme.Tiffany => "TiffanyPalette.axaml",
        AppTheme.Rainbow => "RainbowPalette.axaml",
        _ => "GitHubDarkPalette.axaml"
    };

    /// <summary>Fluent 內建控制項（下拉、捲軸、Slider…）要用亮或暗的底版。</summary>
    private static ThemeVariant GetVariant(AppTheme theme) => theme switch
    {
        AppTheme.Dark => ThemeVariant.Dark,
        AppTheme.Rainbow => ThemeVariant.Dark,
        _ => ThemeVariant.Light
    };

    public static bool IsDark(AppTheme theme) => GetVariant(theme) == ThemeVariant.Dark;

    public static void ApplyTheme(AppTheme theme)
    {
        var app = Application.Current;
        if (app == null) return;

        app.RequestedThemeVariant = GetVariant(theme);

        var merged = app.Resources.MergedDictionaries;
        for (int i = merged.Count - 1; i >= 0; i--)
        {
            if (merged[i] is ResourceInclude include &&
                include.Source?.ToString().Contains(PaletteFolder, StringComparison.OrdinalIgnoreCase) == true)
            {
                merged.RemoveAt(i);
            }
        }

        var baseUri = new Uri($"avares://{Global.AssemblyName}/");
        merged.Add(new ResourceInclude(baseUri)
        {
            Source = new Uri($"avares://{Global.AssemblyName}/{PaletteFolder}{GetPaletteFile(theme)}")
        });
    }

    public static void NextTheme()
    {
        var values = Enum.GetValues<AppTheme>();
        var nextIndex = (Array.IndexOf(values, CurrentTheme) + 1) % values.Length;
        CurrentTheme = values[nextIndex];
    }

    public static string GetThemeName(AppTheme theme) => theme switch
    {
        AppTheme.Light => LanguageManager.GetText("ThemeLight", "光線模式"),
        AppTheme.Tiffany => LanguageManager.GetText("ThemeTiffany", "蒂芬妮藍"),
        AppTheme.Rainbow => LanguageManager.GetText("ThemeRainbow", "彩虹幻境"),
        _ => LanguageManager.GetText("ThemeDark", "GitHub 暗黑")
    };

    /// <summary>下拉選單顯示順序。</summary>
    public static List<(AppTheme Theme, string Name)> GetAllThemes()
    {
        return new[] { AppTheme.Dark, AppTheme.Light, AppTheme.Tiffany, AppTheme.Rainbow }
            .Select(t => (t, GetThemeName(t)))
            .ToList();
    }
}
