using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.UIA3;
using Application = FlaUI.Core.Application;

namespace ExcelPasteTool.E2ETests.Infrastructure;

/// <summary>
/// 一次端到端測試的執行環境：準備隔離的程式資料夾 → 以 <c>--ui-test</c> 啟動真實 exe →
/// 提供找控制項、點擊、讀值、截圖、傾印控制項樹的方法，並把每一步寫進記錄檔。
///
/// 產出物（每個測試一個資料夾，例如 TestResults/e2e/Launch_PasteData_FillsTable/）：
/// <code>
/// steps.log            每一步的開始 / 結束 / 耗時 / 錯誤（人看）
/// summary.json         同上的結構化版本（AI 讀這個）
/// screenshots/NN-*.png 依序編號的截圖（失敗時自動加一張 failure）
/// trees/NN-*.txt       當下的 UI Automation 控制項樹（AutomationId、Name、類型、位置）
/// app.log              程式本身的記錄：Avalonia Trace、未處理例外（--ui-test 模式才有）
/// exception.txt        失敗時的完整例外
/// </code>
/// </summary>
public sealed class AppSession : IDisposable
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _testName;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DateTimeOffset _startedAt = DateTimeOffset.Now;
    private readonly List<StepRecord> _steps = new();
    private readonly List<string> _screenshots = new();
    private readonly List<string> _trees = new();
    private readonly string? _originalClipboard;
    private int _artifactCounter;
    private bool _completed;
    private string? _failure;

    private UIA3Automation? _automation;
    private Application? _app;
    private Process? _process;
    private Window? _window;

    public string OutputDir { get; }
    public string AppLogPath => Path.Combine(OutputDir, "app.log");
    public Window MainWindow => _window ?? throw new InvalidOperationException("主視窗還沒開啟");

    private AppSession(string testName)
    {
        _testName = testName;
        OutputDir = Path.Combine(E2EPaths.ResultsDir, testName);
        if (Directory.Exists(OutputDir)) Directory.Delete(OutputDir, recursive: true);
        Directory.CreateDirectory(Path.Combine(OutputDir, "screenshots"));
        Directory.CreateDirectory(Path.Combine(OutputDir, "trees"));
        Log($"測試：{testName}");
        Log($"產出物：{OutputDir}");

        try { _originalClipboard = ClipboardHelper.GetText(); }
        catch { _originalClipboard = null; }
    }

    /// <summary>準備程式資料夾並啟動 DataSplitter.exe，等主視窗出現。</summary>
    public static AppSession Start(string testName, string theme = "Light", string language = "TraditionalChinese")
    {
        var session = new AppSession(testName);
        try
        {
            session.Step("準備隔離的程式資料夾", () => session.PrepareSandbox(theme, language));
            session.Step("啟動 DataSplitter.exe --ui-test", session.Launch);
        }
        catch
        {
            session.Dispose();
            throw;
        }
        return session;
    }

    // ===== 步驟與記錄 =====

    /// <summary>執行一個步驟：記錄開始、結束與耗時；失敗時自動截圖、傾印控制項樹、寫下例外，再往外丟。</summary>
    public void Step(string name, Action action) => Step<object?>(name, () =>
    {
        action();
        return null;
    });

    public T Step<T>(string name, Func<T> action)
    {
        var record = new StepRecord { Index = _steps.Count + 1, Name = name };
        _steps.Add(record);
        var sw = Stopwatch.StartNew();
        Log($"▶ [{record.Index}] {name}");
        try
        {
            var result = action();
            record.Status = "passed";
            record.DurationMs = sw.ElapsedMilliseconds;
            Log($"✔ [{record.Index}] {name}（{record.DurationMs} ms）");
            return result;
        }
        catch (Exception ex)
        {
            record.Status = "failed";
            record.DurationMs = sw.ElapsedMilliseconds;
            record.Error = ex.Message;
            Log($"✖ [{record.Index}] {name}（{record.DurationMs} ms）：{ex.GetType().Name}: {ex.Message}");
            RecordFailure(ex);
            throw;
        }
    }

    public void Log(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} +{_clock.ElapsedMilliseconds,6} ms  {message}";
        File.AppendAllText(Path.Combine(OutputDir, "steps.log"), line + Environment.NewLine, Utf8NoBom);
        Console.WriteLine(line);
    }

    /// <summary>測試最後呼叫，代表全部步驟成功；沒呼叫就結束視為失敗。</summary>
    public void Complete() => _completed = true;

    // ===== 控制項 =====

    /// <summary>依 AutomationId 找控制項，等到出現為止（預設 15 秒）。</summary>
    public AutomationElement Find(string automationId, TimeSpan? timeout = null)
    {
        var found = WaitFor(() => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            timeout, $"找不到 AutomationId=\"{automationId}\" 的控制項");
        return found!;
    }

    /// <summary>點按鈕：優先用 UIA 的 Invoke（不需要搶前景、不受滑鼠位置影響），不支援時才模擬滑鼠點擊。</summary>
    public void Invoke(string automationId)
    {
        var element = Find(automationId);
        if (!element.IsEnabled) throw new InvalidOperationException($"\"{automationId}\" 目前是停用狀態");
        if (element.Patterns.Invoke.TryGetPattern(out var invoke))
        {
            invoke.Invoke();
            Log($"   Invoke \"{automationId}\"");
        }
        else
        {
            MainWindow.SetForeground();
            element.Click(moveMouse: true);
            Log($"   滑鼠點擊 \"{automationId}\"（不支援 Invoke）");
        }
    }

    /// <summary>讀文字：TextBox 讀 Value，其他控制項讀 Name。</summary>
    public string ReadText(string automationId)
    {
        var element = Find(automationId);
        if (element.Patterns.Value.TryGetPattern(out var value))
            return value.Value.ValueOrDefault ?? "";
        return element.Properties.Name.ValueOrDefault ?? "";
    }

    /// <summary>等到某個控制項的文字符合條件，回傳最後讀到的文字。</summary>
    public string WaitForText(string automationId, Func<string, bool> predicate, TimeSpan? timeout = null)
    {
        var last = "";
        var ok = WaitFor(() =>
        {
            last = ReadText(automationId);
            return predicate(last) ? last : null;
        }, timeout, null);
        if (ok == null)
            throw new TimeoutException($"等待 \"{automationId}\" 的文字逾時，最後讀到：\"{Shorten(last)}\"");
        Log($"   \"{automationId}\" = \"{Shorten(last)}\"");
        return last;
    }

    /// <summary>同時按下一組鍵（例如 Alt+F5）。會先把主視窗拉到前景，按鍵才送得到程式。</summary>
    public void PressKeys(params FlaUI.Core.WindowsAPI.VirtualKeyShort[] keys)
    {
        MainWindow.SetForeground();
        FlaUI.Core.Input.Keyboard.TypeSimultaneously(keys);
        Log($"   按鍵 {string.Join("+", keys)}");
    }

    // ===== 剪貼簿 =====

    public void SetClipboard(string text)
    {
        ClipboardHelper.SetText(text);
        Log($"   剪貼簿 ← \"{Shorten(text)}\"");
    }

    // ===== 截圖與控制項樹 =====

    /// <summary>截主視窗（先拉到前景，避免被其他視窗擋住）。回傳檔案路徑。</summary>
    public string Screenshot(string name)
    {
        var path = Path.Combine(OutputDir, "screenshots", $"{NextArtifactNumber():00}-{name}.png");
        try
        {
            if (_window != null)
            {
                try { _window.SetForeground(); } catch { /* 前景切不過去還是照截 */ }
                Thread.Sleep(150);
                Capture.Element(_window).ToFile(path);
            }
            else
            {
                Capture.Screen().ToFile(path);
            }
            _screenshots.Add(Relative(path));
            Log($"   截圖 {Relative(path)}");
        }
        catch (Exception ex)
        {
            Log($"   截圖失敗：{ex.Message}");
        }
        return path;
    }

    /// <summary>把目前的 UI Automation 控制項樹寫成文字檔（AI 用來找 AutomationId、看哪些東西有出現）。</summary>
    public string DumpTree(string name)
    {
        var path = Path.Combine(OutputDir, "trees", $"{NextArtifactNumber():00}-{name}.txt");
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 格式：控制項類型 #AutomationId \"Name\" (ClassName) [x,y 寬x高] {狀態}");
            if (_window == null || _automation == null)
            {
                sb.AppendLine("（主視窗尚未開啟）");
            }
            else
            {
                var count = 0;
                var walker = _automation.TreeWalkerFactory.GetControlViewWalker();
                WriteNode(sb, walker, _window, 0, ref count);
                sb.AppendLine($"# 共 {count} 個節點");
            }
            File.WriteAllText(path, sb.ToString(), Utf8NoBom);
            _trees.Add(Relative(path));
            Log($"   控制項樹 {Relative(path)}");
        }
        catch (Exception ex)
        {
            Log($"   傾印控制項樹失敗：{ex.Message}");
        }
        return path;
    }

    private static void WriteNode(StringBuilder sb, FlaUI.Core.ITreeWalker walker, AutomationElement element, int depth, ref int count)
    {
        const int maxDepth = 40, maxNodes = 3000;
        if (depth > maxDepth || count >= maxNodes) return;
        count++;

        var p = element.Properties;
        var type = Safe(() => p.ControlType.ValueOrDefault.ToString(), "?");
        var id = Safe(() => p.AutomationId.ValueOrDefault, "");
        var name = Safe(() => p.Name.ValueOrDefault, "");
        var cls = Safe(() => p.ClassName.ValueOrDefault, "");
        var rect = Safe(() =>
        {
            var r = p.BoundingRectangle.ValueOrDefault;
            return $"{r.X},{r.Y} {r.Width}x{r.Height}";
        }, "?");
        var flags = new List<string>();
        if (!Safe(() => p.IsEnabled.ValueOrDefault, true)) flags.Add("停用");
        if (Safe(() => p.IsOffscreen.ValueOrDefault, false)) flags.Add("畫面外");
        var value = element.Patterns.Value.TryGetPattern(out var vp) ? Safe(() => vp.Value.ValueOrDefault, "") : null;

        sb.Append(new string(' ', depth * 2)).Append(type);
        if (!string.IsNullOrEmpty(id)) sb.Append(" #").Append(id);
        if (!string.IsNullOrEmpty(name)) sb.Append(" \"").Append(Shorten(name)).Append('"');
        if (!string.IsNullOrEmpty(cls)) sb.Append(" (").Append(cls).Append(')');
        sb.Append(" [").Append(rect).Append(']');
        if (value != null) sb.Append(" value=\"").Append(Shorten(value)).Append('"');
        if (flags.Count > 0) sb.Append(" {").Append(string.Join(",", flags)).Append('}');
        sb.AppendLine();

        var child = Safe<AutomationElement?>(() => walker.GetFirstChild(element), null);
        while (child != null && count < maxNodes)
        {
            WriteNode(sb, walker, child, depth + 1, ref count);
            var current = child;
            child = Safe<AutomationElement?>(() => walker.GetNextSibling(current), null);
        }
    }

    // ===== 啟動 / 結束 =====

    private string SandboxDir => Path.Combine(E2EPaths.ResultsDir, "_app");

    /// <summary>測試用的截圖資料夾（寫進 config.json 的 ScreenshotFolder，不會動到使用者的「圖片」資料夾）；每次啟動前清空。</summary>
    public string ScreenshotFolder => Path.Combine(SandboxDir, "Screenshots");

    /// <summary>
    /// 把建置好的程式複製到 TestResults/e2e/_app，並放一份固定的 config.json。
    /// 設定檔放在 exe 旁邊，所以這樣不會動到開發時 bin/ 底下或使用者自己的設定。
    /// </summary>
    private void PrepareSandbox(string theme, string language)
    {
        var exe = E2EPaths.AppExe;
        if (!File.Exists(exe))
            throw new FileNotFoundException($"找不到主程式，請先建置（dotnet build src/ExcelPasteTool）：{exe}", exe);
        var sourceDir = Path.GetDirectoryName(exe)!;
        Log($"   來源：{sourceDir}");

        KillLeftoverProcesses();
        Directory.CreateDirectory(SandboxDir);

        // runtimes/ 底下有各平台的原生函式庫（每個上百 MB），只需要本機這一個
        var rid = "win-" + RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
        var sandboxRuntimes = Path.Combine(SandboxDir, "runtimes");
        if (Directory.Exists(sandboxRuntimes))
        {
            foreach (var dir in Directory.GetDirectories(sandboxRuntimes))
                if (!Path.GetFileName(dir).Equals(rid, StringComparison.OrdinalIgnoreCase))
                    Directory.Delete(dir, recursive: true);
        }

        var copied = 0;
        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            if (relative.Equals("config.json", StringComparison.OrdinalIgnoreCase)) continue;
            var parts = relative.Split(Path.DirectorySeparatorChar);
            if (parts.Length > 2 && parts[0].Equals("runtimes", StringComparison.OrdinalIgnoreCase)
                && !parts[1].Equals(rid, StringComparison.OrdinalIgnoreCase))
                continue;
            var target = Path.Combine(SandboxDir, relative);
            var targetInfo = new FileInfo(target);
            var sourceInfo = new FileInfo(file);
            if (targetInfo.Exists && targetInfo.Length == sourceInfo.Length && targetInfo.LastWriteTimeUtc == sourceInfo.LastWriteTimeUtc)
                continue;
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
            copied++;
        }
        Log($"   複製 {copied} 個有變動的檔案到 {Relative(SandboxDir)}");

        // 屬性名稱要和 AppConfig 一樣（PascalCase），程式讀設定時大小寫有區分
        var config = new
        {
            Language = language,
            Theme = theme,
            TourCompleted = true,
            CompactTitleBar = false,
            ShowSqlPanel = true,
            ScreenshotFolder,
        };
        if (Directory.Exists(ScreenshotFolder)) Directory.Delete(ScreenshotFolder, recursive: true);
        File.WriteAllText(Path.Combine(SandboxDir, "config.json"), JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        Log($"   config.json：Theme={theme}, Language={language}, TourCompleted=true");
    }

    private void KillLeftoverProcesses()
    {
        foreach (var process in Process.GetProcessesByName("DataSplitter"))
        {
            try
            {
                var path = process.MainModule?.FileName;
                if (path != null && path.StartsWith(SandboxDir, StringComparison.OrdinalIgnoreCase))
                {
                    Log($"   結束上次殘留的測試程式 PID {process.Id}");
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
            }
            catch
            {
                // 沒權限讀別的程序：那就不是我們的
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private void Launch()
    {
        var exe = Path.Combine(SandboxDir, Path.GetFileName(E2EPaths.AppExe));
        var psi = new ProcessStartInfo(exe, "--ui-test")
        {
            WorkingDirectory = SandboxDir,
            UseShellExecute = false,
        };
        psi.Environment["DATASPLITTER_UI_TEST_LOG"] = AppLogPath;

        _automation = new UIA3Automation();
        // 自己啟動再交給 FlaUI：這樣結束時讀得到 ExitCode
        _process = Process.Start(psi) ?? throw new InvalidOperationException($"無法啟動 {exe}");
        _app = Application.Attach(_process.Id);
        Log($"   PID {_app.ProcessId}");

        _window = WaitFor(() =>
        {
            if (_app.HasExited) throw new InvalidOperationException($"程式啟動後就結束了（ExitCode={SafeExitCode()}），請看 app.log");
            return _app.GetMainWindow(_automation, TimeSpan.FromMilliseconds(500));
        }, TimeSpan.FromSeconds(30), "等不到主視窗（30 秒）");
        Log($"   主視窗：\"{_window!.Title}\"");
    }

    private T? WaitFor<T>(Func<T?> probe, TimeSpan? timeout, string? timeoutMessage) where T : class
    {
        var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
        while (true)
        {
            var result = probe();
            if (result != null) return result;
            if (_app is { HasExited: true })
                throw new InvalidOperationException($"程式已經結束（ExitCode={SafeExitCode()}），請看 app.log");
            if (DateTime.UtcNow > deadline)
            {
                if (timeoutMessage == null) return null;
                throw new TimeoutException(timeoutMessage);
            }
            Thread.Sleep(100);
        }
    }

    private string SafeExitCode()
    {
        try { return _process is { HasExited: true } ? _process.ExitCode.ToString() : "?（尚未結束）"; }
        catch (Exception ex) { return $"?（{ex.Message}）"; }
    }

    private void RecordFailure(Exception ex)
    {
        _failure ??= $"{ex.GetType().Name}: {ex.Message}";
        File.AppendAllText(Path.Combine(OutputDir, "exception.txt"), ex + Environment.NewLine + Environment.NewLine, Utf8NoBom);
        Screenshot("failure");
        DumpTree("failure");
    }

    public void Dispose()
    {
        if (_app != null)
        {
            try
            {
                if (!_app.HasExited)
                {
                    Log("關閉程式");
                    _app.Close();
                    if (!WaitForExit(TimeSpan.FromSeconds(5)))
                    {
                        Log("   5 秒內沒有結束，強制結束");
                        _app.Kill();
                    }
                }
                Log($"   ExitCode={SafeExitCode()}");
            }
            catch (Exception ex)
            {
                Log($"   關閉程式時發生例外：{ex.Message}");
            }
            _app.Dispose();
            _process?.Dispose();
        }
        _automation?.Dispose();

        try
        {
            if (_originalClipboard != null) ClipboardHelper.SetText(_originalClipboard);
        }
        catch
        {
            // 還原剪貼簿失敗不影響測試結果
        }

        WriteSummary();
    }

    private bool WaitForExit(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (_app!.HasExited) return true;
            Thread.Sleep(100);
        }
        return _app!.HasExited;
    }

    private void WriteSummary()
    {
        var appLogErrors = File.Exists(AppLogPath)
            ? File.ReadAllLines(AppLogPath).Where(l => l.Contains("[ERROR]") || l.Contains("[FATAL]")).ToList()
            : new List<string>();
        var status = _completed && _failure == null ? "passed" : "failed";
        var summary = new
        {
            test = _testName,
            status,
            error = _failure ?? (_completed ? null : "測試沒有跑完（中途例外，詳見 exception.txt / steps.log）"),
            startedAt = _startedAt.ToString("yyyy-MM-dd HH:mm:ss zzz"),
            durationMs = _clock.ElapsedMilliseconds,
            exe = E2EPaths.AppExe,
            outputDir = Relative(OutputDir),
            steps = _steps,
            screenshots = _screenshots,
            trees = _trees,
            appLog = File.Exists(AppLogPath) ? Relative(AppLogPath) : null,
            appLogErrors,
            exception = File.Exists(Path.Combine(OutputDir, "exception.txt")) ? Relative(Path.Combine(OutputDir, "exception.txt")) : null,
        };
        Log($"結果：{status}");
        File.WriteAllText(Path.Combine(OutputDir, "summary.json"), JsonSerializer.Serialize(summary, JsonOptions), Utf8NoBom);
    }

    // ===== 小工具 =====

    private int NextArtifactNumber() => Interlocked.Increment(ref _artifactCounter);

    private static string Relative(string path)
    {
        var full = Path.GetFullPath(path);
        var root = E2EPaths.RepoRoot;
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? full[root.Length..].TrimStart('\\', '/').Replace('\\', '/')
            : full;
    }

    private static string Shorten(string text)
    {
        var oneLine = text.Replace("\r", "\\r").Replace("\n", "\\n");
        return oneLine.Length <= 120 ? oneLine : oneLine[..120] + "…";
    }

    private static T Safe<T>(Func<T> get, T fallback)
    {
        try { return get(); }
        catch { return fallback; }
    }

    public sealed class StepRecord
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public string Status { get; set; } = "running";
        public long DurationMs { get; set; }
        public string? Error { get; set; }
    }
}
