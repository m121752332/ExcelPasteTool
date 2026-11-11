using Avalonia;
using System;

namespace ExcelPasteTool;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // Windows 改用軟體繪製：省去啟動時初始化 GPU（ANGLE / Direct3D）的約 0.3～0.4 秒。
            // 本工具畫面單純，CPU 繪製已足夠；若日後加入大量動畫，可改回 AngleEgl 優先。
            .With(new Win32PlatformOptions { RenderingMode = new[] { Win32RenderingMode.Software } })
            .LogToTrace();
}
