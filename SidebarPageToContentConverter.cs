using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ExcelPasteTool;

public class SidebarPageToContentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SidebarPage page)
        {
            switch (page)
            {
                case SidebarPage.Tools:
                    return new DataToolView();
                case SidebarPage.Settings:
                    // �i�X�R: return new SettingsView();
                    return new TextBlock { Text = "�]�m�����]�ݳ]�p�^", FontSize = 20, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                case SidebarPage.About:
                    return new TextBlock { Text = "���󭶭��]�ݳ]�p�^", FontSize = 20, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
                default:
                    return null;
            }
        }
        return null;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
