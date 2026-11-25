using System.Diagnostics;
using Avalonia.Threading;

namespace ExcelPasteTool;

/// <summary>
/// 端到端 UI 測試模式（tests/ExcelPasteTool.E2ETests 用 FlaUI 啟動真實執行檔時帶 <c>--ui-test</c>）。
///
/// 開啟後：
/// - 不檢查 GitHub 更新、不自動播放導覽（避免對話框蓋住畫面、點不到按鈕）。
/// - 若環境變數 <c>DATASPLITTER_UI_TEST_LOG</c> 指定了檔案路徑，會把 Avalonia 的 Trace 記錄
///   與未處理的例外寫進那個檔案，讓 AI 能直接讀到程式內部發生了什麼事。
///
/// 一般使用者執行時不會帶這個參數，行為完全不變。
/// </summary>
public static class UiTestMode
{
    public const string Argument = "--ui-test";
    public const string LogEnvironmentVariable = "DATASPLITTER_UI_TEST_LOG";

    public static bool IsEnabled { get; private set; }

    private static readonly object LogLock = new();
    private static string? _logPath;

    public static void EnableIfRequested(string[] args)
    {
        if (!args.Contains(Argument)) return;
        IsEnabled = true;
        MainWindow.SkipStartupChecks = true;

        var logPath = Environment.GetEnvironmentVariable(LogEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(logPath)) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(logPath))!);
            _logPath = logPath;
            Trace.AutoFlush = true;
            Trace.Listeners.Add(new LogFileTraceListener());
            Write("INFO", $"UI 測試模式啟動，版本 {UpdateService.CurrentVersionText}，PID {Environment.ProcessId}");

            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                Write("FATAL", $"AppDomain 未處理例外（IsTerminating={e.IsTerminating}）：{e.ExceptionObject}");
            TaskScheduler.UnobservedTaskException += (_, e) =>
                Write("ERROR", $"未觀察的 Task 例外：{e.Exception}");
            // 只記錄，不改變 Handled：例外照原本的方式處理
            Dispatcher.UIThread.UnhandledException += (_, e) =>
                Write("ERROR", $"UI 執行緒未處理例外：{e.Exception}");
        }
        catch
        {
            // 記錄檔開不起來不該影響程式本身
            _logPath = null;
        }
    }

    public static void Write(string level, string message)
    {
        var path = _logPath;
        if (path == null) return;
        try
        {
            lock (LogLock)
                File.AppendAllText(path, $"{DateTime.Now:HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
        }
        catch
        {
            // 忽略：記錄失敗不影響程式
        }
    }

    private sealed class LogFileTraceListener : TraceListener
    {
        public override void Write(string? message) => UiTestMode.Write("TRACE", message ?? "");
        public override void WriteLine(string? message) => UiTestMode.Write("TRACE", message ?? "");
    }
}
