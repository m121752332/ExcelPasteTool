using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ExcelPasteTool;

public class SidebarWidthConverter : IValueConverter
{
    // 此轉換器用於側邊欄寬度，根據是否展開改變寬度
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 如果 value 是 true（展開），則回傳 180，否則回傳 56
        if (value is bool isExpanded)
            return isExpanded ? 100 : 56;
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

public class SidebarSelectedBarConverter : IValueConverter
{
    // 此轉換器用於側邊欄選取導引條的顏色
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 如果 value 是 true（被選取），則回傳綠色 #2ecc40
        if (value is bool isSelected && isSelected)
            return "#2ecc40";
        // 未選取則回傳透明
        return "Transparent";
    }
    // 此方法未實作，因為只用於單向資料繫結
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SidebarSelectedBackgroundConverter : IValueConverter
{
    // 此轉換器用於側邊欄按鈕的背景色，根據是否選取改變顏色
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 如果 value 是 true（被選取），則回傳黑色 #232323
        if (value is bool isSelected && isSelected)
            return "#232323";
        // 未選取則回傳透明
        return "Transparent";
    }
    // 此方法未實作，因為只用於單向資料繫結
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
