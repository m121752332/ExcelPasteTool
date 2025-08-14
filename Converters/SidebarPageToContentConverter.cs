using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ExcelPasteTool;

public class SidebarPageToContentConverter : IValueConverter
{
    // 單例模式，避免資料重置
    private static DataToolView? _dataToolViewInstance;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SidebarPage page)
        {
            switch (page)
            {
                case SidebarPage.Tools:
                    return _dataToolViewInstance ??= new DataToolView();
                case SidebarPage.Settings:
                    // 載入真正的 SettingsView
                    return new SettingsView();
                case SidebarPage.About:
                    return new TextBlock { Text = "關於頁面(待開發)", FontSize = 20, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                default:
                    return null;
            }
        }
        return null;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
