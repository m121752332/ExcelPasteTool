using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using ExcelPasteTool.UITests.Infrastructure;

namespace ExcelPasteTool.UITests.Tests;

/// <summary>全域快捷鍵：Ctrl+,（macOS ⌘+,）開設定頁、Alt+Shift+P 追加貼上。</summary>
public class ShortcutTests
{
    [AvaloniaFact]
    public void CtrlComma_OpensSettings()
    {
        using var host = MainWindowHost.Open();
        var modifier = OperatingSystem.IsMacOS() ? RawInputModifiers.Meta : RawInputModifiers.Control;
        host.Window.KeyPress(Key.OemComma, modifier, PhysicalKey.Comma, ",");
        MainWindowHost.Pump();
        Assert.Equal(SidebarPage.Settings, host.Window.Sidebar.SelectedPage);
    }

    [AvaloniaFact]
    public async Task AltShiftP_AppendsClipboard_WithoutReplacing()
    {
        using var host = MainWindowHost.Open();
        var view = (DataToolView)host.Find<ContentControl>("MainContent").Content!;
        host.TypeInto(host.Find<TextBox>("TopBox"), "A01");
        await host.Window.Clipboard!.SetTextAsync("B01\r\nA01");

        host.Window.KeyPress(Key.P, RawInputModifiers.Alt | RawInputModifiers.Shift, PhysicalKey.P, "P");
        for (var i = 0; i < 5; i++) { await Task.Delay(20); MainWindowHost.Pump(); }

        Assert.Equal(new[] { "A01", "B01" }, view.DataItems.Select(x => x.Value));
    }
}
