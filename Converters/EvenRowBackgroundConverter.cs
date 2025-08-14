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

            // 1) �u���ΥD�D�귽�]Light �U���ѥ�/�H�ǡFDark �i�����δ��Ѳ`��^
            if (app != null && app.TryGetResource(isEven ? "TableEvenRowBrush" : "TableOddRowBrush", theme, out var brushObj))
            {
                if (brushObj is IBrush themed) return themed;
            }

            // 2) �ª� EvenRowBackgroundBrush�]�Ȱ��ƦC�^
            if (app != null && isEven && app.TryGetResource("EvenRowBackgroundBrush", theme, out var evenObj))
            {
                if (evenObj is IBrush evenBrush) return evenBrush;
            }

            // 3) Fallback�G�̥D�D���w�]�C��
            bool isLight = theme == ThemeVariant.Light;
            if (isLight)
            {
                var lightEven = Color.Parse("#F2F5FA"); // �H��
                var lightOdd  = Color.Parse("#FFFFFF"); // ��
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
