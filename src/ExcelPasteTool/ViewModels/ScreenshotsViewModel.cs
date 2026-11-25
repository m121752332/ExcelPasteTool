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

    public ScreenshotItem(ScreenshotFile file) => File = file;

    public ScreenshotFile File { get; }
    public string FilePath => File.FilePath;
    public string FileName => File.FileName;
    public string ModifiedText => File.ModifiedText;
    public string SizeText => File.SizeText;
    /// <summary>清單第二行：時間 · 大小。</summary>
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

/// <summary>
/// 截圖管理頁：左邊是截圖清單（縮圖、檔名、時間、大小，最新的在最上面），右邊是選取那張的預覽與操作
/// （複製到剪貼簿、用預設程式開啟、在資料夾中顯示、刪除）。截圖新增或刪除時自動重新整理。
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

    public ObservableCollection<ScreenshotItem> Items { get; } = new();

    private ScreenshotItem? _selectedItem;
    private Bitmap? _preview;

    public ScreenshotItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (ReferenceEquals(_selectedItem, value)) return;
            _selectedItem = value;
            LoadPreview();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(ShowNoSelection));
        }
    }

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

    public bool HasSelection => _selectedItem != null;
    public bool IsEmpty => Items.Count == 0;
    public bool HasItems => Items.Count > 0;
    public bool ShowNoSelection => HasItems && !HasSelection;

    /// <summary>標題下方的說明：資料夾位置與張數。</summary>
    public string SummaryText => string.Format(
        LanguageManager.GetText("ScreenshotsSummary", "截圖資料夾：{0} · 共 {1} 張"), ScreenshotService.FolderDisplay, Items.Count);

    /// <summary>沒有截圖時的提示，帶上目前平台的快捷鍵。</summary>
    public string EmptyDescText => string.Format(
        LanguageManager.GetText("ScreenshotsEmptyDesc", "在 App 任何畫面按 {0}，就會拍下目前的畫面並存在這裡"), ScreenshotService.ShortcutText);

    public string CaptureTooltipText => $"{CaptureText.Text} ({ScreenshotService.ShortcutText})";

    public ScreenshotsViewModel()
    {
        LanguageManager.LanguageChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(SummaryText));
            OnPropertyChanged(nameof(EmptyDescText));
            OnPropertyChanged(nameof(CaptureTooltipText));
        });
    }

    /// <summary>重新讀取資料夾。原本選取的檔案還在就保留選取，否則選第一張（selectFirst 為 true 時一律選最新的）。</summary>
    public void Refresh(bool selectFirst = false)
    {
        var previous = selectFirst ? null : _selectedItem?.FilePath;
        foreach (var item in Items) item.Dispose();
        Items.Clear();
        foreach (var file in ScreenshotService.List())
            Items.Add(new ScreenshotItem(file));

        // 同一個檔案重新整理後是新的物件，預覽一律重讀
        _selectedItem = Items.FirstOrDefault(i => string.Equals(i.FilePath, previous, StringComparison.OrdinalIgnoreCase))
                        ?? Items.FirstOrDefault();
        LoadPreview();
        OnPropertyChanged(nameof(SelectedItem));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(ShowNoSelection));
        OnPropertyChanged(nameof(SummaryText));
    }

    /// <summary>刪除目前選取的截圖，選取移到下一張（沒有下一張就選上一張）。</summary>
    public bool DeleteSelected()
    {
        var item = _selectedItem;
        if (item == null) return false;
        var index = Items.IndexOf(item);
        // 先放掉預覽圖，再刪檔
        Preview = null;
        if (!ScreenshotService.Delete(item.FilePath)) return false;

        // ScreenshotService.Changed 可能已經重新整理過清單；這裡依刪除前的位置重新選取
        Refresh();
        if (Items.Count > 0)
            SelectedItem = Items[Math.Clamp(index, 0, Items.Count - 1)];
        return true;
    }

    private void LoadPreview()
    {
        if (_selectedItem == null)
        {
            Preview = null;
            return;
        }
        try
        {
            using var stream = File.OpenRead(_selectedItem.FilePath);
            Preview = new Bitmap(stream);
        }
        catch
        {
            Preview = null;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
