using ExcelPasteTool.Helpers;

namespace ExcelPasteTool;

public static class LanguageController
{
    /// <summary>
    /// �����y���A�ھ� saveConfig �M�w�O�_�s��
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
