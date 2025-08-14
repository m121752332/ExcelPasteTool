using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;
using System.Collections.Generic;

namespace ExcelPasteTool;

public enum AppTheme
{
    Light,
    Dark
}

public static class ThemeManager
{
    private static readonly object _themeLock = new();

    // 預設主題
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
        lock (_themeLock)
        {
            var app = Application.Current;
            if (app?.Styles == null) return;

            // 1) 讓 FluentTheme 跟著切換 Light/Dark，避免內建控件（TextBox、ComboBox 下拉、Popup 等）出現黑色
            app.RequestedThemeVariant = theme switch
            {
                AppTheme.Light => ThemeVariant.Light,
                AppTheme.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Dark
            };

            // 2) 移除既有的自訂主題 StyleInclude（避免重覆加入）
            for (int i = app.Styles.Count - 1; i >= 0; i--)
            {
                if (app.Styles[i] is StyleInclude styleInclude &&
                    styleInclude.Source is not null)
                {
                    var path = styleInclude.Source.ToString() ?? string.Empty;
                    if (path.Contains("Assets/Styles/LightTheme.axaml", StringComparison.OrdinalIgnoreCase) ||
                        path.Contains("Assets/Styles/DarkTheme.axaml", StringComparison.OrdinalIgnoreCase))
                    {
                        app.Styles.RemoveAt(i);
                    }
                }
            }

            // 3) 載入對應主題的自訂樣式
            string themePath = theme switch
            {
                AppTheme.Light => $"avares://{Global.AssemblyName}/Assets/Styles/LightTheme.axaml",
                AppTheme.Dark => $"avares://{Global.AssemblyName}/Assets/Styles/DarkTheme.axaml",
                _ => $"avares://{Global.AssemblyName}/Assets/Styles/DarkTheme.axaml"
            };

            try
            {
                var themeStyle = new StyleInclude(new Uri($"avares://{Global.AssemblyName}/"))
                {
                    Source = new Uri(themePath)
                };
                app.Styles.Add(themeStyle);
            }
            catch
            {
                // 載入失敗時忽略，避免崩潰
            }
        }
    }

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
            AppTheme.Light => LanguageManager.GetText("ThemeLight", "光線模式"),
            AppTheme.Dark => LanguageManager.GetText("ThemeDark", "暗黑模式"),
            _ => LanguageManager.GetText("ThemeDark", "暗黑模式")
        };
    }

    public static List<(AppTheme Theme, string Name)> GetAllThemes()
    {
        return new List<(AppTheme, string)>
        {
            (AppTheme.Dark, LanguageManager.GetText("ThemeDark", "暗黑模式")),
            (AppTheme.Light, LanguageManager.GetText("ThemeLight", "光線模式"))
        };
    }
}