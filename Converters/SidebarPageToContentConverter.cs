using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ExcelPasteTool;

public class SidebarPageToContentConverter : IValueConverter
{
    // ��ҼҦ��A�קK��ƭ��m
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
                    // ���J�u���� SettingsView
                    return new SettingsView();
                case SidebarPage.About:
                    return new TextBlock { Text = "���󭶭�(�ݶ}�o)", FontSize = 20, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                default:
                    return null;
            }
        }
        return null;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
