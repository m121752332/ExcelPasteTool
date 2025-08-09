using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ExcelPasteTool;

public enum AppLanguage
{
    TraditionalChinese,  // 繁體中文
    SimplifiedChinese,   // 簡體中文
    English              // 英文
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
            AppLanguage.TraditionalChinese => "繁體中文",
            AppLanguage.SimplifiedChinese => "?体中文",
            AppLanguage.English => "English",
            _ => "繁體中文"
        };
    }

    public static string GetLanguageFontFamily(AppLanguage language)
    {
        return language switch
        {
            AppLanguage.TraditionalChinese => "Microsoft JhengHei",  // 微軟正黑體
            AppLanguage.SimplifiedChinese => "Source Han Sans SC",  // 思源黑體 SC
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
            ["AppTitle"] = "資料分隔處理器",
            ["SeparatorLabel"] = "? 分隔符號：",
            ["ThemeLabel"] = "?? 主題:",
            ["LanguageLabel"] = "?? 語言:",
            ["PipDataTitle"] = "?? PIP 分隔資料",
            ["SqlConditionTitle"] = "?? SQL 條件",
            ["DataListTitle"] = "?? 資料清單",
            ["PasteDataButton"] = "?? 資料貼上",
            ["ClearDataButton"] = "?? 清空資料",
            ["RowNumberHeader"] = "行號",
            ["DataContentHeader"] = "資料內容",
            ["SeparatorPipe"] = "豎線 |",
            ["SeparatorSemicolon"] = "分號 ;",
            ["SeparatorColon"] = "冒號 :",
            ["SeparatorSpace"] = "空白",
            ["ThemeDark"] = "暗黑模式",
            ["ThemeModern"] = "現代化主題"
        };
    }

    private static Dictionary<string, string> GetSimplifiedChineseTexts()
    {
        return new Dictionary<string, string>
        {
            ["AppTitle"] = "?据分隔?理器",
            ["SeparatorLabel"] = "? 分隔符?：",
            ["ThemeLabel"] = "?? 主?:",
            ["LanguageLabel"] = "?? ?言:",
            ["PipDataTitle"] = "?? PIP 分隔?据",
            ["SqlConditionTitle"] = "?? SQL ?件",
            ["DataListTitle"] = "?? ?据清?",
            ["PasteDataButton"] = "?? ?据粘?",
            ["ClearDataButton"] = "?? 清空?据",
            ["RowNumberHeader"] = "行?",
            ["DataContentHeader"] = "?据?容",
            ["SeparatorPipe"] = "?? |",
            ["SeparatorSemicolon"] = "分? ;",
            ["SeparatorColon"] = "冒? :",
            ["SeparatorSpace"] = "空格",
            ["ThemeDark"] = "暗黑模式",
            ["ThemeModern"] = "?代化主?"
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

// 可觀察的本地化文字類別
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