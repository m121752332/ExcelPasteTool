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
            ["RemoveRowTooltip"] = "移除此筆",
            ["SeparatorPipe"] = "豎線 |",
            ["SeparatorSemicolon"] = "分號 ;",
            ["SeparatorColon"] = "冒號 :",
            ["SeparatorSpace"] = "空白",
            ["ThemeDark"] = "GitHub 暗黑",
            ["ThemeLight"] = "光線模式",
            ["ThemeTiffany"] = "蒂芬妮藍",
            ["ThemeDesc"] = "選擇 App 的配色主題",
            ["FontTitle"] = "字型",
            ["FontDesc"] = "選擇介面與資料區使用的字型",
            ["FontDefault"] = "Noto Sans TC（內建）",
            ["FontSizeTitle"] = "字體大小",
            ["FontSizeDesc"] = "調整整個 App 的文字大小",
            ["FontPreviewText"] = "預覽：TIPTOP 料號 A-001 | 資料分隔 1234567890",
            ["FontResetButton"] = "還原預設",
            ["FontChangedToast"] = "字型設定已儲存",
            ["RowCountFormat"] = "共 {0} 筆",
            ["EmptyStateText"] = "尚無資料\n從 Excel 複製一欄後，按「資料貼上」",
            ["TopBoxWatermark"] = "也可以直接在這裡輸入以分隔符號串接的資料",
            ["LanguageChangedToast"] = "已將語言設定變更完成",
            ["ThemeChangedToast"] = "已將主題設定變更完成",
            ["LanguageDesc"] = "變更 App 的語言設定",
            ["HelpTitle"] = "說明",
            ["HelpOpenInBrowser"] = "用瀏覽器開啟",
            ["HelpUnavailableText"] = "這台電腦沒有可用的內嵌瀏覽器元件，已改用預設瀏覽器開啟說明。\n之後也可以按右上角的「用瀏覽器開啟」。",
            ["HelpViewerTitle"] = "說明開啟方式",
            ["HelpViewerDesc"] = "按 F1 或點選「說明」時，要在 App 內顯示還是用預設瀏覽器開啟",
            ["HelpViewerInApp"] = "內建視窗",
            ["HelpViewerBrowser"] = "預設瀏覽器",
            ["HelpViewerChangedToast"] = "說明開啟方式已儲存"
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
            ["RemoveRowTooltip"] = "移除此条",
            ["SeparatorPipe"] = "竖线 |",
            ["SeparatorSemicolon"] = "分号 ;",
            ["SeparatorColon"] = "冒号 :",
            ["SeparatorSpace"] = "空格",
            ["ThemeDark"] = "GitHub 暗黑",
            ["ThemeLight"] = "光线模式",
            ["ThemeTiffany"] = "蒂芙尼蓝",
            ["ThemeDesc"] = "选择 App 的配色主题",
            ["FontTitle"] = "字体",
            ["FontDesc"] = "选择界面与数据区使用的字体",
            ["FontDefault"] = "Noto Sans TC（内置）",
            ["FontSizeTitle"] = "字体大小",
            ["FontSizeDesc"] = "调整整个 App 的文字大小",
            ["FontPreviewText"] = "预览：TIPTOP 料号 A-001 | 数据分隔 1234567890",
            ["FontResetButton"] = "还原默认",
            ["FontChangedToast"] = "字体设定已保存",
            ["RowCountFormat"] = "共 {0} 笔",
            ["EmptyStateText"] = "尚无数据\n从 Excel 复制一列后，按「数据粘贴」",
            ["TopBoxWatermark"] = "也可以直接在这里输入以分隔符号串接的数据",
            ["LanguageChangedToast"] = "已将语言设定变更完成",
            ["ThemeChangedToast"] = "已将主题设定变更完成",
            ["LanguageDesc"] = "变更 App 的语言设定",
            ["HelpTitle"] = "说明",
            ["HelpOpenInBrowser"] = "用浏览器打开",
            ["HelpUnavailableText"] = "这台电脑没有可用的内嵌浏览器组件，已改用默认浏览器打开说明。\n之后也可以点右上角的「用浏览器打开」。",
            ["HelpViewerTitle"] = "说明打开方式",
            ["HelpViewerDesc"] = "按 F1 或点击「说明」时，要在 App 内显示还是用默认浏览器打开",
            ["HelpViewerInApp"] = "内置窗口",
            ["HelpViewerBrowser"] = "默认浏览器",
            ["HelpViewerChangedToast"] = "说明打开方式已保存"
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
            ["RemoveRowTooltip"] = "Remove this row",
            ["SeparatorPipe"] = "Pipe |",
            ["SeparatorSemicolon"] = "Semicolon ;",
            ["SeparatorColon"] = "Colon :",
            ["SeparatorSpace"] = "Space",
            ["ThemeDark"] = "GitHub Dark",
            ["ThemeLight"] = "Light Mode",
            ["ThemeTiffany"] = "Tiffany Blue",
            ["ThemeDesc"] = "Choose the app's color theme",
            ["FontTitle"] = "Font",
            ["FontDesc"] = "Font used for the interface and data areas",
            ["FontDefault"] = "Noto Sans TC (built-in)",
            ["FontSizeTitle"] = "Font Size",
            ["FontSizeDesc"] = "Adjust the text size of the whole app",
            ["FontPreviewText"] = "Preview: TIPTOP item A-001 | Data split 1234567890",
            ["FontResetButton"] = "Reset",
            ["FontChangedToast"] = "Font settings saved",
            ["RowCountFormat"] = "{0} rows",
            ["EmptyStateText"] = "No data yet\nCopy a column in Excel, then click \"Paste Data\"",
            ["TopBoxWatermark"] = "Or type values joined by the separator here",
            ["LanguageChangedToast"] = "Language settings changed successfully",
            ["ThemeChangedToast"] = "Theme settings changed successfully",
            ["LanguageDesc"] = "Change the app's language settings",
            ["HelpTitle"] = "Help",
            ["HelpOpenInBrowser"] = "Open in browser",
            ["HelpUnavailableText"] = "No embedded browser component is available on this computer, so the help was opened in your default browser.\nYou can also click \"Open in browser\" at the top right.",
            ["HelpViewerTitle"] = "Help Viewer",
            ["HelpViewerDesc"] = "Where to show the help when you press F1 or click Help: inside the app or in your default browser",
            ["HelpViewerInApp"] = "In-app window",
            ["HelpViewerBrowser"] = "Default browser",
            ["HelpViewerChangedToast"] = "Help viewer setting saved"
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