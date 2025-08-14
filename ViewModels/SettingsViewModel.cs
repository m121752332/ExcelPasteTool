using ExcelPasteTool.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Linq;

namespace ExcelPasteTool;

public class SettingsViewModel : INotifyPropertyChanged
{
    public static SettingsViewModel Instance { get; } = new SettingsViewModel();
    
    public LocalizedText TitleText { get; } = new("SettingsTitle", "設定");
    public LocalizedText LanguageTitleText { get; } = new("LanguageLabel", "語言");
    public LocalizedText LanguageDescText { get; } = new("LanguageDesc", "變更 App 的語言設定");
    public LocalizedText ThemeTitleText { get; } = new("ThemeLabel", "主題");

    public ObservableCollection<string> Languages { get; } = new();
    public ObservableCollection<string> Themes { get; } = new();

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
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(SelectedTheme));
        
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
        
        _isUpdating = false;
        
        // 最後通知選中項更新
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(SelectedTheme));
        
        // 更新本地化文字
        OnPropertyChanged(nameof(TitleText));
        OnPropertyChanged(nameof(LanguageTitleText));
        OnPropertyChanged(nameof(LanguageDescText));
        OnPropertyChanged(nameof(ThemeTitleText));
        
        Debug.WriteLine("RefreshAllCollections finished");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        Debug.WriteLine($"OnPropertyChanged: {name}");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}