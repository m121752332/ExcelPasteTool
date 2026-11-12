using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;

[assembly: AvaloniaTestApplication(typeof(ExcelPasteTool.UITests.TestAppBuilder))]
// 主題、語言、字型都是靜態狀態，UI 測試一律依序執行，避免互相干擾
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ExcelPasteTool.UITests;

/// <summary>
/// 無畫面測試用的 Avalonia 啟動設定：沿用正式的 App（同一份 App.axaml、配色與樣式），
/// 但平台換成 Headless，並關掉假繪製（UseHeadlessDrawing = false）改用 Skia 真實繪製，才擷取得到畫面。
/// 預設字型固定為內建的 Noto Sans TC，讓不同電腦產生的截圖盡量一致。
/// </summary>
public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
        .With(new FontManagerOptions
        {
            DefaultFamilyName = "avares://DataSplitter/Assets/Fonts#Noto Sans TC"
        });
}
