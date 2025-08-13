using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ExcelPasteTool;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ConfigServices.Load();
        // ���ιw�]�y��
        if (Enum.TryParse<AppLanguage>(ConfigServices.Config.Language, out var lang))
            LanguageManager.CurrentLanguage = lang;
        // ���ιw�]�D�D
        if (Enum.TryParse<AppTheme>(ConfigServices.Config.Theme, out var theme))
            ThemeManager.CurrentTheme = theme;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            
            // ��l�ƥD�D�t��
            ThemeManager.ApplyTheme(ThemeManager.CurrentTheme);
        }

        base.OnFrameworkInitializationCompleted();
    }
}