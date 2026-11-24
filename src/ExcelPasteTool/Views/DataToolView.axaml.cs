using Avalonia.Controls;
using Avalonia.Input;
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
        // SelectableTextBlock 會自己處理雙擊（選字），所以連已處理的事件也要接
        DataRows.AddHandler(InputElement.DoubleTappedEvent, OnRowDoubleTapped,
            Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
        // 點一下選取該列（按 Insert 會在它下面插入一筆）
        DataRows.AddHandler(InputElement.PointerPressedEvent, OnRowPointerPressed,
            Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
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

        // 選單開著時按鈕維持高亮
        if (SqlOptionsButton.Flyout is { } flyout)
        {
            flyout.Opened += (_, _) => SqlOptionsButton.Classes.Set("active", true);
            flyout.Closed += (_, _) => SqlOptionsButton.Classes.Set("active", false);
        }
        SqlColumnBox.TextChanged += (_, _) => OnSqlOptionsChanged();
        SqlItemsPerLineBox.ValueChanged += (_, _) => OnSqlOptionsChanged();
        SqlUnquoteNumbersBox.IsCheckedChanged += (_, _) => OnSqlOptionsChanged();
    }

    /// <summary>SQL 格式選項的浮出選單是否開著。</summary>
    public bool IsSqlOptionsOpen => SqlOptionsButton.Flyout?.IsOpen == true;

    public void ShowSqlOptions() => SqlOptionsButton.Flyout?.ShowAt(SqlOptionsButton);

    public void HideSqlOptions() => SqlOptionsButton.Flyout?.Hide();

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
            sqlRow.MinHeight = value ? 150 : 0;
            LeftPanelGrid.RowDefinitions[2].Height = new GridLength(value ? 14 : 8);
            if (!value) HideSqlOptions();
            SqlOptionsButton.IsVisible = value;
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
        AppendPasteButtonText.IsVisible = !IsToolbarCompact;
        AppendPasteButton.Classes.Set("icon-only", IsToolbarCompact);
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
            AppendPasteButton.Click += async (_, _) => await AppendFromClipboardAsync(TopLevel.GetTopLevel(this)?.Clipboard);
            AddRowButton.Click += (_, _) => AddNewRow(DataItems.Count);
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
        AppendPasteButtonText.Text = LanguageManager.GetText("AppendPasteButton", "追加貼上");
        ToolTip.SetTip(AppendPasteButton, LanguageManager.GetText("AppendPasteTooltip", "把剪貼簿資料接在現有資料後面，重複的略過 (Alt+Shift+P)"));
        AddRowButtonText.Text = LanguageManager.GetText("AddRowButton", "新增一筆（選取某列後按 Insert 可插在它下面）");

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
        // 空白暫存列（正在新增、還沒輸入）不算筆數
        var count = DataItems.Count(x => !x.IsNew);
        RowCountText.Text = string.Format(LanguageManager.GetText("RowCountFormat", "共 {0} 筆"), count);
        EmptyStateText.IsVisible = DataItems.Count == 0;
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
        RemoveRowAt(DataItems.IndexOf(item));
    }

    // ===== 資料表直接編輯 =====

    private void OnRowDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Control source || source.DataContext is not DataItem item) return;
        if (source is TextBox || source.FindAncestorOfType<Button>() != null || source is Button) return;
        var row = source.FindAncestorOfType<Grid>(includeSelf: true);
        while (row != null && !row.Classes.Contains("data-row"))
            row = row.FindAncestorOfType<Grid>();
        var editor = row?.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(t => t.Classes.Contains("cell-editor"));
        if (editor != null) BeginEdit(item, editor);
    }

    // 提示「雙擊可修改」，和移除按鈕一樣在滑入時才設定
    private void RowValue_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Control control)
            ToolTip.SetTip(control, LanguageManager.GetText("EditCellTooltip", "雙擊可修改這筆資料"));
    }

    /// <summary>開始編輯一筆資料（同時間只編輯一筆）。</summary>
    public void BeginEdit(DataItem item, TextBox editor)
    {
        foreach (var other in DataItems.Where(x => x.IsEditing && x != item))
            other.IsEditing = false;
        _activeEditor = editor;
        _activeEditorFocused = false;
        editor.Text = item.Value;
        item.IsEditing = true;
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // 還在編輯同一筆才把焦點放進去（期間可能已經確認或取消）
            if (!item.IsEditing || !ReferenceEquals(editor.DataContext, item)) return;
            editor.Focus();
            editor.SelectAll();
            _activeEditorFocused = editor.IsFocused;
        });
    }

    // 目前正在編輯的輸入框；只有它真的拿到過焦點之後，「離開輸入框」才算確認，
    // 避免隱藏中的輸入框因焦點移動觸發 LostFocus，把剛開始的編輯提早結束
    private TextBox? _activeEditor;
    private bool _activeEditorFocused;

    private void CellEditor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox editor || editor.DataContext is not DataItem item) return;
        if (e.Key == Key.Enter)
        {
            // 在最後一列新增時，按 Enter 確認後直接開下一筆，方便連續輸入
            var continueAdding = item.IsNew && DataItems.IndexOf(item) == DataItems.Count - 1 && !string.IsNullOrWhiteSpace(editor.Text);
            CommitEdit(item, editor.Text);
            e.Handled = true;
            if (continueAdding && !item.IsNew && DataItems.Contains(item))
                AddNewRow(DataItems.Count);
        }
        else if (e.Key == Key.Escape)
        {
            item.IsEditing = false; // 取消：不改資料
            _activeEditor = null;
            if (item.IsNew) RemoveRowAt(DataItems.IndexOf(item)); // 取消新增：拿掉暫存列
            e.Handled = true;
        }
    }

    private void CellEditor_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox editor || !ReferenceEquals(editor, _activeEditor) || !_activeEditorFocused) return;
        if (editor.DataContext is DataItem item)
            CommitEdit(item, editor.Text);
    }

    /// <summary>
    /// 確認編輯：去掉前後空白；清空等於刪除這筆；和其他筆重複時不修改並提示（資料表維持不重複）。
    /// 修改後同步左邊的分隔資料與 SQL 條件。
    /// </summary>
    public void CommitEdit(DataItem item, string? text)
    {
        if (!item.IsEditing) return;
        item.IsEditing = false;
        _activeEditor = null;
        _activeEditorFocused = false;
        var index = DataItems.IndexOf(item);
        if (index < 0) return;

        var value = (text ?? string.Empty).Trim();
        if (value == item.Value && !item.IsNew) return;
        if (value.Length == 0)
        {
            RemoveRowAt(index);
            return;
        }
        if (Items.Where((x, i) => i != index).Contains(value))
        {
            EnqueueToast(string.Format(LanguageManager.GetText("EditDuplicateToast", "「{0}」已經在資料表裡，未修改"), value), ToastSeverity.Warning);
            if (item.IsNew) RemoveRowAt(index); // 新增的那筆是空白暫存列，重複就不留
            return;
        }
        item.IsNew = false;
        UpdateTableState(); // IsNew 不會觸發集合變更，筆數要自己更新
        item.Value = value;
        if (index < Items.Count) Items[index] = value;
        UpdateTopAndBottom();
    }

    // ===== 追加資料：追加貼上、表格底部新增列、Insert 插入 =====

    /// <summary>
    /// 追加貼上（「追加貼上」按鈕與 Alt+Shift+P）：把剪貼簿內容接在現有資料後面，
    /// 已經有的值和剪貼簿裡重複的值都略過；「重複項」視窗列出這次合併後被略過的值。
    /// </summary>
    public async Task AppendFromClipboardAsync(IClipboard? clipboard)
    {
        if (clipboard == null) return;
        var text = await clipboard.TryGetTextAsync();
        if (string.IsNullOrWhiteSpace(text)) return;
        AppendValues(PasteDataHelper.ParsePastedText(text));
    }

    /// <summary>把一批值接在現有資料後面（重複的略過），回傳實際加入的筆數。</summary>
    public int AppendValues(IReadOnlyList<string> pasted)
    {
        if (pasted.Count == 0) return 0;
        var combined = Items.Concat(pasted).ToList();
        var existing = new HashSet<string>(Items);
        var added = 0;
        foreach (var value in pasted)
        {
            if (!existing.Add(value)) continue;
            DataItems.Add(new DataItem(DataItems.Count + 1, value));
            Items.Add(value);
            added++;
        }
        SetDuplicates(combined);
        UpdateTopAndBottom();
        Avalonia.Threading.Dispatcher.UIThread.Post(() => TableScroll.ScrollToEnd(), Avalonia.Threading.DispatcherPriority.Background);

        var skipped = pasted.Count - added;
        EnqueueToast(skipped > 0
            ? string.Format(LanguageManager.GetText("AppendDoneSkippedToast", "已追加 {0} 筆，略過 {1} 筆重複"), added, skipped)
            : string.Format(LanguageManager.GetText("AppendDoneToast", "已追加 {0} 筆"), added),
            added == 0 ? ToastSeverity.Warning : ToastSeverity.Success);
        return added;
    }

    private void OnRowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Control { DataContext: DataItem item }) SelectRow(item);
    }

    /// <summary>選取一列（同時只選一列）。</summary>
    public void SelectRow(DataItem? item)
    {
        foreach (var other in DataItems.Where(x => x.IsSelected && x != item))
            other.IsSelected = false;
        if (item != null) item.IsSelected = true;
    }

    /// <summary>目前選取的列。</summary>
    public DataItem? SelectedItem => DataItems.FirstOrDefault(x => x.IsSelected);

    /// <summary>Insert：在選取的列下面插入一筆並直接輸入；沒有選取時加在最後面。</summary>
    public DataItem? InsertBelowSelected()
    {
        var selected = SelectedItem;
        return AddNewRow(selected == null ? DataItems.Count : DataItems.IndexOf(selected) + 1);
    }

    /// <summary>
    /// 在指定位置加一筆空白暫存列並進入編輯；確認時空白或重複就自動拿掉。
    /// 同一時間只會有一列空白暫存列：已經有一列時，同一個位置（例如連按「新增一筆」）直接聚焦那一列，
    /// 換到別的位置（例如在別列按 Insert）就把它移過去，不會越加越多。
    /// </summary>
    public DataItem? AddNewRow(int index)
    {
        // 正在編輯、已經輸入內容的那一筆先確認；還空白的暫存列留著，下面直接沿用
        foreach (var editing in DataItems.Where(x => x.IsEditing).ToList())
        {
            var text = ReferenceEquals(_activeEditor?.DataContext, editing) ? _activeEditor!.Text : editing.Value;
            if (editing.IsNew && string.IsNullOrWhiteSpace(text)) continue;
            CommitEdit(editing, text);
        }

        index = System.Math.Clamp(index, 0, DataItems.Count);
        var pending = DataItems.Where(x => x.IsNew).ToList();
        if (pending.Count > 0)
        {
            var keep = pending[0];
            // 多出來的空白暫存列（舊版留下或還沒進入編輯的）一律拿掉
            foreach (var extra in pending.Skip(1))
            {
                var extraIndex = DataItems.IndexOf(extra);
                if (extraIndex < index) index--;
                RemoveRowAt(extraIndex);
            }
            var keepIndex = DataItems.IndexOf(keep);
            // 插在暫存列的前面或後面都是同一個位置：直接用它
            if (index == keepIndex || index == keepIndex + 1)
            {
                SelectRow(keep);
                FocusNewRow(keep);
                return keep;
            }
            keep.IsEditing = false;
            if (ReferenceEquals(_activeEditor?.DataContext, keep)) { _activeEditor = null; _activeEditorFocused = false; }
            if (keepIndex < index) index--;
            RemoveRowAt(keepIndex);
        }

        var item = new DataItem(index + 1, string.Empty) { IsNew = true };
        DataItems.Insert(index, item);
        Items.Insert(index, string.Empty);
        for (int i = index + 1; i < DataItems.Count; i++)
            DataItems[i].RowNumber = i + 1;
        SelectRow(item);
        FocusNewRow(item);
        return item;
    }

    // 等新的一列畫出來，再找到它的輸入框開始編輯（已經在編輯就只把焦點移回去）
    private void FocusNewRow(DataItem item)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (!DataItems.Contains(item)) return;
            DataRows.ScrollIntoView(DataItems.IndexOf(item));
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (!DataItems.Contains(item)) return;
                var editor = DataRows.GetVisualDescendants().OfType<TextBox>()
                    .FirstOrDefault(t => t.Classes.Contains("cell-editor") && ReferenceEquals(t.DataContext, item));
                if (editor == null) return;
                if (item.IsEditing && ReferenceEquals(editor, _activeEditor)) editor.Focus();
                else BeginEdit(item, editor);
            }, Avalonia.Threading.DispatcherPriority.Background);
        }, Avalonia.Threading.DispatcherPriority.Background);
    }

    private void RemoveRowAt(int index)
    {
        if (index < 0 || index >= DataItems.Count) return;
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
        // Avalonia 12 的 TextChanged 是事後才發出（不在設定 Text 的當下），_suppressEvents 擋不住；
        // 程式自己寫進去的內容（例如刪除、編輯一筆後重新串接）不必再拆一次，否則資料表會整個重建、捲動位置跑掉
        if (text == _programmaticTopText) return;
        SetItems(SeparatorHelper.Split(text, _separator).Select(x => x.Trim()).Where(x => x.Length > 0));
        UpdateBottom();
    }

    // 最近一次由程式寫進 TopBox 的內容（見 TopBoxChanged）
    private string? _programmaticTopText;

    /// <summary>已確認的資料（排除正在新增、還沒輸入內容的空白暫存列）。</summary>
    private IEnumerable<string> CommittedItems => Items.Where(x => x.Length > 0);

    private void UpdateTopAndBottom()
    {
        _suppressEvents = true;
        try { TopBox.Text = _programmaticTopText = SeparatorHelper.Join(CommittedItems, _separator); }
        finally { _suppressEvents = false; }
        UpdateBottom();
    }

    private void UpdateBottom()
    {
        BottomBox.Text = PasteDataHelper.BuildSqlCondition(CommittedItems, _sqlOptions);
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
