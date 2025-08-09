using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ExcelPasteTool;

public enum AppLanguage
{
    TraditionalChinese,  // �c�餤��
    SimplifiedChinese,   // ²�餤��
    English              // �^��
}

public static class LanguageManager
{
    private static AppLanguage _currentLanguage = AppLanguage.TraditionalChinese;
    private static Dictionary<string, string> _currentTexts = new();
    
    public static AppLanguage CurrentLanguage 
    { 
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                LoadLanguage(value);
                LanguageChanged?.Invoke();
            }
        }
    }

    public static event Action? LanguageChanged;

    public static void Initialize()
    {
        LoadLanguage(_currentLanguage);
    }

    private static void LoadLanguage(AppLanguage language)
    {
        _currentTexts = language switch
        {
            AppLanguage.TraditionalChinese => GetTraditionalChineseTexts(),
            AppLanguage.SimplifiedChinese => GetSimplifiedChineseTexts(),
            AppLanguage.English => GetEnglishTexts(),
            _ => GetTraditionalChineseTexts()
        };
    }

    public static string GetText(string key, string defaultValue = "")
    {
        return _currentTexts.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public static string GetLanguageName(AppLanguage language)
    {
        return language switch
        {
            AppLanguage.TraditionalChinese => "�c�餤��",
            AppLanguage.SimplifiedChinese => "?�^����",
            AppLanguage.English => "English",
            _ => "�c�餤��"
        };
    }

    public static string GetLanguageFontFamily(AppLanguage language)
    {
        return language switch
        {
            AppLanguage.TraditionalChinese => "Microsoft JhengHei",  // �L�n������
            AppLanguage.SimplifiedChinese => "Source Han Sans SC",  // �䷽���� SC
            AppLanguage.English => "Segoe UI",                      // Segoe UI
            _ => "Microsoft JhengHei"
        };
    }

    public static List<(AppLanguage Language, string Name)> GetAllLanguages()
    {
        return new List<(AppLanguage, string)>
        {
            (AppLanguage.TraditionalChinese, GetLanguageName(AppLanguage.TraditionalChinese)),
            (AppLanguage.SimplifiedChinese, GetLanguageName(AppLanguage.SimplifiedChinese)),
            (AppLanguage.English, GetLanguageName(AppLanguage.English))
        };
    }

    private static Dictionary<string, string> GetTraditionalChineseTexts()
    {
        return new Dictionary<string, string>
        {
            ["AppTitle"] = "��Ƥ��j�B�z��",
            ["SeparatorLabel"] = "? ���j�Ÿ��G",
            ["ThemeLabel"] = "?? �D�D:",
            ["LanguageLabel"] = "?? �y��:",
            ["PipDataTitle"] = "?? PIP ���j���",
            ["SqlConditionTitle"] = "?? SQL ����",
            ["DataListTitle"] = "?? ��ƲM��",
            ["PasteDataButton"] = "?? ��ƶK�W",
            ["ClearDataButton"] = "?? �M�Ÿ��",
            ["RowNumberHeader"] = "�渹",
            ["DataContentHeader"] = "��Ƥ��e",
            ["SeparatorPipe"] = "�ݽu |",
            ["SeparatorSemicolon"] = "���� ;",
            ["SeparatorColon"] = "�_�� :",
            ["SeparatorSpace"] = "�ť�",
            ["ThemeDark"] = "�t�¼Ҧ�",
            ["ThemeModern"] = "�{�N�ƥD�D"
        };
    }

    private static Dictionary<string, string> GetSimplifiedChineseTexts()
    {
        return new Dictionary<string, string>
        {
            ["AppTitle"] = "?�u���j?�z��",
            ["SeparatorLabel"] = "? ���j��?�G",
            ["ThemeLabel"] = "?? �D?:",
            ["LanguageLabel"] = "?? ?��:",
            ["PipDataTitle"] = "?? PIP ���j?�u",
            ["SqlConditionTitle"] = "?? SQL ?��",
            ["DataListTitle"] = "?? ?�u�M?",
            ["PasteDataButton"] = "?? ?�u��?",
            ["ClearDataButton"] = "?? �M��?�u",
            ["RowNumberHeader"] = "��?",
            ["DataContentHeader"] = "?�u?�e",
            ["SeparatorPipe"] = "?? |",
            ["SeparatorSemicolon"] = "��? ;",
            ["SeparatorColon"] = "�_? :",
            ["SeparatorSpace"] = "�Ů�",
            ["ThemeDark"] = "�t�¼Ҧ�",
            ["ThemeModern"] = "?�N�ƥD?"
        };
    }

    private static Dictionary<string, string> GetEnglishTexts()
    {
        return new Dictionary<string, string>
        {
            ["AppTitle"] = "Data Separator Tool",
            ["SeparatorLabel"] = "? Separator:",
            ["ThemeLabel"] = "?? Theme:",
            ["LanguageLabel"] = "?? Language:",
            ["PipDataTitle"] = "?? PIP Separated Data",
            ["SqlConditionTitle"] = "?? SQL Condition",
            ["DataListTitle"] = "?? Data List",
            ["PasteDataButton"] = "?? Paste Data",
            ["ClearDataButton"] = "?? Clear Data",
            ["RowNumberHeader"] = "Row",
            ["DataContentHeader"] = "Data Content",
            ["SeparatorPipe"] = "Pipe |",
            ["SeparatorSemicolon"] = "Semicolon ;",
            ["SeparatorColon"] = "Colon :",
            ["SeparatorSpace"] = "Space",
            ["ThemeDark"] = "Dark Mode",
            ["ThemeModern"] = "Modern Theme"
        };
    }
}

// �i�[����a�Ƥ�r���O
public class LocalizedText : INotifyPropertyChanged
{
    private readonly string _key;
    private readonly string _defaultValue;

    public LocalizedText(string key, string defaultValue = "")
    {
        _key = key;
        _defaultValue = defaultValue;
        LanguageManager.LanguageChanged += OnLanguageChanged;
    }

    public string Text => LanguageManager.GetText(_key, _defaultValue);

    private void OnLanguageChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}