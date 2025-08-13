using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace ExcelPasteTool
{
    public class EvenRowBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isEven && isEven)
            {
                // ¨ú¥Î Application.Current.Resources ªº Brush
                return Avalonia.Application.Current?.Resources["EvenRowBackgroundBrush"] ?? Brushes.Transparent;
            }
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
