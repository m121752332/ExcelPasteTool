namespace ExcelPasteTool;

/// <summary>
/// 圖案管理器，統一管理應用程式中使用的圖案符號
/// </summary>
public static class IconManager
{
    // 系統功能符號
    public static string SettingsIcon => "?";        // 設定
    public static string LanguageIcon => "??";       // 語言  
    public static string ThemeIcon => "??";          // 主題
    
    // 資料處理符號
    public static string DataIcon => "??";           // 資料
    public static string SearchIcon => "??";         // 搜尋
    public static string ListIcon => "??";           // 清單
    
    // 操作按鈕符號
    public static string PasteIcon => "??";          // 貼上
    public static string ClearIcon => "??";          // 清空
    
    // 表格相關符號
    public static string NumberIcon => "#";          // 行號
    public static string ContentIcon => "??";        // 內容

    /// <summary>
    /// 根據圖案類型獲取對應的符號
    /// </summary>
    public static string GetIcon(string iconType)
    {
        return iconType.ToLower() switch
        {
            "settings" => SettingsIcon,
            "language" => LanguageIcon,
            "theme" => ThemeIcon,
            "data" => DataIcon,
            "search" => SearchIcon,
            "list" => ListIcon,
            "paste" => PasteIcon,
            "clear" => ClearIcon,
            "number" => NumberIcon,
            "content" => ContentIcon,
            _ => "?"
        };
    }
}