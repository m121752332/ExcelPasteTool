using Avalonia.Controls;

namespace ExcelPasteTool;

/// <summary>
/// 開發者模式：開啟後在主視窗按 F12 會開啟內建的「開發者工具」視窗（視覺樹、屬性、在畫面上標出選取的控制項）。
/// <para>
/// Avalonia 12 起舊的 Avalonia.Diagnostics（App 內的 DevTools）已不支援，官方新版改成要另外安裝並登入帳號的外部工具，
/// 所以這裡用 <see cref="DevToolsWindow"/> 自己做一個簡易版，不需要額外套件，Release 發佈版也能用。
/// 開發者模式關閉時不能開啟，已開著的視窗也會一併關閉。
/// </para>
/// </summary>
public static class DeveloperModeService
{
    /// <summary>目前是否為開發者模式（設定頁開關，存在設定檔）。</summary>
    public static bool IsEnabled => ConfigServices.Config.DeveloperMode;

    /// <summary>目前開著的開發者工具視窗（沒有開時為 null）。</summary>
    public static DevToolsWindow? Current { get; private set; }

    /// <summary>
    /// 開啟（或叫到最前面）檢視 <paramref name="target"/> 的開發者工具視窗。
    /// 開發者模式關閉時不做任何事並回傳 null。
    /// </summary>
    public static DevToolsWindow? Open(Window target)
    {
        if (!IsEnabled) return null;

        if (Current is { } existing)
        {
            if (existing.Target == target)
            {
                existing.Activate();
                return existing;
            }
            existing.Close();
        }

        var window = new DevToolsWindow(target);
        window.Closed += (_, _) =>
        {
            if (Current == window) Current = null;
        };
        Current = window;
        window.Show(target);
        return window;
    }

    /// <summary>設定頁切換開關時呼叫：關閉開發者模式時，已開著的開發者工具視窗一併關閉。</summary>
    public static void Apply(bool enabled)
    {
        if (!enabled)
            Current?.Close();
    }
}
