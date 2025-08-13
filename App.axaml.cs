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
        // 應用預設語言
        if (Enum.TryParse<AppLanguage>(ConfigServices.Config.Language, out var lang))
            LanguageManager.CurrentLanguage = lang;
        // 應用預設主題
        if (Enum.TryParse<AppTheme>(ConfigServices.Config.Theme, out var theme))
            ThemeManager.CurrentTheme = theme;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            
            // 初始化主題系統
            ThemeManager.ApplyTheme(ThemeManager.CurrentTheme);
        }

        base.OnFrameworkInitializationCompleted();
    }
}