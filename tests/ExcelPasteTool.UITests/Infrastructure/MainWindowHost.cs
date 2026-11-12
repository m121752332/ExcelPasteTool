using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ExcelPasteTool.UITests.Infrastructure;

/// <summary>
/// 開一個無畫面的主視窗，並把會影響畫面的靜態狀態（主題、語言、字型、導覽、檢查更新）重設成固定值，
/// 讓每個測試都從相同的起點開始、截圖可重現。用 using 包起來，結束時會關閉視窗。
/// </summary>
public sealed class MainWindowHost : IDisposable
{
    public const double DefaultWidth = 1100;
    public const double DefaultHeight = 700;

    public MainWindow Window { get; }

    private MainWindowHost(MainWindow window) => Window = window;

    public static MainWindowHost Open(
        AppTheme theme = AppTheme.Dark,
        AppLanguage language = AppLanguage.TraditionalChinese,
        double width = DefaultWidth,
        double height = DefaultHeight)
    {
        ResetAppState(theme, language);
        var window = new MainWindow { Width = width, Height = height };
        window.Show();
        Pump();
        return new MainWindowHost(window);
    }

    /// <summary>主題、語言、字型回到指定值；關掉首次導覽與啟動時檢查更新；側邊欄頁面改成每次新建。</summary>
    public static void ResetAppState(AppTheme theme, AppLanguage language)
    {
        MainWindow.SkipStartupChecks = true;
        ConfigServices.Config.TourCompleted = true;
        ConfigServices.Config.CompactTitleBar = false;
        SidebarPageToContentConverter.ResetCache();

        LanguageManager.CurrentLanguage = language;
        ThemeManager.CurrentTheme = theme;
        ThemeManager.ApplyTheme(theme);
        FontManager.Apply("", 14);
        Pump();
    }

    /// <summary>跑完排隊中的 UI 工作並推進一次繪製，讓版面與畫面跟上最新狀態。</summary>
    public static void Pump()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>依 Name 找控制項（會往 UserControl 裡面找）。找不到直接讓測試失敗並列出原因。</summary>
    public T Find<T>(string name) where T : Control
    {
        Pump();
        return Window.GetVisualDescendants().OfType<T>().FirstOrDefault(c => c.Name == name)
               ?? throw new Xunit.Sdk.XunitException($"畫面上找不到 {typeof(T).Name}「{name}」");
    }

    /// <summary>把焦點放到控制項上，模擬使用者打字。</summary>
    public void TypeInto(Control target, string text)
    {
        target.Focus();
        Pump();
        Window.KeyTextInput(text);
        Pump();
    }

    public void PressKey(Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Window.KeyPress(key, modifiers, PhysicalKey.None, null);
        Window.KeyRelease(key, modifiers, PhysicalKey.None, null);
        Pump();
    }

    public void Dispose()
    {
        Window.Close();
        Pump();
    }
}
