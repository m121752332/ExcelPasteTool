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
            AppLanguage.SimplifiedChinese => "简体中文",
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
            ["SettingsTitle"] = "設定",
            ["SeparatorLabel"] = "分隔符號：",
            ["ThemeLabel"] = "主題:",
            ["LanguageLabel"] = "語言:",
            ["PipDataTitle"] = "PIP 分隔資料",
            ["SqlConditionTitle"] = "SQL 條件",
            ["DataListTitle"] = "資料清單",
            ["PasteDataButton"] = "資料貼上",
            ["ClearDataButton"] = "清空資料",
            ["RowNumberHeader"] = "行號",
            ["DataContentHeader"] = "資料內容",
            ["SeparatorPipe"] = "豎線 |",
            ["SeparatorSemicolon"] = "分號 ;",
            ["SeparatorColon"] = "冒號 :",
            ["SeparatorSpace"] = "空白",
            ["ThemeDark"] = "暗黑模式",
            ["ThemeLight"] = "光線模式",
            ["LanguageChangedToast"] = "已將語言設定變更完成",
            ["ThemeChangedToast"] = "已將主題設定變更完成",
            ["LanguageDesc"] = "變更 App 的語言設定"
        };
    }

    private static Dictionary<string, string> GetSimplifiedChineseTexts()
    {
        return new Dictionary<string, string>
        {
            ["AppTitle"] = "数据分隔处理器",
            ["SettingsTitle"] = "设定",
            ["SeparatorLabel"] = "分隔符号：",
            ["ThemeLabel"] = "主题:",
            ["LanguageLabel"] = "语言:",
            ["PipDataTitle"] = "PIP 分隔数据",
            ["SqlConditionTitle"] = "SQL 条件",
            ["DataListTitle"] = "数据清单",
            ["PasteDataButton"] = "数据粘贴",
            ["ClearDataButton"] = "清空数据",
            ["RowNumberHeader"] = "行号",
            ["DataContentHeader"] = "数据内容",
            ["SeparatorPipe"] = "竖线 |",
            ["SeparatorSemicolon"] = "分号 ;",
            ["SeparatorColon"] = "冒号 :",
            ["SeparatorSpace"] = "空格",
            ["ThemeDark"] = "暗黑模式",
            ["ThemeLight"] = "光线模式",
            ["LanguageChangedToast"] = "已将语言设定变更完成",
            ["ThemeChangedToast"] = "已将主题设定变更完成",
            ["LanguageDesc"] = "变更 App 的语言设定"
        };
    }

    private static Dictionary<string, string> GetEnglishTexts()
    {
        return new Dictionary<string, string>
        {
            ["AppTitle"] = "Data Separator Tool",
            ["SettingsTitle"] = "Settings",
            ["SeparatorLabel"] = "Separator:",
            ["ThemeLabel"] = "Theme:",
            ["LanguageLabel"] = "Language:",
            ["PipDataTitle"] = "PIP Separated Data",
            ["SqlConditionTitle"] = "SQL Condition",
            ["DataListTitle"] = "Data List",
            ["PasteDataButton"] = "Paste Data",
            ["ClearDataButton"] = "Clear Data",
            ["RowNumberHeader"] = "Row",
            ["DataContentHeader"] = "Data Content",
            ["SeparatorPipe"] = "Pipe |",
            ["SeparatorSemicolon"] = "Semicolon ;",
            ["SeparatorColon"] = "Colon :",
            ["SeparatorSpace"] = "Space",
            ["ThemeDark"] = "Dark Mode",
            ["ThemeLight"] = "Light Mode",
            ["LanguageChangedToast"] = "Language settings changed successfully",
            ["ThemeChangedToast"] = "Theme settings changed successfully",
            ["LanguageDesc"] = "Change the app's language settings"
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