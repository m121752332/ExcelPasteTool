using System;

namespace ExcelPasteTool;

public static class FontManager
{
    public static event Action? FontChanged;

    public static void ApplyLanguageFont(AppLanguage language)
    {
        // 簡化字型管理，通過觸發事件讓 UI 更新
        FontChanged?.Invoke();
    }

    public static string GetLanguageFontFamily(AppLanguage language)
    {
        return LanguageManager.GetLanguageFontFamily(language);
    }

    public static string GetCodeFontFamily(AppLanguage language)
    {
        // 程式碼輸入框使用等寬字型
        return language switch
        {
            AppLanguage.TraditionalChinese => "Consolas, Microsoft JhengHei",
            AppLanguage.SimplifiedChinese => "Consolas, Source Han Sans SC",
            AppLanguage.English => "Consolas, Segoe UI",
            _ => "Consolas, Microsoft JhengHei"
        };
    }
}