using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExcelPasteTool;

public class SettingsViewModel : INotifyPropertyChanged
{
    public ObservableCollection<string> Languages { get; } = new() { "繁體中文", "簡體中文", "English" };
    public ObservableCollection<string> Themes { get; } = new() { "暗黑模式", "光線模式" };

    public Action<string>? EnqueueToast { get; set; }

    private string _selectedLanguage;
    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage != value)
            {
                _selectedLanguage = value;
                ConfigServices.Config.Language = value switch
                {
                    "繁體中文" => "TraditionalChinese",
                    "簡體中文" => "SimplifiedChinese",
                    "English" => "English",
                    _ => "TraditionalChinese"
                };
                ConfigServices.Save();
                EnqueueToast?.Invoke("預設語系已變更完成");
                OnPropertyChanged();
            }
        }
    }

    private string _selectedTheme;
    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (_selectedTheme != value)
            {
                _selectedTheme = value;
                ConfigServices.Config.Theme = value == "暗黑模式" ? "Dark" : "Light";
                ConfigServices.Save();
                EnqueueToast?.Invoke("預設主題已變更完成");
                OnPropertyChanged();
            }
        }
    }

    public SettingsViewModel()
    {
        // 初始化選項
        _selectedLanguage = ConfigServices.Config.Language switch
        {
            "TraditionalChinese" => "繁體中文",
            "SimplifiedChinese" => "簡體中文",
            "English" => "English",
            _ => "繁體中文"
        };
        _selectedTheme = ConfigServices.Config.Theme == "Dark" ? "暗黑模式" : "光線模式";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}