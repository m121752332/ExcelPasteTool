using ExcelPasteTool.Helpers;

namespace ExcelPasteTool;

public static class LanguageController
{
    /// <summary>
    /// 切換語言，根據 saveConfig 決定是否存檔
    /// </summary>
    public static void SetLanguage(AppLanguage language, bool saveConfig)
    {
        LanguageManager.CurrentLanguage = language;
        FontManager.ApplyLanguageFont(language);
        if (saveConfig)
        {
            ConfigServices.Config.Language = language.ToString();
            ConfigServices.Save();
        }
    }
}
