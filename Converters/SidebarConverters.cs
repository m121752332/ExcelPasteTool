using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ExcelPasteTool;

public class SidebarWidthConverter : IValueConverter
{
    // 此轉換器用於側邊欄寬度，根據是否展開改變寬度
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 如果 value 是 true（展開），則回傳 150，否則回傳 56
        if (value is bool isExpanded)
            return isExpanded ? 150 : 56;
        // 預設寬度 56
        return 56;
    }
    // 此方法未實作，因為只用於單向資料繫結
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SidebarExpandVisibilityConverter : IValueConverter
{
    // 此轉換器用於側邊欄展開狀態的可見性
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 如果 value 是 true（展開），則回傳 true，否則回傳 false
        if (value is bool isExpanded)
            return isExpanded;
        // 預設為不可見
        return false;
    }
    // 此方法未實作，因為只用於單向資料繫結
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
