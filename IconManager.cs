namespace ExcelPasteTool;

/// <summary>
/// �Ϯ׺޲z���A�Τ@�޲z���ε{�����ϥΪ��ϮײŸ�
/// </summary>
public static class IconManager
{
    // �t�Υ\��Ÿ�
    public static string SettingsIcon => "?";        // �]�w
    public static string LanguageIcon => "??";       // �y��  
    public static string ThemeIcon => "??";          // �D�D
    
    // ��ƳB�z�Ÿ�
    public static string DataIcon => "??";           // ���
    public static string SearchIcon => "??";         // �j�M
    public static string ListIcon => "??";           // �M��
    
    // �ާ@���s�Ÿ�
    public static string PasteIcon => "??";          // �K�W
    public static string ClearIcon => "??";          // �M��
    
    // �������Ÿ�
    public static string NumberIcon => "#";          // �渹
    public static string ContentIcon => "??";        // ���e

    /// <summary>
    /// �ھڹϮ���������������Ÿ�
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