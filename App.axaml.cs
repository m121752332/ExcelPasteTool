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
        // 套用預設語言
        if (Enum.TryParse<AppLanguage>(ConfigServices.Config.Language, out var lang))
            LanguageManager.CurrentLanguage = lang;
        // 套用預設主題
        if (Enum.TryParse<AppTheme>(ConfigServices.Config.Theme, out var theme))
            ThemeManager.CurrentTheme = theme;

        // 初始化主題配色與字型（在建立視窗前，避免第一個畫面閃爍）
        ThemeManager.ApplyTheme(ThemeManager.CurrentTheme);
        FontManager.Apply(ConfigServices.Config.FontFamily, ConfigServices.Config.FontSize);
        LanguageManager.Initialize();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
