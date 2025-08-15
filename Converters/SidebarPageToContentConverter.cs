using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ExcelPasteTool;

public class SidebarPageToContentConverter : IValueConverter
{
    // Cache views to preserve state where helpful
    private static DataToolView? _dataToolViewInstance;
    private static SettingsView? _settingsViewInstance;
    private static AboutView? _aboutViewInstance;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SidebarPage page)
        {
            switch (page)
            {
                case SidebarPage.Tools:
                    return _dataToolViewInstance ??= new DataToolView();
                case SidebarPage.Settings:
                    return _settingsViewInstance ??= new SettingsView();
                case SidebarPage.About:
                    return _aboutViewInstance ??= new AboutView();
                default:
                    return null;
            }
        }
        return null;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
