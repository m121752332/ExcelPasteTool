using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Media;
using ExcelPasteTool.Helpers;
using ExcelPasteTool.UITests.Infrastructure;

namespace ExcelPasteTool.UITests.Tests;

/// <summary>
/// 設定頁可展開列的動畫：高度與透明度漸變、箭頭旋轉、收合時等動畫結束才把標題列圓角改回來。
/// 其他 UI 測試都關掉動畫直接看最終狀態，這裡打開動畫並等它跑完。
/// </summary>
public class ExpanderAnimationTests : IDisposable
{
    public ExpanderAnimationTests() => ExpanderAnimator.AnimationsEnabled = true;
    public void Dispose() => ExpanderAnimator.AnimationsEnabled = false;

    private static async Task WaitForAnimationAsync()
    {
        var until = DateTime.UtcNow + ExpanderAnimator.Duration + TimeSpan.FromMilliseconds(200);
        while (DateTime.UtcNow < until)
        {
            await Task.Delay(16);
            MainWindowHost.Pump();
        }
    }

    private static void Click(MainWindowHost host, string name)
    {
        host.Find<Button>(name).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        MainWindowHost.Pump();
    }

    private static double ChevronAngle(Control chevron) =>
        chevron.RenderTransform?.Value is { } m ? Math.Round(Math.Atan2(m.M12, m.M11) * 180 / Math.PI) : 0;

    [AvaloniaFact]
    public async Task DeveloperGroup_CollapseAndExpand_AnimateTogether()
    {
        using var host = MainWindowHost.Open();
        ExpanderAnimator.AnimationsEnabled = true;
        host.Window.SidebarItemClickCommand.Execute(SidebarPage.Settings);
        MainWindowHost.Pump();
        Click(host, "DeveloperCategoryButton");

        var header = host.Find<Button>("DeveloperGroupExpandButton");
        var body = host.Find<Border>("DeveloperGroupBody");
        var chevron = host.Find<Avalonia.Controls.Shapes.Path>("DeveloperGroupChevron");
        var fullHeight = body.Bounds.Height;
        Assert.True(fullHeight > 50);
        Assert.Contains("expanded", header.Classes);

        // 收合：動畫期間內容還在、標題列維持上圓下直；跑完後才隱藏並改回四角圓、箭頭轉回 0 度
        Click(host, "DeveloperGroupExpandButton");
        Assert.True(body.IsVisible);
        Assert.Contains("expanded", header.Classes);
        await WaitForAnimationAsync();
        Assert.False(body.IsVisible);
        Assert.DoesNotContain("expanded", header.Classes);
        Assert.Equal(0, ChevronAngle(chevron));

        // 展開：一開始就顯示（從 0 高度長出來），跑完後高度回到完整、箭頭轉 180 度
        Click(host, "DeveloperGroupExpandButton");
        Assert.True(body.IsVisible);
        Assert.Contains("expanded", header.Classes);
        Assert.True(body.Bounds.Height < fullHeight, $"展開動畫應從 0 開始長高，目前 {body.Bounds.Height}");
        await WaitForAnimationAsync();
        Assert.Equal(double.PositiveInfinity, body.MaxHeight);
        Assert.Equal(fullHeight, body.Bounds.Height, 1);
        Assert.Equal(1, body.Opacity);
        Assert.Equal(180, Math.Abs(ChevronAngle(chevron)));
    }
}
