using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelPasteTool.Helpers;

namespace ExcelPasteTool;

public partial class DataToolView : UserControl
{
    public ObservableCollection<DataItem> DataItems { get; } = new();
    public ObservableCollection<string> Items { get; } = new();
    private char _separator = '|';
    private readonly char _defaultSeparator = '|';
    private readonly List<SeparatorOption> _separatorOptions = new()
    {
        new('|', "SeparatorPipe", "豎線 |"),
        new(';', "SeparatorSemicolon", "分號 ;"),
        new(':', "SeparatorColon", "冒號 :"),
        new(' ', "SeparatorSpace", "空白")
    };

    private readonly ToastQueueHelper _toastHelper;

    // 重新填充下拉選單 / 程式改寫 TopBox 時，避免觸發使用者操作事件
    private bool _suppressEvents;
    private bool _initialized;

    public DataToolView()
    {
        InitializeComponent();
        DataContext = this;
        _toastHelper = new ToastQueueHelper(this);
        DataItems.CollectionChanged += OnDataItemsChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 頁面有快取，切換分頁回來會再次觸發 Loaded；事件只需要掛一次，否則按一次會執行多次
        if (!_initialized)
        {
            _initialized = true;
            ThemeSelector.SelectionChanged += ThemeSelector_SelectionChanged;
            LanguageSelector.SelectionChanged += LanguageSelector_SelectionChanged;
            SeparatorComboBox.SelectionChanged += SeparatorComboBox_SelectionChanged;
            TopBox.TextChanged += TopBoxChanged;
            PasteDataButton.Click += PasteData_Click;
            ClearDataButton.Click += ClearData_Click;
            CopyTopBoxButton.Click += CopyTopBoxButton_Click;
            CopyBottomBoxButton.Click += CopyBottomBoxButton_Click;
        }

        // 主題 / 語言事件在離開畫面時解除，回來時重新訂閱
        ThemeManager.ThemeChanged -= OnThemeChanged;
        LanguageManager.LanguageChanged -= OnLanguageChanged;
        ThemeManager.ThemeChanged += OnThemeChanged;
        LanguageManager.LanguageChanged += OnLanguageChanged;

        UpdateUITexts();
        UpdateTableState();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        ThemeManager.ThemeChanged -= OnThemeChanged;
        LanguageManager.LanguageChanged -= OnLanguageChanged;
        base.OnDetachedFromVisualTree(e);
    }

    // ===== 文字與下拉選單 =====

    private void UpdateUITexts()
    {
        SeparatorLabel.Text = LanguageManager.GetText("SeparatorLabel", "分隔符號:");
        LanguageLabel.Text = LanguageManager.GetText("LanguageLabel", "語言:");
        ThemeLabel.Text = LanguageManager.GetText("ThemeLabel", "主題:");
        PipDataTitle.Text = LanguageManager.GetText("PipDataTitle", "PIP 分隔資料");
        SqlConditionTitle.Text = LanguageManager.GetText("SqlConditionTitle", "SQL 條件");
        PasteButtonText.Text = LanguageManager.GetText("PasteDataButton", "資料貼上");
        ClearButtonText.Text = LanguageManager.GetText("ClearDataButton", "清空資料");
        RowNumberHeader.Text = LanguageManager.GetText("RowNumberHeader", "行號");
        DataContentHeader.Text = LanguageManager.GetText("DataContentHeader", "資料內容");
        EmptyStateText.Text = LanguageManager.GetText("EmptyStateText", "尚無資料\n從 Excel 複製一欄後，按「資料貼上」");
        TopBox.Watermark = LanguageManager.GetText("TopBoxWatermark", "也可以直接在這裡輸入以分隔符號串接的資料");

        _suppressEvents = true;
        try
        {
            FillComboBox(SeparatorComboBox,
                _separatorOptions.Select(o => LanguageManager.GetText(o.TextKey, o.DefaultText)),
                _separatorOptions.FindIndex(o => o.Character == _separator));
            var languages = LanguageManager.GetAllLanguages();
            FillComboBox(LanguageSelector, languages.Select(l => l.Name),
                languages.FindIndex(l => l.Language == LanguageManager.CurrentLanguage));
            FillThemeSelector();
        }
        finally
        {
            _suppressEvents = false;
        }
        UpdateTableState();
    }

    private void FillThemeSelector()
    {
        var themes = ThemeManager.GetAllThemes();
        FillComboBox(ThemeSelector, themes.Select(t => t.Name),
            themes.FindIndex(t => t.Theme == ThemeManager.CurrentTheme));
    }

    private static void FillComboBox(ComboBox comboBox, IEnumerable<string> items, int selectedIndex)
    {
        comboBox.Items.Clear();
        foreach (var item in items)
            comboBox.Items.Add(item);
        comboBox.SelectedIndex = selectedIndex;
    }

    // 語言切換可能是由本頁的下拉選單觸發，不能在它自己的 SelectionChanged 內清空 Items（會造成索引越界），
    // 因此延後到目前事件處理完畢再重新填充。
    private void OnLanguageChanged() => Avalonia.Threading.Dispatcher.UIThread.Post(UpdateUITexts);

    private void OnThemeChanged()
    {
        // 主題名稱不變，只需同步選取項目
        _suppressEvents = true;
        try { ThemeSelector.SelectedIndex = ThemeManager.GetAllThemes().FindIndex(t => t.Theme == ThemeManager.CurrentTheme); }
        finally { _suppressEvents = false; }
    }

    private void LanguageSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents || LanguageSelector.SelectedIndex < 0) return;
        var allLanguages = LanguageManager.GetAllLanguages();
        if (LanguageSelector.SelectedIndex >= allLanguages.Count) return;
        var selectedLanguage = allLanguages[LanguageSelector.SelectedIndex].Language;
        if (LanguageManager.CurrentLanguage != selectedLanguage)
        {
            // 工具頁面的語言切換是臨時的，不保存到配置檔案（要保存請到設定頁）
            LanguageController.SetLanguage(selectedLanguage, false);
            _toastHelper.EnqueueToast("語言已臨時切換（未保存）");
        }
    }

    private void ThemeSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents || ThemeSelector.SelectedIndex < 0) return;
        var allThemes = ThemeManager.GetAllThemes();
        if (ThemeSelector.SelectedIndex >= allThemes.Count) return;
        var selectedTheme = allThemes[ThemeSelector.SelectedIndex].Theme;
        if (ThemeManager.CurrentTheme != selectedTheme)
        {
            // 工具頁面的主題切換是臨時的，不保存到配置檔案（要保存請到設定頁）
            ThemeManager.CurrentTheme = selectedTheme;
            _toastHelper.EnqueueToast("主題已臨時切換（未保存）");
        }
    }

    private void SeparatorComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        var selectedIndex = SeparatorComboBox.SelectedIndex;
        if (selectedIndex < 0 || selectedIndex >= _separatorOptions.Count) return;
        var newSeparator = _separatorOptions[selectedIndex].Character;
        if (_separator != newSeparator)
        {
            _separator = newSeparator;
            UpdateTopAndBottom();
        }
    }

    // ===== 資料處理 =====

    private void OnDataItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateTableState();

    /// <summary>更新筆數徽章與「無資料」提示。</summary>
    private void UpdateTableState()
    {
        var count = DataItems.Count;
        RowCountText.Text = string.Format(LanguageManager.GetText("RowCountFormat", "共 {0} 筆"), count);
        EmptyStateText.IsVisible = count == 0;
    }

    private void SetItems(IEnumerable<string> values)
    {
        DataItems.Clear();
        Items.Clear();
        int rowNumber = 1;
        foreach (var value in values.Distinct())
        {
            DataItems.Add(new DataItem(rowNumber++, value));
            Items.Add(value);
        }
        TableScroll.Offset = default;
    }

    private async void PasteData_Click(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;
        var text = await clipboard.GetTextAsync();
        if (string.IsNullOrWhiteSpace(text)) return;

        var lines = text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        var processedData = lines
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Select(GetFirstColumn)
            .Where(v => v.Length > 0);
        SetItems(processedData);
        UpdateTopAndBottom();
    }

    private static string GetFirstColumn(string line)
    {
        var separators = new char[] { '\t', ',', ';', '|' };
        foreach (var sep in separators)
        {
            if (line.Contains(sep))
            {
                var parts = line.Split(new char[] { sep }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                    return parts[0].Trim();
            }
        }
        return line.Trim();
    }

    private void ClearData_Click(object? sender, RoutedEventArgs e)
    {
        SetItems(Enumerable.Empty<string>());
        _suppressEvents = true;
        try
        {
            TopBox.Text = "";
            BottomBox.Text = "";
            _separator = _defaultSeparator;
            SeparatorComboBox.SelectedIndex = 0;
        }
        finally
        {
            _suppressEvents = false;
        }
        EnqueueToast("畫面已清理回初始狀態");
    }

    /// <summary>移除單筆資料：重排後面的行號，並同步分隔資料與 SQL 條件。捲動位置保留，方便連續刪除。</summary>
    private void RemoveRow_Click(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not DataItem item) return;
        var index = DataItems.IndexOf(item);
        if (index < 0) return;

        DataItems.RemoveAt(index);
        if (index < Items.Count) Items.RemoveAt(index);
        for (int i = index; i < DataItems.Count; i++)
            DataItems[i].RowNumber = i + 1;
        UpdateTopAndBottom();
    }

    // 提示文字在滑入時才設定，語言切換後不必重建資料列
    private void RemoveRowButton_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (sender is Control control)
            ToolTip.SetTip(control, LanguageManager.GetText("RemoveRowTooltip", "移除此筆"));
    }

    private void TopBoxChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressEvents) return;
        var text = TopBox.Text ?? string.Empty;
        SetItems(text.Split(_separator).Select(x => x.Trim()).Where(x => x.Length > 0));
        UpdateBottom();
    }

    private void UpdateTopAndBottom()
    {
        _suppressEvents = true;
        try { TopBox.Text = string.Join(_separator, Items); }
        finally { _suppressEvents = false; }
        UpdateBottom();
    }

    private void UpdateBottom()
    {
        BottomBox.Text = Items.Count == 0
            ? string.Empty
            : $"({string.Join(", ", Items.Select(x => $"'{x.Replace("'", "''")}'"))})";
    }

    // ===== 複製 =====

    private void EnqueueToast(string message) => _toastHelper.EnqueueToast(message);

    private async Task CopyTextAsync(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;
        (this.GetVisualRoot() as Window)?.Activate();
        await Task.Delay(50);
        await clipboard.SetTextAsync(text);
        var check = await clipboard.GetTextAsync();
        EnqueueToast(check == text ? "已複製到剪貼簿！" : "複製失敗，請重試");
    }

    private async void CopyTopBoxButton_Click(object? sender, RoutedEventArgs e) => await CopyTextAsync(TopBox.Text);

    private async void CopyBottomBoxButton_Click(object? sender, RoutedEventArgs e) => await CopyTextAsync(BottomBox.Text);

    private async void CopyTableButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataItems.Count > 0)
            await CopyTextAsync(string.Join("\r\n", DataItems.Select(x => x.Value)));
    }

    public class SeparatorOption
    {
        public char Character { get; }
        public string TextKey { get; }
        public string DefaultText { get; }
        public SeparatorOption(char character, string textKey, string defaultText)
        {
            Character = character;
            TextKey = textKey;
            DefaultText = defaultText;
        }
    }
}
