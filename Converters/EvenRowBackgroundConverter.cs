using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Globalization;

namespace ExcelPasteTool.Converters
{
    public class EvenRowBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isEven = value is bool b && b;
            var app = Application.Current;
            var theme = app?.ActualThemeVariant ?? ThemeVariant.Light;

            // 1) 優先用主題資源（Light 下提供白/淡灰；Dark 可忽略或提供深色）
            if (app != null && app.TryGetResource(isEven ? "TableEvenRowBrush" : "TableOddRowBrush", theme, out var brushObj))
            {
                if (brushObj is IBrush themed) return themed;
            }

            // 2) 舊的 EvenRowBackgroundBrush（僅偶數列）
            if (app != null && isEven && app.TryGetResource("EvenRowBackgroundBrush", theme, out var evenObj))
            {
                if (evenObj is IBrush evenBrush) return evenBrush;
            }

            // 3) Fallback：依主題給預設顏色
            bool isLight = theme == ThemeVariant.Light;
            if (isLight)
            {
                var lightEven = Color.Parse("#F2F5FA"); // 淡灰
                var lightOdd  = Color.Parse("#FFFFFF"); // 白
                return new SolidColorBrush(isEven ? lightEven : lightOdd);
            }
            else
            {
                var darkEven = Color.Parse("#141414");
                var darkOdd  = Color.Parse("#1a1a1a");
                return new SolidColorBrush(isEven ? darkEven : darkOdd);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
