using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExcelPasteTool;

public class SettingsViewModel : INotifyPropertyChanged
{
    public ObservableCollection<string> Languages { get; } = new() { "�c�餤��", "²�餤��", "English" };
    public ObservableCollection<string> Themes { get; } = new() { "�t�¼Ҧ�", "���u�Ҧ�" };

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
                    "�c�餤��" => "TraditionalChinese",
                    "²�餤��" => "SimplifiedChinese",
                    "English" => "English",
                    _ => "TraditionalChinese"
                };
                ConfigServices.Save();
                EnqueueToast?.Invoke("�w�]�y�t�w�ܧ󧹦�");
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
                ConfigServices.Config.Theme = value == "�t�¼Ҧ�" ? "Dark" : "Light";
                ConfigServices.Save();
                EnqueueToast?.Invoke("�w�]�D�D�w�ܧ󧹦�");
                OnPropertyChanged();
            }
        }
    }

    public SettingsViewModel()
    {
        // ��l�ƿﶵ
        _selectedLanguage = ConfigServices.Config.Language switch
        {
            "TraditionalChinese" => "�c�餤��",
            "SimplifiedChinese" => "²�餤��",
            "English" => "English",
            _ => "�c�餤��"
        };
        _selectedTheme = ConfigServices.Config.Theme == "Dark" ? "�t�¼Ҧ�" : "���u�Ҧ�";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}