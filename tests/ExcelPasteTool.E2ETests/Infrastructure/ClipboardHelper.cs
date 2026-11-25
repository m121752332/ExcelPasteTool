using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ExcelPasteTool.E2ETests.Infrastructure;

/// <summary>
/// 讀寫 Windows 剪貼簿。剪貼簿 API 需要 STA 執行緒，xUnit 的執行緒是 MTA，所以另開一條；
/// 剪貼簿偶爾會被其他程式佔用，失敗時重試幾次。
/// </summary>
public static class ClipboardHelper
{
    public static string? GetText() => RunSta(() => Clipboard.ContainsText() ? Clipboard.GetText() : null);

    public static void SetText(string text) => RunSta(() =>
    {
        Clipboard.SetText(text);
        return true;
    });

    public static void Clear() => RunSta(() =>
    {
        Clipboard.Clear();
        return true;
    });

    private static T RunSta<T>(Func<T> action)
    {
        T result = default!;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    result = action();
                    error = null;
                    return;
                }
                catch (ExternalException ex) when (attempt < 10)
                {
                    error = ex;
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    error = ex;
                    return;
                }
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null) throw new InvalidOperationException("操作剪貼簿失敗", error);
        return result;
    }
}
