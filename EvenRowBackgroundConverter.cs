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
            if (value is bool isEven)
            {
                if (isEven)
                {
                    // 偶數行 #141414
                    return new SolidColorBrush(Color.Parse("#141414"));
                }
                else
                {
                    // 奇數行 #1a1a1a
                    return new SolidColorBrush(Color.Parse("#1a1a1a"));
                }
            }
            return Brushes.Transparent;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
