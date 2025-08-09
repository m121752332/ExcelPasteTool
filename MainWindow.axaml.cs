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
    private char _separator = '|';  // �w�]�ݽu
    private readonly char _defaultSeparator = '|';  // �O��w�]���j�Ÿ�

    // ���j�Ÿ��ﶵ�C��
    private readonly List<SeparatorOption> _separatorOptions = new()
    {
        new('|', "�ݽu |"),
        new(';', "���� ;"),
        new(':', "�_�� :"),
        new(' ', "�ť�")  // �ťզr��
    };

    // ����Ѧ�
    private ComboBox? _themeSelector;
    private ComboBox? _languageSelector;
    private ComboBox? _separatorComboBox;
    private TextBox? _topBox;
    private TextBox? _bottomBox;

    // UI �����Ѧҡ]�Ω󥻦a�ơ^
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
        
        // ��l�ƻy���޲z��
        LanguageManager.Initialize();
        
        // �b���J��M�䱱�
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // �M�䱱�
        _themeSelector = this.FindControl<ComboBox>("ThemeSelector");
        _languageSelector = this.FindControl<ComboBox>("LanguageSelector");
        _separatorComboBox = this.FindControl<ComboBox>("SeparatorComboBox");
        _topBox = this.FindControl<TextBox>("TopBox");
        _bottomBox = this.FindControl<TextBox>("BottomBox");

        // �M��UI����
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

        // ��l�ƿ�ܾ�
        InitializeLanguageSelector();
        InitializeSeparatorSelector();
        InitializeThemeSelector();
        
        // �q�\�ƥ�
        ThemeManager.ThemeChanged += OnThemeChanged;
        LanguageManager.LanguageChanged += OnLanguageChanged;
        FontManager.FontChanged += OnFontChanged;

        // ��l��UI��r
        UpdateUITexts();
        
        // ���Ϊ�l�r��
        FontManager.ApplyLanguageFont(LanguageManager.CurrentLanguage);
    }

    private void InitializeLanguageSelector()
    {
        if (_languageSelector != null)
        {
            _languageSelector.Items.Clear();
            
            // �K�[�Ҧ��i�λy��
            var allLanguages = LanguageManager.GetAllLanguages();
            foreach (var (language, name) in allLanguages)
            {
                _languageSelector.Items.Add(name);
            }
            
            // �]�m��e�襤���y��
            UpdateLanguageSelection();
        }
    }

    private void InitializeSeparatorSelector()
    {
        if (_separatorComboBox != null)
        {
            _separatorComboBox.Items.Clear();
            
            // �K�[�Ҧ����j�Ÿ��ﶵ
            foreach (var option in _separatorOptions)
            {
                _separatorComboBox.Items.Add(option.DisplayName);
            }
            
            // �]�m�w�]�ﶵ�]�ݽu |�^
            _separatorComboBox.SelectedIndex = 0;
            _separator = _defaultSeparator;
        }
    }

    private void InitializeThemeSelector()
    {
        if (_themeSelector != null)
        {
            _themeSelector.Items.Clear();
            
            // �K�[�Ҧ��i�ΥD�D
            var allThemes = ThemeManager.GetAllThemes();
            foreach (var (theme, name) in allThemes)
            {
                _themeSelector.Items.Add(name);
            }
            
            // �]�m��e�襤���D�D
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
        // ��s�������D
        Title = LanguageManager.GetText("AppTitle", "��Ƥ��j�B�z��");

        // ��s���Ҥ�r
        if (_separatorLabel != null)
            _separatorLabel.Text = LanguageManager.GetText("SeparatorLabel", "���j�Ÿ��G");
        
        if (_languageLabel != null)
            _languageLabel.Text = LanguageManager.GetText("LanguageLabel", "�y��:");
        
        if (_themeLabel != null)
            _themeLabel.Text = LanguageManager.GetText("ThemeLabel", "�D�D:");
        
        if (_pipDataTitle != null)
            _pipDataTitle.Text = LanguageManager.GetText("PipDataTitle", "PIP ���j���");
        
        if (_sqlConditionTitle != null)
            _sqlConditionTitle.Text = LanguageManager.GetText("SqlConditionTitle", "SQL ����");
        
        if (_dataListTitle != null)
            _dataListTitle.Text = LanguageManager.GetText("DataListTitle", "��ƲM��");
        
        if (_pasteButtonText != null)
            _pasteButtonText.Text = LanguageManager.GetText("PasteDataButton", "��ƶK�W");
        
        if (_clearButtonText != null)
            _clearButtonText.Text = LanguageManager.GetText("ClearDataButton", "�M�Ÿ��");
        
        if (_rowNumberHeader != null)
            _rowNumberHeader.Text = LanguageManager.GetText("RowNumberHeader", "�渹");
        
        if (_dataContentHeader != null)
            _dataContentHeader.Text = LanguageManager.GetText("DataContentHeader", "��Ƥ��e");

        // ��s���j�Ÿ��ﶵ
        UpdateSeparatorOptions();
        
        // ��s�D�D�ﶵ
        UpdateThemeOptions();
    }

    private void UpdateSeparatorOptions()
    {
        if (_separatorComboBox != null)
        {
            var currentIndex = _separatorComboBox.SelectedIndex;
            _separatorComboBox.Items.Clear();
            
            // �ھڷ�e�y����s���j�Ÿ��ﶵ
            var separatorOptions = new List<string>
            {
                LanguageManager.GetText("SeparatorPipe", "�ݽu |"),
                LanguageManager.GetText("SeparatorSemicolon", "���� ;"),
                LanguageManager.GetText("SeparatorColon", "�_�� :"),
                LanguageManager.GetText("SeparatorSpace", "�ť�")
            };

            foreach (var option in separatorOptions)
            {
                _separatorComboBox.Items.Add(option);
            }
            
            // ��_���
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
            
            // �ھڷ�e�y����s�D�D�ﶵ
            var themeOptions = new List<string>
            {
                LanguageManager.GetText("ThemeDark", "�t�¼Ҧ�"),
                LanguageManager.GetText("ThemeModern", "�{�N�ƥD�D")
            };

            foreach (var option in themeOptions)
            {
                _themeSelector.Items.Add(option);
            }
            
            // ��_���
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
        // �r���ܧ�᪺�B�z
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
                
                // �M�Ų{�����
                DataItems.Clear();
                Items.Clear();
                
                // �B�z�C�@��A�p�G���h��u���Ĥ@��A�æ����Ҧ����
                var processedData = new List<string>();
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        // �p�G���h��]�� Tab �Ψ�L���j�Ť��j�^�A�u���Ĥ@��
                        var firstColumn = GetFirstColumn(trimmedLine);
                        processedData.Add(firstColumn);
                    }
                }
                
                // �h�����ƭȨëO�����ǡ]�ϥ� Distinct �|�O���Ĥ@���X�{�����ǡ^
                var uniqueData = processedData.Distinct().ToList();
                
                // �K�[�h���᪺��ƨ춰�X��
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
        // ���եα`�������j�ŨӤ��ΡA���Ĥ@��
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
        
        // �p�G�S�������j�šA��^���
        return line.Trim();
    }

    private void ClearData_Click(object? sender, RoutedEventArgs e)
    {
        // �M�ũҦ���ơA�^�����ε{����l���A
        DataItems.Clear();
        Items.Clear();
        
        // ���m��J��
        if (_topBox != null)
            _topBox.Text = "";
        
        if (_bottomBox != null)
            _bottomBox.Text = "";
        
        // ���m���j�Ÿ����w�]�ݽu
        ResetSeparatorToDefault();
    }

    private void ResetSeparatorToDefault()
    {
        // ���m���j�Ÿ����w�]��
        _separator = _defaultSeparator;
        if (_separatorComboBox != null)
        {
            _separatorComboBox.SelectedIndex = 0; // ��ܲĤ@�ӿﶵ�]�ݽu |�^
        }
    }

    private void TopBoxChanged(object? sender, TextChangedEventArgs e)
    {
        var text = _topBox?.Text ?? string.Empty;
        var parts = text.Split(_separator).Select(x => x.Trim()).Where(x => x.Length > 0);
        
        // �M�Ų{�����
        DataItems.Clear();
        Items.Clear();
        
        // ���ʿ�J����Ƥ]�i��h���B�z
        var uniqueParts = parts.Distinct().ToList();
        
        // ���s�إ߸��
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
        // �����q�\�ƥ�
        ThemeManager.ThemeChanged -= OnThemeChanged;
        LanguageManager.LanguageChanged -= OnLanguageChanged;
        FontManager.FontChanged -= OnFontChanged;
        base.OnClosed(e);
    }
}

// ���j�Ÿ��ﶵ���O
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