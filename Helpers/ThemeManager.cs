using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;
using System.Collections.Generic;

namespace ExcelPasteTool;

public enum AppTheme
{
    Light,      // ���u�Ҧ�
    Dark         // �t�¼Ҧ�
}

public static class ThemeManager
{
    private static readonly object _themeLock = new();

    // �]�w�`��Ҧ����q�{�D�D
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

            // �M���{�����۩w�q�D�D
            for (int i = app.Styles.Count - 1; i >= 0; i--)
            {
                if (app.Styles[i] is StyleInclude styleInclude && 
                    styleInclude.Source?.AbsolutePath?.Contains("Themes") == true)
                {
                    app.Styles.RemoveAt(i);
                }
            }

            // ���J�s�D�D
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
                // �p�G�D�D���J���ѡA�h�O���즳�D�D
            }
        }
    }

    // �O�d�`����k�H�K�N���X�R
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
            AppTheme.Light => "���u�Ҧ�",
            AppTheme.Dark => "�t�¼Ҧ�",
            _ => "�����D�D"
        };
    }

    // �s�W�G��o�Ҧ��i�ΥD�D���C��
    public static List<(AppTheme Theme, string Name)> GetAllThemes()
    {
        return new List<(AppTheme, string)>
        {
            (AppTheme.Dark, "�t�¼Ҧ�"),
            (AppTheme.Light, "���u�Ҧ�")
        };
    }
}