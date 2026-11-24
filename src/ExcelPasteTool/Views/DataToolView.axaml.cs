using Avalonia.Controls;
using Avalonia.Input.Platform;
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
        new(',', "SeparatorComma", "逗號 ,"),
        new(';', "SeparatorSemicolon", "分號 ;"),
        new(':', "SeparatorColon", "冒號 :"),
        new(' ', "SeparatorSpace", "空白")
    };

    private readonly ToastQueueHelper _toastHelper;

    // 最近一次「資料貼上」的重複值記錄（給「重複項」視窗用）；清除資料時歸零
    private List<DuplicateEntry> _duplicates = new();
    private int _pastedCount;

    // 重新填充下拉選單 / 程式改寫 TopBox 時，避免觸發使用者操作事件
    private bool _suppressEvents;
    private bool _initialized;

    public DataToolView()
    {
        InitializeComponent();
        DataContext = this;
        _toastHelper = new ToastQueueHelper(this);
        DataItems.CollectionChanged += OnDataItemsChanged;
        RightPanel.SizeChanged += (_, e) => ApplyToolbarLayout(e.NewSize.Width);
        Loaded += OnLoaded;
        ToggleSqlPanelButton.Click += (_, _) => IsSqlPanelVisible = !IsSqlPanelVisible;
        // 設定頁切換「顯示 SQL 條件區塊」時立即套用（單例，不會重複訂閱；工具頁本身也有快取只建一次）
        SettingsViewModel.Instance.ShowSqlPanelChanged += visible => IsSqlPanelVisible = visible;
        IsSqlPanelVisible = ConfigServices.Config.ShowSqlPanel;
        ApplySavedSplitRatio();
        LeftPanelSplitter.DragCompleted += (_, _) => SaveSplitRatio();
        InitSqlOptions();
    }

    // ===== 左側上下分隔條 =====

    /// <summary>分隔資料所佔比例的上下限（兩邊都留得下最小高度）。</summary>
    private const double MinSplitRatio = 0.2, MaxSplitRatio = 0.8;

    // SQL 區塊隱藏前的列高，重新顯示時還原（使用者拖過分隔條時才不會被重設成各半）
    private GridLength _sqlRowHeight = new(1, GridUnitType.Star);

    private void ApplySavedSplitRatio()
    {
        var ratio = ConfigServices.Config.LeftPanelSplitRatio;
        if (ratio <= 0) return; // 0 = 預設各半
        ratio = System.Math.Clamp(ratio, MinSplitRatio, MaxSplitRatio);
        LeftPanelGrid.RowDefinitions[1].Height = new GridLength(ratio, GridUnitType.Star);
        _sqlRowHeight = new GridLength(1 - ratio, GridUnitType.Star);
        if (_isSqlPanelVisible) LeftPanelGrid.RowDefinitions[3].Height = _sqlRowHeight;
    }

    private void SaveSplitRatio()
    {
        double top = TopBox.Bounds.Height, bottom = SqlSection.Bounds.Height;
        if (top <= 0 || bottom <= 0) return;
        var ratio = System.Math.Clamp(top / (top + bottom), MinSplitRatio, MaxSplitRatio);
        // 拖完一律轉回比例（Star），視窗縮放時上下仍按比例分配
        LeftPanelGrid.RowDefinitions[1].Height = new GridLength(ratio, GridUnitType.Star);
        _sqlRowHeight = new GridLength(1 - ratio, GridUnitType.Star);
        LeftPanelGrid.RowDefinitions[3].Height = _sqlRowHeight;
        ConfigServices.Config.LeftPanelSplitRatio = System.Math.Round(ratio, 3);
        ConfigServices.Save();
    }

    // ===== SQL 格式選項 =====

    private SqlFormatOptions _sqlOptions = SqlFormatOptions.Default;

    private void InitSqlOptions()
    {
        _sqlOptions = SqlFormatOptions.FromConfig(ConfigServices.Config);
        _suppressEvents = true;
        try
        {
            SqlColumnBox.Text = _sqlOptions.ColumnName;
            SqlItemsPerLineBox.Value = _sqlOptions.ItemsPerLine;
            SqlUnquoteNumbersBox.IsChecked = _sqlOptions.UnquoteNumbers;
        }
        finally { _suppressEvents = false; }

        SqlOptionsButton.Click += (_, _) => SetSqlOptionsOpen(!SqlOptionsPanel.IsVisible);
        SqlColumnBox.TextChanged += (_, _) => OnSqlOptionsChanged();
        SqlItemsPerLineBox.ValueChanged += (_, _) => OnSqlOptionsChanged();
        SqlUnquoteNumbersBox.IsCheckedChanged += (_, _) => OnSqlOptionsChanged();
    }

    private void SetSqlOptionsOpen(bool open)
    {
        SqlOptionsPanel.IsVisible = open && _isSqlPanelVisible;
        SqlOptionsButton.Classes.Set("active", SqlOptionsPanel.IsVisible);
    }

    private void OnSqlOptionsChanged()
    {
        if (_suppressEvents) return;
        var config = ConfigServices.Config;
        config.SqlColumnName = (SqlColumnBox.Text ?? string.Empty).Trim();
        config.SqlItemsPerLine = (int)System.Math.Clamp(SqlItemsPerLineBox.Value ?? 0, 0, SqlFormatOptions.MaxItemsPerLine);
        config.SqlUnquoteNumbers = SqlUnquoteNumbersBox.IsChecked == true;
        _sqlOptions = SqlFormatOptions.FromConfig(config);
        ConfigServices.Save();
        UpdateBottom();
    }

    /// <summary>SQL 條件下方的提醒：超過 1000 筆已拆組、拆組時沒填欄位名稱、勾了「數值不加引號」但資料不全是數字。</summary>
    private void UpdateSqlWarning()
    {
        var lines = new List<string>();
        if (Items.Count > PasteDataHelper.OracleInListLimit)
        {
            lines.Add(string.Format(LanguageManager.GetText("SqlSplitWarning",
                "共 {0} 筆，超過 Oracle 單一 IN 上限 1000 筆，已拆成 {1} 組以 OR 串接"),
                Items.Count, (Items.Count + PasteDataHelper.OracleInListLimit - 1) / PasteDataHelper.OracleInListLimit));
            if (_sqlOptions.ColumnName.Length == 0)
                lines.Add(LanguageManager.GetText("SqlColumnMissingWarning", "尚未填欄位名稱，暫用 col，請在格式選項填入或貼上後自行替換"));
        }
        if (_sqlOptions.UnquoteNumbers && Items.Count > 0 && !PasteDataHelper.AllPlainNumbers(Items))
            lines.Add(LanguageManager.GetText("SqlNotAllNumbersWarning", "資料不全是數字（或有前導零），已全部加上引號"));
        SqlWarningText.Text = string.Join("\n", lines);
        SqlWarningText.IsVisible = lines.Count > 0 && _isSqlPanelVisible;
    }

    private bool _isSqlPanelVisible = true;

    /// <summary>
    /// SQL 條件區塊是否顯示。啟動時依設定頁「顯示 SQL 條件區塊」決定；
    /// 標題旁的眼睛按鈕只切換當次畫面，不會改設定。隱藏時只留標題列，上方的分隔資料框撐滿。
    /// </summary>
    public bool IsSqlPanelVisible
    {
        get => _isSqlPanelVisible;
        set
        {
            if (_isSqlPanelVisible && !value && LeftPanelGrid.RowDefinitions[3].Height.IsStar)
                _sqlRowHeight = LeftPanelGrid.RowDefinitions[3].Height;
            _isSqlPanelVisible = value;
            BottomBox.IsVisible = value;
            LeftPanelSplitter.IsVisible = value;
            var sqlRow = LeftPanelGrid.RowDefinitions[3];
            sqlRow.Height = value ? _sqlRowHeight : GridLength.Auto;
            sqlRow.MinHeight = value ? 140 : 0;
            LeftPanelGrid.RowDefinitions[2].Height = new GridLength(value ? 14 : 8);
            SetSqlOptionsOpen(value && SqlOptionsPanel.IsVisible);
            if (!value) SqlWarningText.IsVisible = false;
            else UpdateSqlWarning();
            ToggleSqlPanelIcon.Data = Avalonia.Media.StreamGeometry.Parse(value ? IconManager.EyeOffIcon : IconManager.EyeIcon);
            UpdateSqlPanelTooltip();
        }
    }

    private void UpdateSqlPanelTooltip() =>
        ToolTip.SetTip(ToggleSqlPanelButton, _isSqlPanelVisible
            ? LanguageManager.GetText("SqlPanelHideTooltip", "隱藏 SQL 條件")
            : LanguageManager.GetText("SqlPanelShowTooltip", "顯示 SQL 條件"));

    /// <summary>右欄寬度低於這個值時，「重複項」「清空資料」改成只顯示圖示。</summary>
    public const double CompactToolbarThreshold = 420;

    /// <summary>目前工具列是否為精簡模式（只顯示圖示）。</summary>
    public bool IsToolbarCompact { get; private set; }

    private void ApplyToolbarLayout(double rightPanelWidth)
    {
        IsToolbarCompact = rightPanelWidth < CompactToolbarThreshold;
        DuplicatesButtonText.IsVisible = !IsToolbarCompact;
        ClearButtonText.IsVisible = !IsToolbarCompact;
        DuplicatesButton.Classes.Set("icon-only", IsToolbarCompact);
        ClearDataButton.Classes.Set("icon-only", IsToolbarCompact);
        UpdateDuplicatesButton();
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
            DuplicatesButton.Click += DuplicatesButton_Click;
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
        TopBox.PlaceholderText = LanguageManager.GetText("TopBoxWatermark", "也可以直接在這裡輸入以分隔符號串接的資料");
        ToolTip.SetTip(CopyTopBoxButton, LanguageManager.GetText("CopyTopBoxTooltip", "複製分隔資料"));
        ToolTip.SetTip(CopyBottomBoxButton, LanguageManager.GetText("CopySqlTooltip", "複製SQL條件"));
        UpdateSqlPanelTooltip();
        ToolTip.SetTip(SqlOptionsButton, LanguageManager.GetText("SqlOptionsTooltip", "SQL 格式選項"));
        SqlColumnLabel.Text = LanguageManager.GetText("SqlColumnLabel", "欄位名稱:");
        SqlColumnBox.PlaceholderText = LanguageManager.GetText("SqlColumnWatermark", "例如 ima01");
        SqlItemsPerLineLabel.Text = LanguageManager.GetText("SqlItemsPerLineLabel", "每行筆數:");
        SqlUnquoteNumbersBox.Content = LanguageManager.GetText("SqlUnquoteNumbers", "數值不加引號");
        SqlOptionsHint.Text = LanguageManager.GetText("SqlOptionsHint",
            "每行筆數 0 代表不換行；超過 1000 筆會自動拆成多組 IN 以 OR 串接（Oracle 上限）");
        if (_isSqlPanelVisible) UpdateSqlWarning();
        ToolTip.SetTip(CopyTableButton, LanguageManager.GetText("CopyTableTooltip", "複製全部資料"));
        ToolTip.SetTip(ClearDataButton, ClearButtonText.Text);
        ToolTip.SetTip(PasteDataButton, LanguageManager.GetText("PasteDataTooltip", "貼上剪貼簿資料 (Alt+P)"));

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
            _toastHelper.EnqueueToast(LanguageManager.GetText("LanguageTempToast", "語言已臨時切換（未保存）"), ToastSeverity.Warning);
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
            _toastHelper.EnqueueToast(LanguageManager.GetText("ThemeTempToast", "主題已臨時切換（未保存）"), ToastSeverity.Warning);
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
        UpdateDuplicatesButton();
    }

    /// <summary>「重複項」按鈕顯示有重複的值的個數；沒有重複時停用。精簡模式時個數改顯示在角標。</summary>
    private void UpdateDuplicatesButton()
    {
        var count = _duplicates.Count;
        DuplicatesButtonText.Text = string.Format(LanguageManager.GetText("DuplicatesButton", "重複項 {0}"), count);
        DuplicatesButton.IsEnabled = count > 0;
        DuplicatesCountText.Text = count > 99 ? "99+" : count.ToString();
        DuplicatesCountBadge.IsVisible = IsToolbarCompact && count > 0;
        // 精簡模式看不到文字，提示第一行補上按鈕文字（含個數）
        ToolTip.SetTip(DuplicatesButton, DuplicatesButtonText.Text + "\n"
            + LanguageManager.GetText("DuplicatesTooltip", "查看貼上時被移除的重複資料"));
    }

    private void SetDuplicates(IReadOnlyCollection<string> pastedValues)
    {
        _pastedCount = pastedValues.Count;
        _duplicates = PasteDataHelper.FindDuplicates(pastedValues);
        UpdateDuplicatesButton();
    }

    private async void DuplicatesButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_duplicates.Count == 0 || TopLevel.GetTopLevel(this) is not Window owner) return;
        await new DuplicatesWindow(_duplicates, _pastedCount).ShowDialog(owner);
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
        => await PasteFromClipboardAsync(TopLevel.GetTopLevel(this)?.Clipboard);

    /// <summary>把剪貼簿內容貼到右側資料表（「資料貼上」按鈕與主視窗的 Alt+P 共用）。</summary>
    public async Task PasteFromClipboardAsync(Avalonia.Input.Platform.IClipboard? clipboard)
    {
        if (clipboard == null) return;
        var text = await clipboard.TryGetTextAsync();
        if (string.IsNullOrWhiteSpace(text)) return;

        var processedData = PasteDataHelper.ParsePastedText(text);
        SetDuplicates(processedData);
        SetItems(processedData);
        UpdateTopAndBottom();
    }

    private void ClearData_Click(object? sender, RoutedEventArgs e)
    {
        SetDuplicates(System.Array.Empty<string>());
        SetItems(Enumerable.Empty<string>());
        _suppressEvents = true;
        try
        {
            TopBox.Text = "";
            BottomBox.Text = "";
            SqlWarningText.IsVisible = false;
            _separator = _defaultSeparator;
            SeparatorComboBox.SelectedIndex = 0;
        }
        finally
        {
            _suppressEvents = false;
        }
        EnqueueToast(LanguageManager.GetText("ClearedToast", "畫面已清理回初始狀態"), ToastSeverity.Info);
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
        SetItems(SeparatorHelper.Split(text, _separator).Select(x => x.Trim()).Where(x => x.Length > 0));
        UpdateBottom();
    }

    private void UpdateTopAndBottom()
    {
        _suppressEvents = true;
        try { TopBox.Text = SeparatorHelper.Join(Items, _separator); }
        finally { _suppressEvents = false; }
        UpdateBottom();
    }

    private void UpdateBottom()
    {
        BottomBox.Text = PasteDataHelper.BuildSqlCondition(Items, _sqlOptions);
        UpdateSqlWarning();
    }

    // ===== 複製 =====

    private void EnqueueToast(string message, ToastSeverity severity = ToastSeverity.Success) => _toastHelper.EnqueueToast(message, severity);

    private async Task CopyTextAsync(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null) return;
        (TopLevel.GetTopLevel(this) as Window)?.Activate();
        await Task.Delay(50);
        await clipboard.SetTextAsync(text);
        var check = await clipboard.TryGetTextAsync();
        if (check == text)
            EnqueueToast(LanguageManager.GetText("CopySuccessToast", "已複製到剪貼簿！"));
        else
            EnqueueToast(LanguageManager.GetText("CopyFailedToast", "複製失敗，請重試"), ToastSeverity.Error);
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
