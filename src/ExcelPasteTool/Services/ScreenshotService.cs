using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;

namespace ExcelPasteTool;

/// <summary>一張截圖檔的資訊（截圖管理頁的清單項目）。</summary>
public sealed record ScreenshotFile(string FilePath, DateTime ModifiedAt, long SizeBytes)
{
    public string FileName => Path.GetFileName(FilePath);
    public string ModifiedText => ModifiedAt.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
    public string SizeText => ScreenshotService.FormatSize(SizeBytes);
}

/// <summary>
/// 一個月份資料夾（例如 202611）裡的截圖。Key 為 null 代表截圖資料夾根目錄裡、還沒分月份的舊截圖（「未分類」）。
/// </summary>
public sealed record ScreenshotMonth(string? Key, string FolderPath, IReadOnlyList<ScreenshotFile> Files)
{
    public bool IsUnsorted => Key == null;
    public int Count => Files.Count;
    public long TotalBytes => Files.Sum(f => f.SizeBytes);
    public string TotalSizeText => ScreenshotService.FormatSize(TotalBytes);
}

/// <summary>
/// App 內截圖（Alt+F5，macOS 是 ⌥F5）：
/// 用 Avalonia 的 RenderTargetBitmap 把主視窗的內容重新繪製到一張點陣圖，再存成 PNG。
/// 只拍 App 本身（不含系統標題列、其他視窗與桌面），跨平台都能用，也不需要螢幕錄製權限。
///
/// 存放位置：預設是「圖片」資料夾底下的 DataSplitter（Windows：%USERPROFILE%\Pictures\DataSplitter，
/// macOS / Linux：~/Pictures/DataSplitter），可在 設定 › 一般設置 › 截圖資料夾 變更。
/// 依年月分資料夾：截圖存在「截圖資料夾\yyyyMM\」（例如 202611），打開資料夾就知道那個月拍了幾張，
/// 整個月份資料夾刪掉就是刪除那個月的截圖。截圖資料夾根目錄裡的 PNG（分月份之前拍的）列為「未分類」，不會自動搬動。
/// 檔名：DataSplitter_yyyyMMdd_HHmmss.png，同一秒拍多張時加上 _2、_3。
/// </summary>
public static class ScreenshotService
{
    public const string FilePrefix = "DataSplitter_";
    public const string FileExtension = ".png";

    /// <summary>截圖新增或刪除時通知（截圖管理頁用來重新整理清單）。</summary>
    public static event Action? Changed;

    /// <summary>測試用：取代預設資料夾，避免 UI 測試把檔案寫到使用者的「圖片」資料夾。</summary>
    internal static string? DefaultFolderOverride { get; set; }

    /// <summary>測試用：畫面上顯示的資料夾路徑（固定文字，基準圖不會因為電腦的使用者名稱不同而改變）。</summary>
    internal static string? FolderDisplayOverride { get; set; }

    /// <summary>預設資料夾：「圖片」資料夾底下的 DataSplitter；系統找不到「圖片」資料夾時，退回使用者資料夾下的 Pictures。</summary>
    public static string DefaultFolder
    {
        get
        {
            if (!string.IsNullOrEmpty(DefaultFolderOverride)) return DefaultFolderOverride;
            var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (string.IsNullOrEmpty(pictures))
                pictures = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Pictures");
            return Path.Combine(pictures, "DataSplitter");
        }
    }

    /// <summary>目前使用的截圖資料夾（設定沒有指定時用預設資料夾）。</summary>
    public static string Folder =>
        string.IsNullOrWhiteSpace(ConfigServices.Config.ScreenshotFolder) ? DefaultFolder : ConfigServices.Config.ScreenshotFolder;

    /// <summary>是否使用自訂資料夾。</summary>
    public static bool IsCustomFolder => !string.IsNullOrWhiteSpace(ConfigServices.Config.ScreenshotFolder);

    /// <summary>畫面上顯示的資料夾路徑。</summary>
    public static string FolderDisplay => FolderDisplayOverride ?? Folder;

    /// <summary>快捷鍵的顯示文字：Windows / Linux 是 Alt+F5，macOS 是 ⌥F5（Option + F5；MacBook 鍵盤要再按住 fn）。</summary>
    public static string ShortcutText => OperatingSystem.IsMacOS() ? "⌥F5" : "Alt+F5";

    /// <summary>依時間產生檔名，例如 DataSplitter_20261125_113005.png。</summary>
    public static string BuildFileName(DateTime time) =>
        $"{FilePrefix}{time.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture)}{FileExtension}";

    /// <summary>月份資料夾名稱，例如 2026/11 → 202611。</summary>
    public static string MonthFolderName(DateTime time) => time.ToString("yyyyMM", CultureInfo.InvariantCulture);

    /// <summary>是不是月份資料夾的名稱（6 位數字 yyyyMM，月份 01 ~ 12）。</summary>
    public static bool IsMonthFolderName(string name) =>
        name.Length == 6 && name.All(char.IsAsciiDigit) && int.Parse(name.AsSpan(4, 2), CultureInfo.InvariantCulture) is >= 1 and <= 12;

    /// <summary>月份的顯示文字：202611 → 2026/11。</summary>
    public static string FormatMonth(string key) => $"{key[..4]}/{key[4..]}";

    /// <summary>取得不會覆蓋既有檔案的完整路徑：同一秒已有檔案時改用 _2、_3……</summary>
    public static string GetUniquePath(string folder, DateTime time)
    {
        var baseName = Path.GetFileNameWithoutExtension(BuildFileName(time));
        var path = Path.Combine(folder, baseName + FileExtension);
        for (var i = 2; File.Exists(path); i++)
            path = Path.Combine(folder, $"{baseName}_{i}{FileExtension}");
        return path;
    }

    /// <summary>檔案大小的顯示文字：512 B、12.3 KB、1.5 MB。</summary>
    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return (bytes / 1024d).ToString("0.0", CultureInfo.InvariantCulture) + " KB";
        return (bytes / (1024d * 1024d)).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
    }

    /// <summary>列出單一資料夾（不含子資料夾）裡的 PNG 截圖，最新的在最前面。資料夾不存在時回傳空清單。</summary>
    public static IReadOnlyList<ScreenshotFile> List(string folder)
    {
        try
        {
            if (!Directory.Exists(folder)) return Array.Empty<ScreenshotFile>();
            return new DirectoryInfo(folder)
                .EnumerateFiles("*" + FileExtension)
                .Select(f => new ScreenshotFile(f.FullName, f.LastWriteTime, f.Length))
                .OrderByDescending(f => f.ModifiedAt)
                .ThenByDescending(f => f.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Screenshot] 無法讀取資料夾 {folder}：{ex.Message}");
            return Array.Empty<ScreenshotFile>();
        }
    }

    /// <summary>
    /// 依月份列出截圖：月份資料夾由新到舊，最後是根目錄的「未分類」。沒有截圖的月份資料夾不列出。
    /// </summary>
    public static IReadOnlyList<ScreenshotMonth> ListMonths(string? root = null)
    {
        root ??= Folder;
        var months = new List<ScreenshotMonth>();
        try
        {
            if (!Directory.Exists(root)) return months;
            foreach (var dir in new DirectoryInfo(root).EnumerateDirectories()
                         .Where(d => IsMonthFolderName(d.Name))
                         .OrderByDescending(d => d.Name, StringComparer.Ordinal))
            {
                var files = List(dir.FullName);
                if (files.Count > 0) months.Add(new ScreenshotMonth(dir.Name, dir.FullName, files));
            }
            var unsorted = List(root);
            if (unsorted.Count > 0) months.Add(new ScreenshotMonth(null, root, unsorted));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Screenshot] 無法讀取資料夾 {root}：{ex.Message}");
        }
        return months;
    }

    /// <summary>
    /// 刪除整個月份的截圖：刪掉資料夾裡所有 PNG，資料夾空了就把資料夾也刪掉
    /// （裡面還有使用者自己放的其他檔案時保留資料夾）。「未分類」只刪根目錄的 PNG，不動月份資料夾。
    /// 回傳刪掉的張數；有檔案刪不掉（例如正在使用中）時丟出例外。
    /// </summary>
    public static int DeleteMonth(ScreenshotMonth month)
    {
        var deleted = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(month.FolderPath, "*" + FileExtension).ToList())
            {
                File.Delete(file);
                deleted++;
            }
            if (!month.IsUnsorted && !Directory.EnumerateFileSystemEntries(month.FolderPath).Any())
                Directory.Delete(month.FolderPath);
        }
        finally
        {
            Changed?.Invoke();
        }
        return deleted;
    }

    /// <summary>把控制項（通常是主視窗）目前的畫面繪製成點陣圖。呼叫端要負責 Dispose。</summary>
    public static RenderTargetBitmap Render(Visual target)
    {
        var size = target.Bounds.Size;
        if (size.Width < 1 || size.Height < 1)
            throw new InvalidOperationException("畫面尚未完成排版，無法截圖");
        // 依螢幕縮放比例輸出實際像素（例如 150% 縮放時 1100×700 會存成 1650×1050），文字才不會糊
        var scaling = TopLevel.GetTopLevel(target)?.RenderScaling ?? 1.0;
        var pixelSize = PixelSize.FromSize(size, scaling);
        var bitmap = new RenderTargetBitmap(pixelSize, new Vector(96 * scaling, 96 * scaling));
        bitmap.Render(target);
        return bitmap;
    }

    /// <summary>拍下控制項目前的畫面並存成 PNG，回傳檔案完整路徑。失敗時丟出例外（由呼叫端顯示錯誤通知）。</summary>
    public static string Capture(Visual target, DateTime? time = null)
    {
        var now = time ?? DateTime.Now;
        var folder = Path.Combine(Folder, MonthFolderName(now));
        Directory.CreateDirectory(folder);
        var path = GetUniquePath(folder, now);
        using (var bitmap = Render(target))
            bitmap.Save(path, PngBitmapEncoderOptions.Default);
        Changed?.Invoke();
        return path;
    }

    /// <summary>刪除截圖檔。成功（或檔案本來就不在）回傳 true。</summary>
    public static bool Delete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
            Changed?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Screenshot] 無法刪除 {path}：{ex.Message}");
            return false;
        }
    }

    /// <summary>把截圖以圖片格式放進剪貼簿，可以直接貼到聊天軟體、Word、小畫家等。</summary>
    public static async Task<bool> CopyToClipboardAsync(IClipboard clipboard, string path)
    {
        try
        {
            Bitmap bitmap;
            using (var stream = File.OpenRead(path))
                bitmap = new Bitmap(stream);
            await clipboard.SetBitmapAsync(bitmap);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Screenshot] 無法複製到剪貼簿 {path}：{ex.Message}");
            return false;
        }
    }

    /// <summary>用檔案總管 / Finder 開啟資料夾（預設是截圖資料夾；不存在時先建立）。</summary>
    public static bool OpenFolder(string? folder = null)
    {
        folder ??= Folder;
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Screenshot] 無法建立資料夾 {folder}：{ex.Message}");
            return false;
        }
        return HelpService.OpenExternal(folder);
    }

    /// <summary>在檔案總管 / Finder 中顯示並選取這個檔案；Linux 沒有統一的作法，改為開啟所在資料夾。</summary>
    public static bool RevealInFolder(string path)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
                return true;
            }
            if (OperatingSystem.IsMacOS())
            {
                var info = new ProcessStartInfo("open") { UseShellExecute = false };
                info.ArgumentList.Add("-R");
                info.ArgumentList.Add(path);
                Process.Start(info);
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Screenshot] 無法在資料夾中顯示 {path}：{ex.Message}");
        }
        return HelpService.OpenExternal(Path.GetDirectoryName(path) ?? Folder);
    }

    /// <summary>用系統預設的看圖程式開啟截圖。</summary>
    public static bool OpenFile(string path) => HelpService.OpenExternal(path);
}
