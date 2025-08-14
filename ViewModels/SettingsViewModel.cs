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
    
    public LocalizedText TitleText { get; } = new("SettingsTitle", "�]�w");
    public LocalizedText LanguageTitleText { get; } = new("LanguageLabel", "�y��");
    public LocalizedText LanguageDescText { get; } = new("LanguageDesc", "�ܧ� App ���y���]�w");
    public LocalizedText ThemeTitleText { get; } = new("ThemeLabel", "�D�D");

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
                    LanguageController.SetLanguage(langData.Language, true); // �]�w�����n�O�s
                    EnqueueToast?.Invoke(LanguageManager.GetText("LanguageChangedToast", "�w�N�y���]�w�ܧ󧹦�"));
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
                    
                    EnqueueToast?.Invoke(LanguageManager.GetText("ThemeChangedToast", "�w�N�D�D�]�w�ܧ󧹦�"));
                }
                
                OnPropertyChanged();
            }
        }
    }

    private SettingsViewModel()
    {
        Debug.WriteLine("SettingsViewModel constructor started");
        
        // ��l�ƶ��X
        RefreshAllCollections();
        
        // �q�\�ƥ�]�b��l�Ƥ���^
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
            // �ϥ� Dispatcher �T�O�b UI �u�{�W����
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
        
        // �O�s��e���A
        var currentLanguage = LanguageManager.CurrentLanguage;
        var currentTheme = ThemeManager.CurrentTheme;
        
        Debug.WriteLine($"Current language: {currentLanguage}, Current theme: {currentTheme}");
        
        // ���M�ſ襤��
        _selectedLanguage = "";
        _selectedTheme = "";
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(SelectedTheme));
        
        // ���s��R�y���ﶵ
        var oldLanguageCount = Languages.Count;
        Languages.Clear();
        OnPropertyChanged(nameof(Languages)); // �ߧY�q���M��
        
        foreach (var (_, name) in LanguageManager.GetAllLanguages())
        {
            Languages.Add(name);
            Debug.WriteLine($"Added language: '{name}'");
        }
        Debug.WriteLine($"Languages: {oldLanguageCount} -> {Languages.Count}");
        OnPropertyChanged(nameof(Languages)); // �q�����s��R
        
        // ���s��R�D�D�ﶵ
        var oldThemeCount = Themes.Count;
        Themes.Clear();
        OnPropertyChanged(nameof(Themes)); // �ߧY�q���M��
        
        foreach (var (_, name) in ThemeManager.GetAllThemes())
        {
            Themes.Add(name);
            Debug.WriteLine($"Added theme: '{name}'");
        }
        Debug.WriteLine($"Themes: {oldThemeCount} -> {Themes.Count}");
        OnPropertyChanged(nameof(Themes)); // �q�����s��R
        
        // �]�w��e�襤��
        var newLanguageName = LanguageManager.GetLanguageName(currentLanguage);
        var newThemeName = ThemeManager.GetThemeName(currentTheme);
        
        Debug.WriteLine($"New language name: '{newLanguageName}'");
        Debug.WriteLine($"New theme name: '{newThemeName}'");
        
        // �ˬd�ﶵ�O�_�s�b
        var languageExists = Languages.Contains(newLanguageName);
        var themeExists = Themes.Contains(newThemeName);
        Debug.WriteLine($"Language exists: {languageExists}, Theme exists: {themeExists}");
        
        // �]�w�襤��
        _selectedLanguage = languageExists ? newLanguageName : (Languages.FirstOrDefault() ?? "");
        _selectedTheme = themeExists ? newThemeName : (Themes.FirstOrDefault() ?? "");
        
        Debug.WriteLine($"Final selected language: '{_selectedLanguage}'");
        Debug.WriteLine($"Final selected theme: '{_selectedTheme}'");
        
        _isUpdating = false;
        
        // �̫�q���襤����s
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(SelectedTheme));
        
        // ��s���a�Ƥ�r
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