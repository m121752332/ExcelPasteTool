using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;
using System.Collections.Generic;

namespace ExcelPasteTool;

public enum AppTheme
{
    Modern,      // 現代化主題
    Dark         // 暗黑模式（黑底橘色字體）
}

public static class ThemeManager
{
    // 設定暗黑模式作為預設主題
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

    public static void ApplyTheme(AppTheme theme)
    {
        var app = Application.Current;
        if (app?.Styles == null) return;

        // 清除現有的自定義主題
        for (int i = app.Styles.Count - 1; i >= 0; i--)
        {
            if (app.Styles[i] is StyleInclude styleInclude && 
                styleInclude.Source?.AbsolutePath?.Contains("Themes") == true)
            {
                app.Styles.RemoveAt(i);
            }
        }

        // 載入新主題
        string themePath = theme switch
        {
            AppTheme.Modern => "avares://ExcelPasteTool/Themes/ModernTheme.axaml",
            AppTheme.Dark => "avares://ExcelPasteTool/Themes/DarkTheme.axaml",
            _ => "avares://ExcelPasteTool/Themes/DarkTheme.axaml"  // 預設設定暗黑模式
        };

        try
        {
            var themeStyle = new StyleInclude(new Uri("avares://ExcelPasteTool/"))
            {
                Source = new Uri(themePath)
            };
            app.Styles.Add(themeStyle);
        }
        catch
        {
            // 如果主題載入失敗，則保持原來主題
        }
    }

    // 保留切換方法以便日後擴充
    public static void NextTheme()
    {
        var values = Enum.GetValues<AppTheme>();
        var currentIndex = Array.IndexOf(values, CurrentTheme);
        var nextIndex = (currentIndex + 1) % values.Length;
        CurrentTheme = values[nextIndex];
    }

    public static string GetThemeName(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.Modern => "現代化主題",
            AppTheme.Dark => "暗黑模式",
            _ => "未知主題"
        };
    }

    // 新增：取得所有可用主題的列表
    public static List<(AppTheme Theme, string Name)> GetAllThemes()
    {
        return new List<(AppTheme, string)>
        {
            (AppTheme.Dark, "暗黑模式"),
            (AppTheme.Modern, "現代化主題")
        };
    }
}