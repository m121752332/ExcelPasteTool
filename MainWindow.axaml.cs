using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input.Platform;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace ExcelPasteTool;

public partial class MainWindow : Window
{
    public ObservableCollection<DataItem> DataItems { get; } = new();
    public ObservableCollection<string> Items { get; } = new();
    private char _separator = '|';  // 預設豎線
    private readonly char _defaultSeparator = '|';  // 記住預設分隔符號

    // 分隔符號選項列表
    private readonly List<SeparatorOption> _separatorOptions = new()
    {
        new('|', "豎線 |"),
        new(';', "分號 ;"),
        new(':', "冒號 :"),
        new(' ', "空白")  // 空白字元
    };

    // 控制項參考
    private ComboBox? _themeSelector;
    private ComboBox? _languageSelector;
    private ComboBox? _separatorComboBox;
    private TextBox? _topBox;
    private TextBox? _bottomBox;

    // UI 元素參考（用於本地化）
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

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        
        // 初始化語言管理器
        LanguageManager.Initialize();
        
        // 在載入後尋找控制項
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // 尋找控制項
        _themeSelector = this.FindControl<ComboBox>("ThemeSelector");
        _languageSelector = this.FindControl<ComboBox>("LanguageSelector");
        _separatorComboBox = this.FindControl<ComboBox>("SeparatorComboBox");
        _topBox = this.FindControl<TextBox>("TopBox");
        _bottomBox = this.FindControl<TextBox>("BottomBox");

        // 尋找UI元素
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

        // 初始化選擇器
        InitializeLanguageSelector();
        InitializeSeparatorSelector();
        InitializeThemeSelector();
        
        // 訂閱事件
        ThemeManager.ThemeChanged += OnThemeChanged;
        LanguageManager.LanguageChanged += OnLanguageChanged;
        FontManager.FontChanged += OnFontChanged;

        // 初始化UI文字
        UpdateUITexts();
        
        // 應用初始字型
        FontManager.ApplyLanguageFont(LanguageManager.CurrentLanguage);
    }

    private void InitializeLanguageSelector()
    {
        if (_languageSelector != null)
        {
            _languageSelector.Items.Clear();
            
            // 添加所有可用語言
            var allLanguages = LanguageManager.GetAllLanguages();
            foreach (var (language, name) in allLanguages)
            {
                _languageSelector.Items.Add(name);
            }
            
            // 設置當前選中的語言
            UpdateLanguageSelection();
        }
    }

    private void InitializeSeparatorSelector()
    {
        if (_separatorComboBox != null)
        {
            _separatorComboBox.Items.Clear();
            
            // 添加所有分隔符號選項
            foreach (var option in _separatorOptions)
            {
                _separatorComboBox.Items.Add(option.DisplayName);
            }
            
            // 設置預設選項（豎線 |）
            _separatorComboBox.SelectedIndex = 0;
            _separator = _defaultSeparator;
        }
    }

    private void InitializeThemeSelector()
    {
        if (_themeSelector != null)
        {
            _themeSelector.Items.Clear();
            
            // 添加所有可用主題
            var allThemes = ThemeManager.GetAllThemes();
            foreach (var (theme, name) in allThemes)
            {
                _themeSelector.Items.Add(name);
            }
            
            // 設置當前選中的主題
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
            {
                _languageSelector.SelectedIndex = currentLanguageIndex;
            }
        }
    }

    private void UpdateThemeSelection()
    {
        if (_themeSelector != null)
        {
            var allThemes = ThemeManager.GetAllThemes();
            var currentThemeIndex = allThemes.FindIndex(t => t.Theme == ThemeManager.CurrentTheme);
            if (currentThemeIndex >= 0)
            {
                _themeSelector.SelectedIndex = currentThemeIndex;
            }
        }
    }

    private void UpdateUITexts()
    {
        // 更新視窗標題
        Title = LanguageManager.GetText("AppTitle", "資料分隔處理器");

        // 更新標籤文字
        if (_separatorLabel != null)
            _separatorLabel.Text = LanguageManager.GetText("SeparatorLabel", "分隔符號：");
        
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

        // 更新分隔符號選項
        UpdateSeparatorOptions();
        
        // 更新主題選項
        UpdateThemeOptions();
    }

    private void UpdateSeparatorOptions()
    {
        if (_separatorComboBox != null)
        {
            var currentIndex = _separatorComboBox.SelectedIndex;
            _separatorComboBox.Items.Clear();
            
            // 根據當前語言更新分隔符號選項
            var separatorOptions = new List<string>
            {
                LanguageManager.GetText("SeparatorPipe", "豎線 |"),
                LanguageManager.GetText("SeparatorSemicolon", "分號 ;"),
                LanguageManager.GetText("SeparatorColon", "冒號 :"),
                LanguageManager.GetText("SeparatorSpace", "空白")
            };

            foreach (var option in separatorOptions)
            {
                _separatorComboBox.Items.Add(option);
            }
            
            // 恢復選擇
            if (currentIndex >= 0 && currentIndex < separatorOptions.Count)
            {
                _separatorComboBox.SelectedIndex = currentIndex;
            }
        }
    }

    private void UpdateThemeOptions()
    {
        if (_themeSelector != null)
        {
            var currentIndex = _themeSelector.SelectedIndex;
            _themeSelector.Items.Clear();
            
            // 根據當前語言更新主題選項
            var themeOptions = new List<string>
            {
                LanguageManager.GetText("ThemeDark", "暗黑模式"),
                LanguageManager.GetText("ThemeModern", "現代化主題")
            };

            foreach (var option in themeOptions)
            {
                _themeSelector.Items.Add(option);
            }
            
            // 恢復選擇
            if (currentIndex >= 0 && currentIndex < themeOptions.Count)
            {
                _themeSelector.SelectedIndex = currentIndex;
            }
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
    }

    private void OnFontChanged()
    {
        // 字型變更後的處理
    }

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
                
                // 清空現有資料
                DataItems.Clear();
                Items.Clear();
                
                // 處理每一行，如果有多欄只取第一欄，並收集所有資料
                var processedData = new List<string>();
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        // 如果有多欄（用 Tab 或其他分隔符分隔），只取第一欄
                        var firstColumn = GetFirstColumn(trimmedLine);
                        processedData.Add(firstColumn);
                    }
                }
                
                // 去除重複值並保持順序（使用 Distinct 會保持第一次出現的順序）
                var uniqueData = processedData.Distinct().ToList();
                
                // 添加去重後的資料到集合中
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
        // 嘗試用常見的分隔符來分割，取第一欄
        var separators = new char[] { '\t', ',', ';', '|' };
        
        foreach (var sep in separators)
        {
            if (line.Contains(sep))
            {
                var parts = line.Split(new char[] { sep }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    return parts[0].Trim();
                }
            }
        }
        
        // 如果沒有找到分隔符，返回整行
        return line.Trim();
    }

    private void ClearData_Click(object? sender, RoutedEventArgs e)
    {
        // 清空所有資料，回到應用程式初始狀態
        DataItems.Clear();
        Items.Clear();
        
        // 重置輸入框
        if (_topBox != null)
            _topBox.Text = "";
        
        if (_bottomBox != null)
            _bottomBox.Text = "";
        
        // 重置分隔符號為預設豎線
        ResetSeparatorToDefault();
    }

    private void ResetSeparatorToDefault()
    {
        // 重置分隔符號為預設值
        _separator = _defaultSeparator;
        if (_separatorComboBox != null)
        {
            _separatorComboBox.SelectedIndex = 0; // 選擇第一個選項（豎線 |）
        }
    }

    private void TopBoxChanged(object? sender, TextChangedEventArgs e)
    {
        var text = _topBox?.Text ?? string.Empty;
        var parts = text.Split(_separator).Select(x => x.Trim()).Where(x => x.Length > 0);
        
        // 清空現有資料
        DataItems.Clear();
        Items.Clear();
        
        // 對手動輸入的資料也進行去重處理
        var uniqueParts = parts.Distinct().ToList();
        
        // 重新建立資料
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

    protected override void OnClosed(System.EventArgs e)
    {
        // 取消訂閱事件
        ThemeManager.ThemeChanged -= OnThemeChanged;
        LanguageManager.LanguageChanged -= OnLanguageChanged;
        FontManager.FontChanged -= OnFontChanged;
        base.OnClosed(e);
    }
}

// 分隔符號選項類別
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