using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input.Platform;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
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
        new('|', "豎線 |"),
        new(';', "分號 ;"),
        new(':', "冒號 :"),
        new(' ', "空白")
    };

    private ComboBox? _themeSelector;
    private ComboBox? _languageSelector;
    private ComboBox? _separatorComboBox;
    private TextBox? _topBox;
    private TextBox? _bottomBox;
    private TextBlock? _separatorLabel;
    private TextBlock? _languageLabel;
    private TextBlock? _themeLabel;
    private TextBlock? _pipDataTitle;
    private TextBlock? _sqlConditionTitle;
    private TextBlock? _dataListTitle;
    private Button? _pasteDataButton;
    private Button? _clearDataButton;
    private TextBlock? _pasteButtonText;
    private TextBlock? _clearButtonText;
    private TextBlock? _rowNumberHeader;
    private TextBlock? _dataContentHeader;
    private ToastQueueHelper _toastHelper;

    public DataToolView()
    {
        InitializeComponent();
        DataContext = this;
        _toastHelper = new ToastQueueHelper(this);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _themeSelector = this.FindControl<ComboBox>("ThemeSelector");
        _languageSelector = this.FindControl<ComboBox>("LanguageSelector");
        _separatorComboBox = this.FindControl<ComboBox>("SeparatorComboBox");
        _topBox = this.FindControl<TextBox>("TopBox");
        _bottomBox = this.FindControl<TextBox>("BottomBox");
        _separatorLabel = this.FindControl<TextBlock>("SeparatorLabel");
        _languageLabel = this.FindControl<TextBlock>("LanguageLabel");
        _themeLabel = this.FindControl<TextBlock>("ThemeLabel");
        _pipDataTitle = this.FindControl<TextBlock>("PipDataTitle");
        _sqlConditionTitle = this.FindControl<TextBlock>("SqlConditionTitle");
        _dataListTitle = this.FindControl<TextBlock>("DataListTitle");
        _pasteDataButton = this.FindControl<Button>("PasteDataButton");
        _clearDataButton = this.FindControl<Button>("ClearDataButton");
        _pasteButtonText = this.FindControl<TextBlock>("PasteButtonText");
        _clearButtonText = this.FindControl<TextBlock>("ClearButtonText");
        _rowNumberHeader = this.FindControl<TextBlock>("RowNumberHeader");
        _dataContentHeader = this.FindControl<TextBlock>("DataContentHeader");

        InitializeLanguageSelector();
        InitializeSeparatorSelector();
        InitializeThemeSelector();

        if (_themeSelector != null) _themeSelector.SelectionChanged += ThemeSelector_SelectionChanged;
        if (_languageSelector != null) _languageSelector.SelectionChanged += LanguageSelector_SelectionChanged;
        if (_separatorComboBox != null) _separatorComboBox.SelectionChanged += SeparatorComboBox_SelectionChanged;
        if (_topBox != null) _topBox.TextChanged += TopBoxChanged;
        if (_pasteDataButton != null) _pasteDataButton.Click += PasteData_Click;
        if (_clearDataButton != null) _clearDataButton.Click += ClearData_Click;
        var copyTop = this.FindControl<Button>("CopyTopBoxButton");
        if (copyTop != null) copyTop.Click += CopyTopBoxButton_Click;
        var copyBottom = this.FindControl<Button>("CopyBottomBoxButton");
        if (copyBottom != null) copyBottom.Click += CopyBottomBoxButton_Click;

        ThemeManager.ThemeChanged += OnThemeChanged;
        LanguageManager.LanguageChanged += OnLanguageChanged;
        FontManager.FontChanged += OnFontChanged;
        UpdateUITexts();
        FontManager.ApplyLanguageFont(LanguageManager.CurrentLanguage);
    }

    private void InitializeLanguageSelector()
    {
        if (_languageSelector != null)
        {
            _languageSelector.Items.Clear();
            var allLanguages = LanguageManager.GetAllLanguages();
            foreach (var (language, name) in allLanguages)
                _languageSelector.Items.Add(name);
            UpdateLanguageSelection();
        }
    }
    private void InitializeSeparatorSelector()
    {
        if (_separatorComboBox != null)
        {
            _separatorComboBox.Items.Clear();
            foreach (var option in _separatorOptions)
                _separatorComboBox.Items.Add(option.DisplayName);
            _separatorComboBox.SelectedIndex = 0;
            _separator = _defaultSeparator;
        }
    }
    private void InitializeThemeSelector()
    {
        if (_themeSelector != null)
        {
            _themeSelector.Items.Clear();
            var allThemes = ThemeManager.GetAllThemes();
            foreach (var (theme, name) in allThemes)
                _themeSelector.Items.Add(name);
            UpdateThemeSelection();
        }
    }
    private void UpdateLanguageSelection()
    {
        if (_languageSelector != null)
        {
            var allLanguages = LanguageManager.GetAllLanguages();
            var currentLanguageIndex = allLanguages.FindIndex(l => l.Language == LanguageManager.CurrentLanguage);
            if (currentLanguageIndex >= 0)
                _languageSelector.SelectedIndex = currentLanguageIndex;
        }
    }
    private void UpdateThemeSelection()
    {
        if (_themeSelector != null)
        {
            var allThemes = ThemeManager.GetAllThemes();
            var currentThemeIndex = allThemes.FindIndex(t => t.Theme == ThemeManager.CurrentTheme);
            if (currentThemeIndex >= 0)
                _themeSelector.SelectedIndex = currentThemeIndex;
        }
    }
    private void UpdateUITexts()
    {
        if (_separatorLabel != null)
            _separatorLabel.Text = LanguageManager.GetText("SeparatorLabel", "分隔符號:");
        if (_languageLabel != null)
            _languageLabel.Text = LanguageManager.GetText("LanguageLabel", "語言:");
        if (_themeLabel != null)
            _themeLabel.Text = LanguageManager.GetText("ThemeLabel", "主題:");
        if (_pipDataTitle != null)
            _pipDataTitle.Text = LanguageManager.GetText("PipDataTitle", "PIP 分隔資料");
        if (_sqlConditionTitle != null)
            _sqlConditionTitle.Text = LanguageManager.GetText("SqlConditionTitle", "SQL 條件");
        if (_dataListTitle != null)
            _dataListTitle.Text = LanguageManager.GetText("DataListTitle", "資料清單");
        if (_pasteButtonText != null)
            _pasteButtonText.Text = LanguageManager.GetText("PasteDataButton", "資料貼上");
        if (_clearButtonText != null)
            _clearButtonText.Text = LanguageManager.GetText("ClearDataButton", "清空資料");
        if (_rowNumberHeader != null)
            _rowNumberHeader.Text = LanguageManager.GetText("RowNumberHeader", "行號");
        if (_dataContentHeader != null)
            _dataContentHeader.Text = LanguageManager.GetText("DataContentHeader", "資料內容");
        UpdateSeparatorOptions();
        UpdateThemeOptions();
    }
    private void UpdateSeparatorOptions()
    {
        if (_separatorComboBox != null)
        {
            var currentIndex = _separatorComboBox.SelectedIndex;
            _separatorComboBox.Items.Clear();
            var separatorOptions = new List<string>
            {
                LanguageManager.GetText("SeparatorPipe", "豎線 |"),
                LanguageManager.GetText("SeparatorSemicolon", "分號 ;"),
                LanguageManager.GetText("SeparatorColon", "冒號 :"),
                LanguageManager.GetText("SeparatorSpace", "空白")
            };
            foreach (var option in separatorOptions)
                _separatorComboBox.Items.Add(option);
            if (currentIndex >= 0 && currentIndex < separatorOptions.Count)
                _separatorComboBox.SelectedIndex = currentIndex;
        }
    }
    private void UpdateThemeOptions()
    {
        if (_themeSelector != null)
        {
            var currentIndex = _themeSelector.SelectedIndex;
            _themeSelector.Items.Clear();
            var themeOptions = new List<string>
            {
                LanguageManager.GetText("ThemeDark", "暗黑模式"),
                LanguageManager.GetText("ThemeLight", "光線模式")
            };
            foreach (var option in themeOptions)
                _themeSelector.Items.Add(option);
            if (currentIndex >= 0 && currentIndex < themeOptions.Count)
                _themeSelector.SelectedIndex = currentIndex;
        }
    }
    private void OnLanguageChanged()
    {
        UpdateLanguageSelection();
        UpdateUITexts();
    }
    private void OnThemeChanged()
    {
        UpdateThemeSelection();
        var app = Avalonia.Application.Current;
        if (app != null && app.Resources != null)
        {
            var brushKey = "EvenRowBackgroundBrush";
            var isDark = ThemeManager.CurrentTheme.ToString().ToLower().Contains("dark");
            var color = isDark ? "#29292B" : "#f5f8fd";
            app.Resources[brushKey] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(color));
            var sidebarColor = isDark ? "#232323" : "#F7F7F7";
            app.Resources["SidebarBackgroundBrush"] = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse(sidebarColor));
        }
    }
    private void OnFontChanged() { }
    private void LanguageSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_languageSelector != null && _languageSelector.SelectedIndex >= 0)
        {
            var allLanguages = LanguageManager.GetAllLanguages();
            if (_languageSelector.SelectedIndex < allLanguages.Count)
            {
                var selectedLanguage = allLanguages[_languageSelector.SelectedIndex].Language;
                if (LanguageManager.CurrentLanguage != selectedLanguage)
                {
                    LanguageManager.CurrentLanguage = selectedLanguage;
                    ConfigServices.Config.Language = selectedLanguage.ToString();
                    ConfigServices.Save();
                    FontManager.ApplyLanguageFont(selectedLanguage);
                }
            }
        }
    }
    private void ThemeSelector_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_themeSelector != null && _themeSelector.SelectedIndex >= 0)
        {
            var allThemes = ThemeManager.GetAllThemes();
            if (_themeSelector.SelectedIndex < allThemes.Count)
            {
                var selectedTheme = allThemes[_themeSelector.SelectedIndex].Theme;
                if (ThemeManager.CurrentTheme != selectedTheme)
                {
                    ThemeManager.CurrentTheme = selectedTheme;
                    ConfigServices.Config.Theme = selectedTheme.ToString();
                    ConfigServices.Save();
                }
            }
        }
    }
    private void SeparatorComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_separatorComboBox != null && _separatorComboBox.SelectedIndex >= 0)
        {
            var selectedIndex = _separatorComboBox.SelectedIndex;
            if (selectedIndex < _separatorOptions.Count)
            {
                var newSeparator = _separatorOptions[selectedIndex].Character;
                if (_separator != newSeparator)
                {
                    _separator = newSeparator;
                    UpdateTopAndBottom();
                }
            }
        }
    }
    private async void PasteData_Click(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            var text = await clipboard.GetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var lines = text.Split(new[] {'\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                DataItems.Clear();
                Items.Clear();
                var processedData = new List<string>();
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        var firstColumn = GetFirstColumn(trimmedLine);
                        processedData.Add(firstColumn);
                    }
                }
                var uniqueData = processedData.Distinct().ToList();
                int rowNumber = 1;
                foreach (var data in uniqueData)
                {
                    DataItems.Add(new DataItem(rowNumber, data));
                    Items.Add(data);
                    rowNumber++;
                }
                UpdateTopAndBottom();
            }
        }
    }
    private string GetFirstColumn(string line)
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
        DataItems.Clear();
        Items.Clear();
        if (_topBox != null)
            _topBox.Text = "";
        if (_bottomBox != null)
            _bottomBox.Text = "";
        ResetSeparatorToDefault();
        EnqueueToast("畫面已清理回初始狀態");
    }
    private void ResetSeparatorToDefault()
    {
        _separator = _defaultSeparator;
        if (_separatorComboBox != null)
            _separatorComboBox.SelectedIndex = 0;
    }
    private void TopBoxChanged(object? sender, TextChangedEventArgs e)
    {
        var text = _topBox?.Text ?? string.Empty;
        var parts = text.Split(_separator).Select(x => x.Trim()).Where(x => x.Length > 0);
        DataItems.Clear();
        Items.Clear();
        var uniqueParts = parts.Distinct().ToList();
        int rowNumber = 1;
        foreach (var part in uniqueParts)
        {
            DataItems.Add(new DataItem(rowNumber, part));
            Items.Add(part);
            rowNumber++;
        }
        UpdateBottom();
    }
    private void UpdateTopAndBottom()
    {
        if (_topBox != null)
            _topBox.Text = string.Join(_separator, Items);
        UpdateBottom();
    }
    private void UpdateBottom()
    {
        if (_bottomBox != null)
            _bottomBox.Text = $"({string.Join(", ", Items.Select(x => $"'{x}'"))})";
    }
    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        ThemeManager.ThemeChanged -= OnThemeChanged;
        LanguageManager.LanguageChanged -= OnLanguageChanged;
        FontManager.FontChanged -= OnFontChanged;
        base.OnDetachedFromVisualTree(e);
    }
    private void EnqueueToast(string message)
    {
        _toastHelper.EnqueueToast(message);
    }
    private async void CopyTopBoxButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_topBox != null && !string.IsNullOrEmpty(_topBox.Text))
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                var window = this.GetVisualRoot() as Window;
                if (window != null)
                    window.Activate();
                await Task.Delay(50);
                await clipboard.SetTextAsync(_topBox.Text);
                var check = await clipboard.GetTextAsync();
                if (check == _topBox.Text)
                    EnqueueToast("已複製到剪貼簿！");
                else
                    EnqueueToast("複製失敗，請重試");
            }
        }
    }
    private async void CopyBottomBoxButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_bottomBox != null && !string.IsNullOrEmpty(_bottomBox.Text))
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                var window = this.GetVisualRoot() as Window;
                if (window != null)
                    window.Activate();
                await Task.Delay(50);
                await clipboard.SetTextAsync(_bottomBox.Text);
                var check = await clipboard.GetTextAsync();
                if (check == _bottomBox.Text)
                    EnqueueToast("已複製到剪貼簿！");
                else
                    EnqueueToast("複製失敗，請重試");
            }
        }
    }
    private async void CopyTableButton_Click(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null && DataItems.Count > 0)
        {
            var text = string.Join("\r\n", DataItems.Select(x => x.Value));
            await clipboard.SetTextAsync(text);
            EnqueueToast("已複製到剪貼簿！");
        }
    }
    public class SeparatorOption
    {
        public char Character { get; }
        public string DisplayName { get; }
        public SeparatorOption(char character, string displayName)
        {
            Character = character;
            DisplayName = displayName;
        }
    }
}
