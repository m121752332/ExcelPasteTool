using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;
using System.Collections.Generic;

namespace ExcelPasteTool;

public enum AppTheme
{
    Light,      // 絬家Α
    Dark         // 穞堵家Α
}

public static class ThemeManager
{
    private static readonly object _themeLock = new();

    // 砞﹚瞏︹家Α纐粄肈
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

            // 睲埃瞷Τ﹚竡肈
            for (int i = app.Styles.Count - 1; i >= 0; i--)
            {
                if (app.Styles[i] is StyleInclude styleInclude && 
                    styleInclude.Source?.AbsolutePath?.Contains("Themes") == true)
                {
                    app.Styles.RemoveAt(i);
                }
            }

            // 更穝肈
            string themePath = theme switch
            {
                AppTheme.Light => $"avares://{Global.AssemblyName}/Themes/LightTheme.axaml",
                AppTheme.Dark => $"avares://{Global.AssemblyName}/Themes/DarkTheme.axaml",
                _ => $"avares://{Global.AssemblyName}/Themes/DarkTheme.axaml"
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
                // 狦肈更ア毖玥玂Τ肈
            }
        }
    }

    // 玂痙碻吏よ猭獽盢ㄓ耎
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
            AppTheme.Light => "絬家Α",
            AppTheme.Dark => "穞堵家Α",
            _ => "ゼ肈"
        };
    }

    // 穝糤莉眔┮Τノ肈
    public static List<(AppTheme Theme, string Name)> GetAllThemes()
    {
        return new List<(AppTheme, string)>
        {
            (AppTheme.Dark, "穞堵家Α"),
            (AppTheme.Light, "絬家Α")
        };
    }
}