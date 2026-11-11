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
            ["HelpViewerChangedToast"] = "說明開啟方式已儲存",
            ["TourButton"] = "導覽",
            ["TourButtonTooltip"] = "重看操作導覽",
            ["TourStepFormat"] = "第 {0} / {1} 步",
            ["TourSkip"] = "略過導覽",
            ["TourPrev"] = "上一步",
            ["TourNext"] = "下一步",
            ["TourDone"] = "完成",
            ["TourWelcomeTitle"] = "歡迎使用 DataSplitter Pro 👋",
            ["TourWelcomeBody"] = "這個工具能把從 Excel 複製的一欄資料，轉成以分隔符號串接的字串與 SQL 條件。\n接下來花 30 秒帶你看一遍主要操作，可隨時按 Esc 略過。",
            ["TourSidebarTitle"] = "功能選單",
            ["TourSidebarBody"] = "從左側切換「工具」「設定」「說明」「關於」頁面；點最上方的按鈕可以展開或收合選單。",
            ["TourPasteTitle"] = "① 貼上 Excel 資料",
            ["TourPasteBody"] = "先在 Excel 選取並複製一欄（Ctrl+C），再按「資料貼上」。每一格會變成一筆資料，重複的值會自動排除；若複製了多欄，只取第一欄。",
            ["TourTableTitle"] = "資料清單",
            ["TourTableBody"] = "貼上的資料會列在這裡。滑鼠移到某一列，右側會出現 ✕ 可移除該筆；點表頭的「行號」可複製全部資料。",
            ["TourSeparatorTitle"] = "② 選擇分隔符號",
            ["TourSeparatorBody"] = "決定資料要用哪個符號串接：豎線 |、分號 ;、冒號 : 或空白，切換後結果立即更新。",
            ["TourTopBoxTitle"] = "③ 分隔資料",
            ["TourTopBoxBody"] = "資料會自動串成一行，例如 A001|A002|A003。也可以直接在這裡輸入或修改，右邊的清單會跟著更新。",
            ["TourSqlTitle"] = "④ SQL 條件",
            ["TourSqlBody"] = "同時產生 SQL 的 IN 條件，例如 ('A001', 'A002')，單引號會自動跳脫，可直接貼到查詢語法中。",
            ["TourCopyTitle"] = "一鍵複製",
            ["TourCopyBody"] = "按標題右側的複製鈕，就能把分隔資料或 SQL 條件複製到剪貼簿。",
            ["TourClearTitle"] = "清空資料",
            ["TourClearBody"] = "要處理下一批資料時，按這裡清除畫面並還原分隔符號。",
            ["TourAppearanceTitle"] = "語言與主題",
            ["TourAppearanceBody"] = "這裡可以臨時切換介面語言與配色主題；要永久保存，或調整字型、字體大小，請到「設定」頁。",
            ["TourReplayTitle"] = "隨時重看導覽",
            ["TourReplayBody"] = "之後想再看一次，按右上角的「導覽」按鈕即可；按 F1 可以開啟完整的使用說明。祝使用愉快！"
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
            ["HelpViewerChangedToast"] = "说明打开方式已保存",
            ["TourButton"] = "导览",
            ["TourButtonTooltip"] = "重看操作导览",
            ["TourStepFormat"] = "第 {0} / {1} 步",
            ["TourSkip"] = "跳过导览",
            ["TourPrev"] = "上一步",
            ["TourNext"] = "下一步",
            ["TourDone"] = "完成",
            ["TourWelcomeTitle"] = "欢迎使用 DataSplitter Pro 👋",
            ["TourWelcomeBody"] = "这个工具能把从 Excel 复制的一列数据，转成以分隔符号串接的字符串与 SQL 条件。\n接下来花 30 秒带你看一遍主要操作，可随时按 Esc 跳过。",
            ["TourSidebarTitle"] = "功能菜单",
            ["TourSidebarBody"] = "从左侧切换「工具」「设定」「说明」「关于」页面；点最上方的按钮可以展开或收起菜单。",
            ["TourPasteTitle"] = "① 粘贴 Excel 数据",
            ["TourPasteBody"] = "先在 Excel 选取并复制一列（Ctrl+C），再按「数据粘贴」。每一格会变成一条数据，重复的值会自动排除；若复制了多列，只取第一列。",
            ["TourTableTitle"] = "数据清单",
            ["TourTableBody"] = "粘贴的数据会列在这里。鼠标移到某一行，右侧会出现 ✕ 可移除该条；点表头的「行号」可复制全部数据。",
            ["TourSeparatorTitle"] = "② 选择分隔符号",
            ["TourSeparatorBody"] = "决定数据要用哪个符号串接：竖线 |、分号 ;、冒号 : 或空格，切换后结果立即更新。",
            ["TourTopBoxTitle"] = "③ 分隔数据",
            ["TourTopBoxBody"] = "数据会自动串成一行，例如 A001|A002|A003。也可以直接在这里输入或修改，右边的清单会跟着更新。",
            ["TourSqlTitle"] = "④ SQL 条件",
            ["TourSqlBody"] = "同时产生 SQL 的 IN 条件，例如 ('A001', 'A002')，单引号会自动转义，可直接粘贴到查询语句中。",
            ["TourCopyTitle"] = "一键复制",
            ["TourCopyBody"] = "按标题右侧的复制按钮，就能把分隔数据或 SQL 条件复制到剪贴板。",
            ["TourClearTitle"] = "清空数据",
            ["TourClearBody"] = "要处理下一批数据时，按这里清除画面并还原分隔符号。",
            ["TourAppearanceTitle"] = "语言与主题",
            ["TourAppearanceBody"] = "这里可以临时切换界面语言与配色主题；要永久保存，或调整字体、字体大小，请到「设定」页。",
            ["TourReplayTitle"] = "随时重看导览",
            ["TourReplayBody"] = "之后想再看一次，按右上角的「导览」按钮即可；按 F1 可以打开完整的使用说明。祝使用愉快！"
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
            ["HelpViewerChangedToast"] = "Help viewer setting saved",
            ["TourButton"] = "Tour",
            ["TourButtonTooltip"] = "Replay the quick tour",
            ["TourStepFormat"] = "Step {0} of {1}",
            ["TourSkip"] = "Skip tour",
            ["TourPrev"] = "Back",
            ["TourNext"] = "Next",
            ["TourDone"] = "Done",
            ["TourWelcomeTitle"] = "Welcome to DataSplitter Pro 👋",
            ["TourWelcomeBody"] = "This tool turns a column copied from Excel into a separator-joined string and a SQL condition.\nHere is a 30-second walkthrough. Press Esc at any time to skip.",
            ["TourSidebarTitle"] = "Navigation",
            ["TourSidebarBody"] = "Switch between Tools, Settings, Help and About here. The button at the top expands or collapses the menu.",
            ["TourPasteTitle"] = "① Paste Excel data",
            ["TourPasteBody"] = "Copy a column in Excel (Ctrl+C), then click Paste Data. Each cell becomes one row and duplicates are removed. If several columns were copied, only the first is used.",
            ["TourTableTitle"] = "Data list",
            ["TourTableBody"] = "Pasted rows appear here. Hover a row and click ✕ to remove it, or click the row number header to copy everything.",
            ["TourSeparatorTitle"] = "② Pick a separator",
            ["TourSeparatorBody"] = "Choose how values are joined: pipe |, semicolon ;, colon : or space. The result updates right away.",
            ["TourTopBoxTitle"] = "③ Separated data",
            ["TourTopBoxBody"] = "Values are joined into one line, for example A001|A002|A003. You can also type or edit here and the list follows.",
            ["TourSqlTitle"] = "④ SQL condition",
            ["TourSqlBody"] = "A SQL IN list is generated as well, for example ('A001', 'A002'). Single quotes are escaped so it can go straight into a query.",
            ["TourCopyTitle"] = "One-click copy",
            ["TourCopyBody"] = "Use the copy buttons beside each title to copy the separated data or the SQL condition.",
            ["TourClearTitle"] = "Clear data",
            ["TourClearBody"] = "Click here to reset the screen and the separator before the next batch.",
            ["TourAppearanceTitle"] = "Language and theme",
            ["TourAppearanceBody"] = "Switch language and theme temporarily here. To keep the change, or to adjust fonts, go to Settings.",
            ["TourReplayTitle"] = "Replay any time",
            ["TourReplayBody"] = "Click the Tour button in the top-right corner to watch this again. Press F1 for the full help. Enjoy!"
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