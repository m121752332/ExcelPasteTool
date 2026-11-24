using ExcelPasteTool.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Linq;

namespace ExcelPasteTool;

/// <summary>設定頁的分類：Home 是第 2 層的分類清單，其餘是第 3 層的參數頁。</summary>
public enum SettingsCategory
{
    Home,
    General,
    Preferences,
    Update
}

public class SettingsViewModel : INotifyPropertyChanged
{
    public static SettingsViewModel Instance { get; } = new SettingsViewModel();
    
    public LocalizedText TitleText { get; } = new("SettingsTitle", "設定");
    public LocalizedText LanguageTitleText { get; } = new("LanguageLabel", "語言");
    public LocalizedText LanguageDescText { get; } = new("LanguageDesc", "變更 App 的語言設定");
    public LocalizedText ThemeTitleText { get; } = new("ThemeLabel", "主題");
    public LocalizedText ThemeDescText { get; } = new("ThemeDesc", "選擇 App 的配色主題");
    public LocalizedText FontTitleText { get; } = new("FontTitle", "字型");
    public LocalizedText FontDescText { get; } = new("FontDesc", "選擇介面與資料區使用的字型");
    public LocalizedText FontSizeTitleText { get; } = new("FontSizeTitle", "字體大小");
    public LocalizedText FontSizeDescText { get; } = new("FontSizeDesc", "調整整個 App 的文字大小");
    public LocalizedText FontPreviewText { get; } = new("FontPreviewText", "預覽：TIPTOP 料號 A-001 | 資料分隔 1234567890");
    public LocalizedText FontResetText { get; } = new("FontResetButton", "還原預設");
    public LocalizedText HelpViewerTitleText { get; } = new("HelpViewerTitle", "說明開啟方式");
    public LocalizedText HelpViewerDescText { get; } = new("HelpViewerDesc", "按 F1 或點選「說明」時，要在 App 內顯示還是用預設瀏覽器開啟");
    public LocalizedText TitleBarTitleText { get; } = new("TitleBarTitle", "精簡標題列");
    public LocalizedText TitleBarDescText { get; } = new("TitleBarDesc", "隱藏系統標題列，改用精簡的標題列，只保留側邊欄開關與最小化、最大化、關閉按鈕");
    public LocalizedText ToggleOnText { get; } = new("ToggleOn", "開啟");
    public LocalizedText ToggleOffText { get; } = new("ToggleOff", "關閉");

    // ===== 三層式導覽（設定 › 分類 › 參數）=====
    public LocalizedText GeneralCategoryText { get; } = new("SettingsGeneralTitle", "一般設置");
    public LocalizedText PreferencesCategoryText { get; } = new("SettingsPreferencesTitle", "使用者介面偏好設置");
    public LocalizedText UpdateCategoryText { get; } = new("SettingsUpdateTitle", "更新設置");
    public LocalizedText BackTooltipText { get; } = new("SettingsBackTooltip", "回到設定 (Alt+←)");
    public LocalizedText LanguageRowTitleText { get; } = new("SettingsLanguageTitle", "語言");
    public LocalizedText ThemeRowTitleText { get; } = new("SettingsThemeTitle", "主題");
    public LocalizedText CurrentVersionTitleText { get; } = new("SettingsCurrentVersionTitle", "目前版本");
    public LocalizedText CurrentVersionDescText { get; } = new("SettingsCurrentVersionDesc", "程式啟動時會自動檢查，也可以隨時手動檢查是否有新版本");
    public LocalizedText ChangelogTitleText { get; } = new("ChangelogTitle", "改版資訊");
    public LocalizedText ChangelogDescText { get; } = new("SettingsChangelogDesc", "查看每個版本更新了哪些內容");
    public LocalizedText ChangelogOpenText { get; } = new("SettingsChangelogOpen", "查看");

    private SettingsCategory _currentCategory = SettingsCategory.Home;

    /// <summary>目前停留的分類。存在單例裡，離開設定頁再回來會停在上次的分類（同 Win11）。</summary>
    public SettingsCategory CurrentCategory
    {
        get => _currentCategory;
        set
        {
            if (_currentCategory == value) return;
            _currentCategory = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsHome));
            OnPropertyChanged(nameof(IsCategoryPage));
            OnPropertyChanged(nameof(IsGeneral));
            OnPropertyChanged(nameof(IsPreferences));
            OnPropertyChanged(nameof(IsUpdate));
            OnPropertyChanged(nameof(CurrentCategoryTitle));
        }
    }

    public bool IsHome => _currentCategory == SettingsCategory.Home;
    public bool IsCategoryPage => _currentCategory != SettingsCategory.Home;
    public bool IsGeneral => _currentCategory == SettingsCategory.General;
    public bool IsPreferences => _currentCategory == SettingsCategory.Preferences;
    public bool IsUpdate => _currentCategory == SettingsCategory.Update;

    /// <summary>麵包屑第二段的文字（目前分類名稱）。</summary>
    public string CurrentCategoryTitle => _currentCategory switch
    {
        SettingsCategory.General => GeneralCategoryText.Text,
        SettingsCategory.Preferences => PreferencesCategoryText.Text,
        SettingsCategory.Update => UpdateCategoryText.Text,
        _ => TitleText.Text
    };

    public void NavigateTo(SettingsCategory category) => CurrentCategory = category;
    public void GoHome() => CurrentCategory = SettingsCategory.Home;

    private bool _isFontSizeExpanded;

    /// <summary>字體大小那一列是否展開（展開後才顯示滑桿、預覽與還原預設）。</summary>
    public bool IsFontSizeExpanded
    {
        get => _isFontSizeExpanded;
        set
        {
            if (_isFontSizeExpanded == value) return;
            _isFontSizeExpanded = value;
            OnPropertyChanged();
        }
    }

    /// <summary>測試用：固定顯示的版本號，避免每次升版都要重建基準圖。</summary>
    internal static string? VersionTextOverride { get; set; }

    public string CurrentVersionDisplay => $"v{VersionTextOverride ?? UpdateService.CurrentVersionText}";

    // 分類卡片上的摘要：不用點進去就看得到目前的值
    public string GeneralSummary => string.Join(" · ",
        _selectedLanguage,
        _selectedHelpViewer,
        $"{TitleBarTitleText.Text} {(CompactTitleBar ? ToggleOnText.Text : ToggleOffText.Text)}");

    public string PreferencesSummary => string.Join(" · ", _selectedTheme, _selectedFontFamily, FontSizeDisplay);

    public string UpdateSummary => string.Format(
        LanguageManager.GetText("UpdateCurrentVersion", "目前版本 v{0}"),
        VersionTextOverride ?? UpdateService.CurrentVersionText);

    private bool _isCheckingUpdate;

    /// <summary>手動檢查更新進行中（按鈕暫停使用並顯示「檢查中…」）。</summary>
    public bool IsCheckingUpdate
    {
        get => _isCheckingUpdate;
        set
        {
            if (_isCheckingUpdate == value) return;
            _isCheckingUpdate = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCheckUpdate));
            OnPropertyChanged(nameof(CheckUpdateButtonText));
        }
    }

    public bool CanCheckUpdate => !_isCheckingUpdate;

    public string CheckUpdateButtonText => _isCheckingUpdate
        ? LanguageManager.GetText("SettingsCheckingUpdate", "檢查中…")
        : LanguageManager.GetText("SettingsCheckUpdateButton", "檢查更新");

    private void NotifySummaries()
    {
        OnPropertyChanged(nameof(GeneralSummary));
        OnPropertyChanged(nameof(PreferencesSummary));
        OnPropertyChanged(nameof(UpdateSummary));
    }

    public double MinFontSize => FontManager.MinFontSize;
    public double MaxFontSize => FontManager.MaxFontSize;

    public ObservableCollection<string> Languages { get; } = new();
    public ObservableCollection<string> Themes { get; } = new();
    public ObservableCollection<string> FontFamilies { get; } = new();
    public ObservableCollection<string> HelpViewers { get; } = new();

    public Action<string>? EnqueueToast { get; set; }

    private string _selectedLanguage = "";
    private string _selectedTheme = "";
    private bool _isUpdating = false;
    
    public string SelectedLanguage
    {
        get
        {
            Debug.WriteLine($"SelectedLanguage getter: '{_selectedLanguage}'");
            return _selectedLanguage;
        }
        set
        {
            Debug.WriteLine($"SelectedLanguage setter: '{_selectedLanguage}' -> '{value}', isUpdating: {_isUpdating}");
            
            if (_selectedLanguage != value && !string.IsNullOrEmpty(value) && !_isUpdating)
            {
                _selectedLanguage = value;
                Debug.WriteLine($"SelectedLanguage changed to: '{_selectedLanguage}'");
                
                var langData = LanguageManager.GetAllLanguages().FirstOrDefault(l => l.Name == value);
                if (langData != default)
                {
                    Debug.WriteLine($"Found language data: {langData.Language}");
                    LanguageController.SetLanguage(langData.Language, true); // 設定頁面要保存
                    EnqueueToast?.Invoke(LanguageManager.GetText("LanguageChangedToast", "已將語言設定變更完成"));
                }
                
                OnPropertyChanged();
            }
        }
    }
    
    public string SelectedTheme
    {
        get
        {
            Debug.WriteLine($"SelectedTheme getter: '{_selectedTheme}'");
            return _selectedTheme;
        }
        set
        {
            Debug.WriteLine($"SelectedTheme setter: '{_selectedTheme}' -> '{value}', isUpdating: {_isUpdating}");
            
            if (_selectedTheme != value && !string.IsNullOrEmpty(value) && !_isUpdating)
            {
                _selectedTheme = value;
                Debug.WriteLine($"SelectedTheme changed to: '{_selectedTheme}'");
                
                var allThemes = ThemeManager.GetAllThemes();
                var selectedThemeData = allThemes.FirstOrDefault(t => t.Name == value);
                
                if (selectedThemeData != default)
                {
                    Debug.WriteLine($"Found theme data: {selectedThemeData.Theme}");
                    ThemeManager.CurrentTheme = selectedThemeData.Theme;
                    
                    ConfigServices.Config.Theme = selectedThemeData.Theme.ToString();
                    ConfigServices.Save();
                    
                    EnqueueToast?.Invoke(LanguageManager.GetText("ThemeChangedToast", "已將主題設定變更完成"));
                }
                
                OnPropertyChanged();
            }
        }
    }

    /// <summary>精簡標題列開關變更時通知主視窗切換（參數：是否為精簡標題列）。</summary>
    public event Action<bool>? CompactTitleBarChanged;

    /// <summary>是否使用精簡標題列，變更後立即套用並存檔。</summary>
    public bool CompactTitleBar
    {
        get => ConfigServices.Config.CompactTitleBar;
        set
        {
            if (ConfigServices.Config.CompactTitleBar == value) return;
            ConfigServices.Config.CompactTitleBar = value;
            ConfigServices.Save();
            CompactTitleBarChanged?.Invoke(value);
            EnqueueToast?.Invoke(LanguageManager.GetText("TitleBarChangedToast", "標題列設定已儲存"));
            OnPropertyChanged();
            NotifySummaries();
        }
    }

    private string _selectedHelpViewer = "";

    /// <summary>說明的開啟方式（內建視窗 / 預設瀏覽器），變更後立即存檔。</summary>
    public string SelectedHelpViewer
    {
        get => _selectedHelpViewer;
        set
        {
            if (_selectedHelpViewer == value || string.IsNullOrEmpty(value) || _isUpdating) return;
            _selectedHelpViewer = value;
            var selected = HelpService.GetAllViewerModes().FirstOrDefault(m => m.Name == value);
            if (selected != default)
            {
                HelpService.ViewerMode = selected.Mode;
                EnqueueToast?.Invoke(LanguageManager.GetText("HelpViewerChangedToast", "說明開啟方式已儲存"));
            }
            OnPropertyChanged();
            NotifySummaries();
        }
    }

    private void RefreshHelpViewers()
    {
        HelpViewers.Clear();
        foreach (var (_, name) in HelpService.GetAllViewerModes())
            HelpViewers.Add(name);
        _selectedHelpViewer = HelpService.GetViewerModeName(HelpService.ViewerMode);
    }

    private string _selectedFontFamily = "";

    /// <summary>下拉選單顯示用的字型名稱；第一項（內建字型）對應 config 中的空字串。</summary>
    public string SelectedFontFamily
    {
        get => _selectedFontFamily;
        set
        {
            if (_selectedFontFamily == value || string.IsNullOrEmpty(value) || _isUpdating) return;
            _selectedFontFamily = value;
            var fontKey = value == FontManager.BuiltInFontDisplayName ? FontManager.DefaultFontKey : value;
            FontManager.ApplyAndSave(fontKey, FontManager.CurrentFontSize);
            EnqueueToast?.Invoke(LanguageManager.GetText("FontChangedToast", "字型設定已儲存"));
            OnPropertyChanged();
            NotifySummaries();
        }
    }

    /// <summary>基本字體大小，變更後立即套用並存檔。</summary>
    public double FontSize
    {
        get => FontManager.CurrentFontSize;
        set
        {
            var size = FontManager.ClampSize(value);
            if (size == FontManager.CurrentFontSize) return;
            FontManager.ApplyAndSave(FontManager.CurrentFontFamily, size);
            OnPropertyChanged();
            OnPropertyChanged(nameof(FontSizeDisplay));
            NotifySummaries();
        }
    }

    public string FontSizeDisplay => $"{FontManager.CurrentFontSize:0} pt";

    public void IncreaseFontSize() => FontSize = FontManager.CurrentFontSize + 1;
    public void DecreaseFontSize() => FontSize = FontManager.CurrentFontSize - 1;

    /// <summary>字型與字體大小還原為預設值。</summary>
    public void ResetFont()
    {
        FontManager.ApplyAndSave(FontManager.DefaultFontKey, FontManager.DefaultFontSize);
        _selectedFontFamily = FontManager.BuiltInFontDisplayName;
        OnPropertyChanged(nameof(SelectedFontFamily));
        OnPropertyChanged(nameof(FontSize));
        OnPropertyChanged(nameof(FontSizeDisplay));
        NotifySummaries();
        EnqueueToast?.Invoke(LanguageManager.GetText("FontChangedToast", "字型設定已儲存"));
    }

    private void RefreshFontFamilies()
    {
        FontFamilies.Clear();
        FontFamilies.Add(FontManager.BuiltInFontDisplayName);
        foreach (var name in FontManager.GetSystemFontFamilies())
            FontFamilies.Add(name);

        var current = FontManager.CurrentFontFamily;
        if (string.IsNullOrEmpty(current))
        {
            _selectedFontFamily = FontManager.BuiltInFontDisplayName;
        }
        else
        {
            // 設定檔裡的字型這台電腦沒有時，仍保留在清單中，避免下拉選單顯示空白
            if (!FontFamilies.Contains(current))
                FontFamilies.Insert(1, current);
            _selectedFontFamily = current;
        }
    }

    private SettingsViewModel()
    {
        Debug.WriteLine("SettingsViewModel constructor started");
        
        // 初始化集合
        RefreshAllCollections();
        
        // 訂閱事件（在初始化之後）
        LanguageManager.LanguageChanged += OnLanguageOrThemeChanged;
        ThemeManager.ThemeChanged += OnLanguageOrThemeChanged;
        
        Debug.WriteLine($"SettingsViewModel constructor finished. Languages: {Languages.Count}, Themes: {Themes.Count}");
        Debug.WriteLine($"Selected Language: '{_selectedLanguage}', Selected Theme: '{_selectedTheme}'");
    }

    private void OnLanguageOrThemeChanged()
    {
        Debug.WriteLine("OnLanguageOrThemeChanged called");
        
        if (!_isUpdating)
        {
            Debug.WriteLine("Scheduling RefreshAllCollections on UI thread");
            // 使用 Dispatcher 確保在 UI 線程上執行
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                RefreshAllCollections();
            });
        }
        else
        {
            Debug.WriteLine("Skipping refresh because _isUpdating = true");
        }
    }

    private void RefreshAllCollections()
    {
        Debug.WriteLine("RefreshAllCollections started");
        _isUpdating = true;
        
        // 保存當前狀態
        var currentLanguage = LanguageManager.CurrentLanguage;
        var currentTheme = ThemeManager.CurrentTheme;
        
        Debug.WriteLine($"Current language: {currentLanguage}, Current theme: {currentTheme}");
        
        // 先清空選中項
        _selectedLanguage = "";
        _selectedTheme = "";
        _selectedFontFamily = "";
        _selectedHelpViewer = "";
        OnPropertyChanged(nameof(SelectedHelpViewer));
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(SelectedFontFamily));
        
        // 重新填充語言選項
        var oldLanguageCount = Languages.Count;
        Languages.Clear();
        OnPropertyChanged(nameof(Languages)); // 立即通知清空
        
        foreach (var (_, name) in LanguageManager.GetAllLanguages())
        {
            Languages.Add(name);
            Debug.WriteLine($"Added language: '{name}'");
        }
        Debug.WriteLine($"Languages: {oldLanguageCount} -> {Languages.Count}");
        OnPropertyChanged(nameof(Languages)); // 通知重新填充
        
        // 重新填充主題選項
        var oldThemeCount = Themes.Count;
        Themes.Clear();
        OnPropertyChanged(nameof(Themes)); // 立即通知清空
        
        foreach (var (_, name) in ThemeManager.GetAllThemes())
        {
            Themes.Add(name);
            Debug.WriteLine($"Added theme: '{name}'");
        }
        Debug.WriteLine($"Themes: {oldThemeCount} -> {Themes.Count}");
        OnPropertyChanged(nameof(Themes)); // 通知重新填充
        
        // 設定當前選中項
        var newLanguageName = LanguageManager.GetLanguageName(currentLanguage);
        var newThemeName = ThemeManager.GetThemeName(currentTheme);
        
        Debug.WriteLine($"New language name: '{newLanguageName}'");
        Debug.WriteLine($"New theme name: '{newThemeName}'");
        
        // 檢查選項是否存在
        var languageExists = Languages.Contains(newLanguageName);
        var themeExists = Themes.Contains(newThemeName);
        Debug.WriteLine($"Language exists: {languageExists}, Theme exists: {themeExists}");
        
        // 設定選中項
        _selectedLanguage = languageExists ? newLanguageName : (Languages.FirstOrDefault() ?? "");
        _selectedTheme = themeExists ? newThemeName : (Themes.FirstOrDefault() ?? "");
        
        Debug.WriteLine($"Final selected language: '{_selectedLanguage}'");
        Debug.WriteLine($"Final selected theme: '{_selectedTheme}'");
        
        RefreshFontFamilies();
        RefreshHelpViewers();

        _isUpdating = false;
        
        // 最後通知選中項更新
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(SelectedFontFamily));
        OnPropertyChanged(nameof(FontSize));
        OnPropertyChanged(nameof(FontSizeDisplay));
        
        OnPropertyChanged(nameof(SelectedHelpViewer));

        // 更新本地化文字
        OnPropertyChanged(nameof(TitleText));
        OnPropertyChanged(nameof(LanguageTitleText));
        OnPropertyChanged(nameof(LanguageDescText));
        OnPropertyChanged(nameof(ThemeTitleText));
        OnPropertyChanged(nameof(CurrentCategoryTitle));
        OnPropertyChanged(nameof(CheckUpdateButtonText));
        NotifySummaries();
        
        Debug.WriteLine("RefreshAllCollections finished");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        Debug.WriteLine($"OnPropertyChanged: {name}");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}