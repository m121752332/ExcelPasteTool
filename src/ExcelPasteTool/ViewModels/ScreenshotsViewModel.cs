using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;

namespace ExcelPasteTool;

/// <summary>截圖清單的一列：檔案資訊加上縮圖（第一次顯示時才讀檔，縮成 160px 寬，節省記憶體）。</summary>
public sealed class ScreenshotItem : IDisposable
{
    public const int ThumbnailWidth = 160;

    private Bitmap? _thumbnail;
    private bool _thumbnailLoaded;

    public ScreenshotItem(ScreenshotFile file, ScreenshotMonthGroup group)
    {
        File = file;
        Group = group;
    }

    public ScreenshotFile File { get; }
    public ScreenshotMonthGroup Group { get; }
    public string FilePath => File.FilePath;
    public string FileName => File.FileName;
    public string ModifiedText => File.ModifiedText;
    public string SizeText => File.SizeText;
    /// <summary>預覽區下方：時間 · 大小。</summary>
    public string DetailText => $"{File.ModifiedText} · {File.SizeText}";

    public Bitmap? Thumbnail
    {
        get
        {
            if (_thumbnailLoaded) return _thumbnail;
            _thumbnailLoaded = true;
            try
            {
                using var stream = System.IO.File.OpenRead(FilePath);
                _thumbnail = Bitmap.DecodeToWidth(stream, ThumbnailWidth, BitmapInterpolationMode.MediumQuality);
            }
            catch
            {
                _thumbnail = null; // 檔案損毀或被刪掉：縮圖留白，清單照常顯示
            }
            return _thumbnail;
        }
    }

    public void Dispose()
    {
        _thumbnail?.Dispose();
        _thumbnail = null;
    }
}

/// <summary>清單裡的月份資料夾列（例如 202611 · 50 張 · 12.3 MB），可展開 / 收合底下的截圖。</summary>
public sealed class ScreenshotMonthGroup : INotifyPropertyChanged
{
    private bool _isExpanded;

    public ScreenshotMonthGroup(ScreenshotMonth month, bool isExpanded)
    {
        Month = month;
        _isExpanded = isExpanded;
        Items = month.Files.Select(f => new ScreenshotItem(f, this)).ToList();
    }

    public ScreenshotMonth Month { get; }
    public IReadOnlyList<ScreenshotItem> Items { get; }
    /// <summary>識別用的鍵：月份資料夾名稱，「未分類」是空字串。</summary>
    public string Key => Month.Key ?? "";
    public bool IsUnsorted => Month.IsUnsorted;
    public string FolderPath => Month.FolderPath;

    /// <summary>標題：月份資料夾名稱（202611）；根目錄的舊截圖顯示「未分類」。</summary>
    public string Title => Month.Key ?? LanguageManager.GetText("ScreenshotsUnsorted", "未分類");

    /// <summary>第二行：張數 · 總大小（月份另外附上 2026/11）。</summary>
    public string DetailText
    {
        get
        {
            var count = string.Format(LanguageManager.GetText("ScreenshotsMonthCount", "{0} 張"), Month.Count);
            return Month.Key == null
                ? $"{count} · {Month.TotalSizeText}"
                : $"{ScreenshotService.FormatMonth(Month.Key)} · {count} · {Month.TotalSizeText}";
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// 截圖管理頁：左邊是依月份資料夾分組的清單（月份列可展開 / 收合，底下是縮圖、檔名、時間、大小，最新的在最上面），
/// 右邊依選取的東西顯示：選截圖 → 預覽與操作（複製、開啟、在資料夾中顯示、刪除）；
/// 選月份 → 張數、大小、資料夾路徑，可開啟該月資料夾或刪除整個月份。截圖新增或刪除時自動重新整理。
/// </summary>
public sealed class ScreenshotsViewModel : INotifyPropertyChanged
{
    public LocalizedText TitleText { get; } = new("ScreenshotsTitle", "截圖管理");
    public LocalizedText CaptureText { get; } = new("ScreenshotsCaptureButton", "拍攝截圖");
    public LocalizedText OpenFolderText { get; } = new("ScreenshotsOpenFolderButton", "開啟資料夾");
    public LocalizedText RefreshTooltipText { get; } = new("ScreenshotsRefreshTooltip", "重新整理");
    public LocalizedText CopyText { get; } = new("ScreenshotsCopyButton", "複製");
    public LocalizedText OpenFileText { get; } = new("ScreenshotsOpenFileButton", "開啟");
    public LocalizedText RevealText { get; } = new("ScreenshotsRevealButton", "在資料夾中顯示");
    public LocalizedText DeleteText { get; } = new("ScreenshotsDeleteButton", "刪除");
    public LocalizedText DeleteConfirmText { get; } = new("ScreenshotsDeleteConfirm", "要刪除這張截圖嗎？刪除後無法復原。");
    public LocalizedText DeleteConfirmButtonText { get; } = new("ScreenshotsDeleteConfirmButton", "刪除");
    public LocalizedText EmptyTitleText { get; } = new("ScreenshotsEmptyTitle", "還沒有截圖");
    public LocalizedText NoSelectionText { get; } = new("ScreenshotsNoSelection", "從左邊的清單選一張截圖來預覽");
    public LocalizedText OpenMonthFolderText { get; } = new("ScreenshotsOpenMonthFolderButton", "開啟這個資料夾");
    public LocalizedText DeleteMonthText { get; } = new("ScreenshotsDeleteMonthButton", "刪除整個月份");
    public LocalizedText ToggleMonthTooltipText { get; } = new("ScreenshotsToggleMonthTooltip", "展開 / 收合");

    /// <summary>左邊清單實際顯示的列：月份列（ScreenshotMonthGroup）與展開月份底下的截圖（ScreenshotItem）。</summary>
    public ObservableCollection<object> Rows { get; } = new();

    /// <summary>全部月份（含收合的）。</summary>
    public IReadOnlyList<ScreenshotMonthGroup> Groups => _groups;

    /// <summary>全部截圖（含收合月份裡的），依清單順序。</summary>
    public IEnumerable<ScreenshotItem> AllItems => _groups.SelectMany(g => g.Items);

    private List<ScreenshotMonthGroup> _groups = new();
    // 使用者展開 / 收合過的月份，重新整理後保留；沒動過的月份只展開最新的一個
    private readonly Dictionary<string, bool> _expandState = new();
    private object? _selectedRow;
    private Bitmap? _preview;

    /// <summary>清單上選取的列（截圖或月份）。</summary>
    public object? SelectedRow
    {
        get => _selectedRow;
        set
        {
            if (ReferenceEquals(_selectedRow, value)) return;
            _selectedRow = value;
            LoadPreview();
            NotifySelection();
        }
    }

    public ScreenshotItem? SelectedItem => _selectedRow as ScreenshotItem;
    public ScreenshotMonthGroup? SelectedGroup => _selectedRow as ScreenshotMonthGroup;

    /// <summary>右邊預覽用的原尺寸圖片（讀進記憶體後就關檔，刪除時不會被鎖住）。</summary>
    public Bitmap? Preview
    {
        get => _preview;
        private set
        {
            if (ReferenceEquals(_preview, value)) return;
            var old = _preview;
            _preview = value;
            OnPropertyChanged();
            old?.Dispose();
        }
    }

    public bool HasSelection => SelectedItem != null;
    public bool HasGroupSelection => SelectedGroup != null;
    public bool IsEmpty => _groups.Count == 0;
    public bool HasItems => _groups.Count > 0;
    public bool ShowNoSelection => HasItems && _selectedRow == null;
    public int TotalCount => _groups.Sum(g => g.Month.Count);

    /// <summary>標題下方的說明：資料夾位置與總張數。</summary>
    public string SummaryText => string.Format(
        LanguageManager.GetText("ScreenshotsSummary", "截圖資料夾：{0} · 共 {1} 張"), ScreenshotService.FolderDisplay, TotalCount);

    /// <summary>沒有截圖時的提示，帶上目前平台的快捷鍵。</summary>
    public string EmptyDescText => string.Format(
        LanguageManager.GetText("ScreenshotsEmptyDesc", "在 App 任何畫面按 {0}，就會拍下目前的畫面並存在這裡"), ScreenshotService.ShortcutText);

    public string CaptureTooltipText => $"{CaptureText.Text} ({ScreenshotService.ShortcutText})";

    /// <summary>選取月份時右邊的標題：2026/11（202611）或「未分類」。</summary>
    public string SelectedGroupTitle => SelectedGroup is { } g
        ? g.IsUnsorted ? g.Title : $"{ScreenshotService.FormatMonth(g.Key)}（{g.Key}）"
        : "";

    /// <summary>選取月份時右邊的說明：張數 · 總大小，以及資料夾位置。</summary>
    public string SelectedGroupDetail => SelectedGroup is { } g
        ? string.Format(LanguageManager.GetText("ScreenshotsMonthDetail", "{0} 張 · {1}"), g.Month.Count, g.Month.TotalSizeText)
        : "";

    public string SelectedGroupFolderText => SelectedGroup is { } g
        ? g.IsUnsorted
            ? LanguageManager.GetText("ScreenshotsUnsortedDesc", "分月份資料夾之前拍的截圖，放在截圖資料夾的根目錄")
            : string.Format(LanguageManager.GetText("ScreenshotsMonthFolder", "資料夾：{0}"), ScreenshotService.FolderDisplayOverride is { } display
                ? Path.Combine(display, g.Key)
                : g.FolderPath)
        : "";

    public string DeleteMonthConfirmText => SelectedGroup is { } g
        ? string.Format(LanguageManager.GetText("ScreenshotsDeleteMonthConfirm", "要刪除「{0}」的 {1} 張截圖嗎？刪除後無法復原。"), g.Title, g.Month.Count)
        : "";

    public ScreenshotsViewModel()
    {
        LanguageManager.LanguageChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() => Refresh());
    }

    /// <summary>
    /// 重新讀取資料夾。原本選取的截圖或月份還在就保留選取；否則選最新那一張。
    /// selectFirst 為 true 時（剛拍好一張）一律選最新的那張，並展開它所在的月份。
    /// </summary>
    public void Refresh(bool selectFirst = false)
    {
        var previousPath = selectFirst ? null : SelectedItem?.FilePath;
        var previousGroup = selectFirst ? null : SelectedGroup?.Key;

        foreach (var item in AllItems) item.Dispose();
        var months = ScreenshotService.ListMonths();
        _groups = months.Select((m, i) =>
        {
            var key = m.Key ?? "";
            var expanded = _expandState.TryGetValue(key, out var state) ? state : i == 0;
            return new ScreenshotMonthGroup(m, expanded);
        }).ToList();

        object? target = null;
        if (previousPath != null)
            target = AllItems.FirstOrDefault(i => string.Equals(i.FilePath, previousPath, StringComparison.OrdinalIgnoreCase));
        if (target == null && previousGroup != null)
            target = _groups.FirstOrDefault(g => g.Key == previousGroup);
        target ??= NewestItem();

        if (target is ScreenshotItem selected && !selected.Group.IsExpanded)
        {
            selected.Group.IsExpanded = true;
            _expandState[selected.Group.Key] = true;
        }

        RebuildRows();
        _selectedRow = target;
        LoadPreview();
        NotifySelection();
        OnPropertyChanged(nameof(Groups));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(EmptyDescText));
        OnPropertyChanged(nameof(CaptureTooltipText));
    }

    /// <summary>最新的一張：檔案時間最新的（月份資料夾由新到舊排，「未分類」的舊截圖也一起比）。</summary>
    private ScreenshotItem? NewestItem() =>
        AllItems.OrderByDescending(i => i.File.ModifiedAt).FirstOrDefault();

    /// <summary>展開 / 收合月份。收合時選取的截圖在裡面的話，改成選取該月份。</summary>
    public void ToggleGroup(ScreenshotMonthGroup group)
    {
        group.IsExpanded = !group.IsExpanded;
        _expandState[group.Key] = group.IsExpanded;
        var keep = _selectedRow;
        if (!group.IsExpanded && keep is ScreenshotItem item && item.Group == group)
            keep = group;
        RebuildRows();
        _selectedRow = null;
        SelectedRow = keep;
        OnPropertyChanged(nameof(SelectedRow));
    }

    private void RebuildRows()
    {
        Rows.Clear();
        foreach (var group in _groups)
        {
            Rows.Add(group);
            if (group.IsExpanded)
                foreach (var item in group.Items)
                    Rows.Add(item);
        }
    }

    /// <summary>刪除目前選取的截圖，選取移到同一個月份的下一張（沒有下一張就上一張；月份空了就選最新的一張）。</summary>
    public bool DeleteSelected()
    {
        if (SelectedItem is not { } item) return false;
        var siblings = item.Group.Items;
        var index = siblings.ToList().IndexOf(item);
        var next = siblings.Count > 1 ? siblings[index + 1 < siblings.Count ? index + 1 : index - 1].FilePath : null;
        // 先放掉預覽圖，再刪檔
        Preview = null;
        if (!ScreenshotService.Delete(item.FilePath)) return false;

        Refresh();
        var target = next == null ? null : AllItems.FirstOrDefault(i => string.Equals(i.FilePath, next, StringComparison.OrdinalIgnoreCase));
        if (target != null) SelectedRow = target;
        OnPropertyChanged(nameof(SelectedRow));
        return true;
    }

    /// <summary>刪除選取的整個月份（資料夾裡的 PNG 全刪，資料夾空了一併刪除）。回傳刪掉的張數；失敗時丟出例外。</summary>
    public int DeleteSelectedMonth()
    {
        if (SelectedGroup is not { } group) return 0;
        Preview = null;
        int deleted;
        try
        {
            deleted = ScreenshotService.DeleteMonth(group.Month);
        }
        finally
        {
            _expandState.Remove(group.Key);
            _selectedRow = null;
            Refresh();
        }
        return deleted;
    }

    private void LoadPreview()
    {
        if (SelectedItem is not { } item)
        {
            Preview = null;
            return;
        }
        try
        {
            using var stream = File.OpenRead(item.FilePath);
            Preview = new Bitmap(stream);
        }
        catch
        {
            Preview = null;
        }
    }

    private void NotifySelection()
    {
        OnPropertyChanged(nameof(SelectedRow));
        OnPropertyChanged(nameof(SelectedItem));
        OnPropertyChanged(nameof(SelectedGroup));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasGroupSelection));
        OnPropertyChanged(nameof(ShowNoSelection));
        OnPropertyChanged(nameof(SelectedGroupTitle));
        OnPropertyChanged(nameof(SelectedGroupDetail));
        OnPropertyChanged(nameof(SelectedGroupFolderText));
        OnPropertyChanged(nameof(DeleteMonthConfirmText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
