using System;

namespace ExcelPasteTool;

public static class FontManager
{
    public static event Action? FontChanged;

    public static void ApplyLanguageFont(AppLanguage language)
    {
        // ²�Ʀr���޲z�A�q�LĲ�o�ƥ��� UI ��s
        FontChanged?.Invoke();
    }

    public static string GetLanguageFontFamily(AppLanguage language)
    {
        return LanguageManager.GetLanguageFontFamily(language);
    }

    public static string GetCodeFontFamily(AppLanguage language)
    {
        // �{���X��J�بϥε��e�r��
        return language switch
        {
            AppLanguage.TraditionalChinese => "Consolas, Microsoft JhengHei",
            AppLanguage.SimplifiedChinese => "Consolas, Source Han Sans SC",
            AppLanguage.English => "Consolas, Segoe UI",
            _ => "Consolas, Microsoft JhengHei"
        };
    }
}