using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExcelPasteTool;

/// <summary>
/// GitHub Releases 上的一個可用更新。
/// </summary>
public sealed class UpdateInfo
{
    public required Version Version { get; init; }
    public required string TagName { get; init; }
    public string ReleaseName { get; init; } = string.Empty;
    public string ReleaseNotes { get; init; } = string.Empty;
    public string ReleasePageUrl { get; init; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>適用目前作業系統的 .zip 更新檔；找不到時為 null（只能手動下載）。</summary>
    public string? AssetName { get; init; }
    public string? AssetDownloadUrl { get; init; }
    public long AssetSize { get; init; }

    public bool CanAutoUpdate => AssetDownloadUrl != null;
}

/// <summary>
/// 下載進度回報。
/// </summary>
public readonly record struct DownloadProgress(long BytesReceived, long TotalBytes, double BytesPerSecond)
{
    public double Percent => TotalBytes > 0 ? BytesReceived * 100d / TotalBytes : 0;
}

/// <summary>
/// 版本檢查與自動更新服務。
///
/// 流程：
///   1. CheckForUpdateAsync()   向 GitHub API 取得最新 Release，與目前版本比較
///   2. DownloadAsync()         下載對應平台的 zip 檔（含進度）
///   3. PrepareUpdate()         解壓到暫存資料夾並驗證內容
///   4. ApplyAndRestart()       產生更新腳本 → 關閉程式 → 腳本等程式結束後覆蓋檔案 → 以 --updated 參數重新啟動
///
/// Release 規範（打 tag 推版時請遵守）：
///   - tag 名稱可用 "1.1.0" 或 "v1.1.0"
///   - 附件須為 .zip，檔名包含平台與架構，例如：
///       DataSplitter_win_x64.zip / DataSplitter_linux_x64.zip / DataSplitter_osx_arm64.zip
///   - zip 內可以直接放檔案，或只包一層資料夾
/// </summary>
public static class UpdateService
{
    public const string RepoOwner = "m121752332";
    public const string RepoName = "ExcelPasteTool";

    /// <summary>重新啟動時帶入的參數，用來顯示「更新完成」。</summary>
    public const string UpdatedArgument = "--updated";
    /// <summary>更新腳本覆蓋失敗時帶入的參數。</summary>
    public const string UpdateFailedArgument = "--update-failed";

    private static readonly string LatestReleaseApi =
        $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";

    public static string ReleasesPageUrl => $"https://github.com/{RepoOwner}/{RepoName}/releases";

    /// <summary>不在使用者設定裡被更新覆蓋的檔案。</summary>
    private static readonly string[] PreservedFiles = { "config.json" };

    /// <summary>暫存資料夾（下載與解壓用）。</summary>
    public static string WorkDirectory => Path.Combine(Path.GetTempPath(), "DataSplitter_update");

    /// <summary>程式所在資料夾（要被覆蓋的位置）。</summary>
    public static string AppDirectory => AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    /// <summary>目前執行檔的完整路徑。</summary>
    public static string ExecutablePath =>
        Environment.ProcessPath ?? Path.Combine(AppDirectory, Global.AssemblyName + (OperatingSystem.IsWindows() ? ".exe" : string.Empty));

    // ------------------------------------------------------------------
    // 版本
    // ------------------------------------------------------------------

    /// <summary>目前程式版本（取自 InformationalVersion，例如 1.1.0）。</summary>
    public static Version CurrentVersion { get; } = ResolveCurrentVersion();

    public static string CurrentVersionText => CurrentVersion.ToString(3);

    // 必須宣告在 CurrentVersion 之後：靜態欄位依宣告順序初始化
    private static readonly Lazy<HttpClient> Http = new(() =>
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DataSplitter-Updater", CurrentVersionText));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    });

    private static Version ResolveCurrentVersion()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (TryParseVersion(info, out var v))
            return v;
        return Normalize(asm.GetName().Version ?? new Version(1, 0, 0));
    }

    /// <summary>
    /// 解析 "v1.1.0"、"1.1.0"、"1.1.0-beta"、"1.1.0+abc123" 這類字串。
    /// </summary>
    public static bool TryParseVersion(string? text, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(text)) return false;

        var s = text.Trim();
        if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            s = s[1..];
        var cut = s.IndexOfAny(new[] { '-', '+', ' ' });
        if (cut >= 0) s = s[..cut];

        // 只有一段數字（例如 "2"）時補成 "2.0"
        if (!s.Contains('.')) s += ".0";

        if (!Version.TryParse(s, out var parsed)) return false;
        version = Normalize(parsed);
        return true;
    }

    /// <summary>補齊四段，避免 1.1.0 與 1.1.0.0 比較時被視為不同。</summary>
    private static Version Normalize(Version v) =>
        new(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));

    // ------------------------------------------------------------------
    // 1. 檢查更新
    // ------------------------------------------------------------------

    /// <summary>
    /// 檢查 GitHub 上是否有比目前更新的正式版 Release。沒有新版時回傳 null。
    /// 網路錯誤等例外會往上拋，由呼叫端決定要不要顯示。
    /// </summary>
    public static async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        using var response = await Http.Value.GetAsync(LatestReleaseApi, ct).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null; // 還沒有任何正式 Release
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: ct).ConfigureAwait(false);
        if (release == null || release.Draft || release.Prerelease)
            return null;

        if (!TryParseVersion(release.TagName, out var latest) || latest <= CurrentVersion)
            return null;

        var asset = SelectAsset(release.Assets);
        return new UpdateInfo
        {
            Version = latest,
            TagName = release.TagName ?? latest.ToString(3),
            ReleaseName = release.Name ?? string.Empty,
            ReleaseNotes = release.Body ?? string.Empty,
            ReleasePageUrl = release.HtmlUrl ?? ReleasesPageUrl,
            PublishedAt = release.PublishedAt,
            AssetName = asset?.Name,
            AssetDownloadUrl = asset?.BrowserDownloadUrl,
            AssetSize = asset?.Size ?? 0,
        };
    }

    /// <summary>
    /// 依目前作業系統與 CPU 架構挑選 zip 附件。
    /// </summary>
    internal static GitHubAsset? SelectAsset(IEnumerable<GitHubAsset>? assets)
    {
        if (assets == null) return null;
        var zips = assets
            .Where(a => a.Name != null && a.BrowserDownloadUrl != null &&
                        a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (zips.Count == 0) return null;

        var arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => "arm64",
            Architecture.X86 => "x86",
            _ => "x64",
        };

        string[] osTokens;
        if (OperatingSystem.IsWindows()) osTokens = new[] { "win", "windows" };
        else if (OperatingSystem.IsMacOS()) osTokens = new[] { "osx", "mac", "macos" };
        else osTokens = new[] { "linux" };

        string[] otherOsTokens = new[] { "win", "windows", "osx", "mac", "macos", "linux" }
            .Except(osTokens).ToArray();

        static string[] Tokens(string name) =>
            Path.GetFileNameWithoutExtension(name).ToLowerInvariant()
                .Split(new[] { '_', '-', '.', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // 第一優先：同時符合平台與架構
        var exact = zips.FirstOrDefault(a =>
        {
            var t = Tokens(a.Name!);
            return t.Any(osTokens.Contains) && t.Contains(arch);
        });
        if (exact != null) return exact;

        // 第二優先：符合平台（檔名沒有標架構）
        var osOnly = zips.FirstOrDefault(a =>
        {
            var t = Tokens(a.Name!);
            return t.Any(osTokens.Contains) && !t.Any(x => x is "x64" or "x86" or "arm64");
        });
        if (osOnly != null) return osOnly;

        // Windows 相容舊命名：DataSplitter_x64.zip（沒有標平台）
        if (OperatingSystem.IsWindows())
        {
            return zips.FirstOrDefault(a =>
            {
                var t = Tokens(a.Name!);
                return !t.Any(otherOsTokens.Contains) && t.Contains(arch);
            });
        }
        return null;
    }

    // ------------------------------------------------------------------
    // 2. 下載
    // ------------------------------------------------------------------

    /// <summary>下載更新檔，回傳本機 zip 路徑。</summary>
    public static async Task<string> DownloadAsync(UpdateInfo info, IProgress<DownloadProgress>? progress, CancellationToken ct = default)
    {
        if (!info.CanAutoUpdate)
            throw new InvalidOperationException("此版本沒有適用於目前平台的更新檔。");

        Directory.CreateDirectory(WorkDirectory);
        var zipPath = Path.Combine(WorkDirectory, $"update_{info.Version.ToString(3)}.zip");
        var partPath = zipPath + ".part";

        using var response = await Http.Value.GetAsync(info.AssetDownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? info.AssetSize;

        await using (var src = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
        await using (var dst = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            var buffer = new byte[81920];
            long received = 0;
            var sw = Stopwatch.StartNew();
            var lastReport = TimeSpan.Zero;
            int read;
            while ((read = await src.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
            {
                await dst.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                received += read;
                if (sw.Elapsed - lastReport > TimeSpan.FromMilliseconds(100) || received == total)
                {
                    lastReport = sw.Elapsed;
                    var speed = received / Math.Max(sw.Elapsed.TotalSeconds, 0.001);
                    progress?.Report(new DownloadProgress(received, total, speed));
                }
            }
            progress?.Report(new DownloadProgress(received, total > 0 ? total : received, 0));

            if (total > 0 && received != total)
                throw new IOException($"下載不完整（{received}/{total} bytes），請重試。");
        }

        File.Move(partPath, zipPath, overwrite: true);
        return zipPath;
    }

    // ------------------------------------------------------------------
    // 3. 解壓與驗證
    // ------------------------------------------------------------------

    /// <summary>
    /// 解壓 zip 到暫存資料夾，回傳「新版檔案根目錄」。
    /// 會確認裡面有同名執行檔，並移除會覆蓋使用者設定的檔案。
    /// </summary>
    public static string PrepareUpdate(string zipPath)
    {
        EnsureAppDirectoryWritable();

        var stagingDir = Path.Combine(WorkDirectory, "staging");
        if (Directory.Exists(stagingDir))
            Directory.Delete(stagingDir, recursive: true);
        Directory.CreateDirectory(stagingDir);

        ZipFile.ExtractToDirectory(zipPath, stagingDir, overwriteFiles: true);

        // zip 裡若只包了一層資料夾，就以該資料夾為根
        var root = stagingDir;
        var entries = Directory.GetFileSystemEntries(root);
        if (entries.Length == 1 && Directory.Exists(entries[0]) && !entries[0].EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            root = entries[0];

        var exeName = Path.GetFileName(ExecutablePath);
        if (!File.Exists(Path.Combine(root, exeName)))
            throw new InvalidDataException($"更新檔內找不到 {exeName}，可能下載到錯誤平台的檔案。");

        // 保留使用者設定
        foreach (var name in PreservedFiles)
        {
            var newFile = Path.Combine(root, name);
            if (File.Exists(newFile) && File.Exists(Path.Combine(AppDirectory, name)))
                File.Delete(newFile);
        }

        return root;
    }

    private static void EnsureAppDirectoryWritable()
    {
        try
        {
            var probe = Path.Combine(AppDirectory, $".write_test_{Guid.NewGuid():N}");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException(
                $"程式所在資料夾沒有寫入權限，無法自動更新：\n{AppDirectory}\n請以系統管理員身分執行或改為手動下載。", ex);
        }
    }

    // ------------------------------------------------------------------
    // 4. 套用並重新啟動
    // ------------------------------------------------------------------

    /// <summary>
    /// 產生並啟動更新腳本。呼叫成功後，呼叫端應立即關閉程式；
    /// 腳本會等目前程式結束 → 覆蓋檔案 → 以 --updated 重新啟動。
    /// </summary>
    public static void LaunchUpdater(string newFilesRoot)
    {
        var pid = Environment.ProcessId;
        var logPath = Path.Combine(WorkDirectory, "update.log");

        if (OperatingSystem.IsWindows())
        {
            var script = Path.Combine(WorkDirectory, "apply_update.ps1");
            // PowerShell 5.1 需要 UTF-8 BOM 才能正確讀取中文路徑
            File.WriteAllText(script, BuildPowerShellScript(pid, newFilesRoot, logPath), new UTF8Encoding(true));
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = WorkDirectory,
            });
        }
        else
        {
            var script = Path.Combine(WorkDirectory, "apply_update.sh");
            File.WriteAllText(script, BuildShellScript(pid, newFilesRoot, logPath).Replace("\r\n", "\n"), new UTF8Encoding(false));
            File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            Process.Start(new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { script },
                UseShellExecute = false,
                WorkingDirectory = WorkDirectory,
            });
        }
    }

    private static string PsQuote(string s) => "'" + s.Replace("'", "''") + "'";
    private static string ShQuote(string s) => "'" + s.Replace("'", "'\\''") + "'";

    private static string BuildPowerShellScript(int pid, string source, string logPath)
    {
        var workingDir = Directory.GetCurrentDirectory();
        return $@"$ErrorActionPreference = 'Stop'
$pidToWait = {pid}
$source  = {PsQuote(source)}
$target  = {PsQuote(AppDirectory)}
$exe     = {PsQuote(ExecutablePath)}
$workDir = {PsQuote(workingDir)}
$log     = {PsQuote(logPath)}

function Log($m) {{ Add-Content -Path $log -Value (""[{{0}}] {{1}}"" -f (Get-Date -Format s), $m) -Encoding UTF8 }}

try {{
    Log 'waiting for app to exit'
    try {{ Wait-Process -Id $pidToWait -Timeout 60 -ErrorAction SilentlyContinue }} catch {{}}
    Start-Sleep -Milliseconds 500

    # robocopy 會自動重試被鎖住的檔案；結束碼 0~7 代表成功
    & robocopy $source $target /E /R:20 /W:1 /NP /NFL /NDL /NJH /NJS | Out-Null
    if ($LASTEXITCODE -ge 8) {{ throw ""robocopy 失敗，結束碼 $LASTEXITCODE"" }}
    Log 'update applied'
    Start-Process -FilePath $exe -ArgumentList '{UpdatedArgument}' -WorkingDirectory $workDir
}}
catch {{
    Log (""update failed: "" + $_)
    Start-Process -FilePath $exe -ArgumentList '{UpdateFailedArgument}' -WorkingDirectory $workDir
}}
";
    }

    private static string BuildShellScript(int pid, string source, string logPath)
    {
        var workingDir = Directory.GetCurrentDirectory();
        return $@"#!/bin/sh
PID={pid}
SRC={ShQuote(source)}
DST={ShQuote(AppDirectory)}
EXE={ShQuote(ExecutablePath)}
WORKDIR={ShQuote(workingDir)}
LOG={ShQuote(logPath)}

log() {{ echo ""[$(date '+%Y-%m-%dT%H:%M:%S')] $1"" >> ""$LOG""; }}

log 'waiting for app to exit'
i=0
while kill -0 ""$PID"" 2>/dev/null && [ $i -lt 120 ]; do
  sleep 0.5
  i=$((i+1))
done

if cp -Rf ""$SRC""/. ""$DST""/ >> ""$LOG"" 2>&1; then
  chmod +x ""$EXE"" 2>/dev/null
  log 'update applied'
  ARG='{UpdatedArgument}'
else
  log 'update failed'
  ARG='{UpdateFailedArgument}'
fi

cd ""$WORKDIR"" || cd ""$DST""
nohup ""$EXE"" ""$ARG"" >/dev/null 2>&1 &
";
    }

    /// <summary>以預設瀏覽器開啟網址（Release 頁面）。</summary>
    public static void OpenUrl(string url)
    {
        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", url);
            else
                Process.Start("xdg-open", url);
        }
        catch { }
    }

    /// <summary>刪除上次更新留下的暫存檔。</summary>
    public static void CleanupWorkDirectory()
    {
        try
        {
            if (Directory.Exists(WorkDirectory))
                Directory.Delete(WorkDirectory, recursive: true);
        }
        catch { }
    }

    // ------------------------------------------------------------------
    // GitHub API 資料結構
    // ------------------------------------------------------------------

    internal sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string? TagName { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("html_url")] public string? HtmlUrl { get; set; }
        [JsonPropertyName("draft")] public bool Draft { get; set; }
        [JsonPropertyName("prerelease")] public bool Prerelease { get; set; }
        [JsonPropertyName("published_at")] public DateTimeOffset? PublishedAt { get; set; }
        [JsonPropertyName("assets")] public List<GitHubAsset>? Assets { get; set; }
    }

    internal sealed class GitHubAsset
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("browser_download_url")] public string? BrowserDownloadUrl { get; set; }
        [JsonPropertyName("size")] public long Size { get; set; }
    }
}
