using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ExcelPasteTool;

public class SidebarWidthConverter : IValueConverter
{
    // ���ഫ���Ω�����e�סA�ھڬO�_�i�}���ܼe��
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // �p�G value �O true�]�i�}�^�A�h�^�� 180�A�_�h�^�� 56
        if (value is bool isExpanded)
            return isExpanded ? 100 : 56;
        // �w�]�e�� 56
        return 56;
    }
    // ����k����@�A�]���u�Ω��V���ô��
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SidebarExpandVisibilityConverter : IValueConverter
{
    // ���ഫ���Ω�����i�}���A���i����
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // �p�G value �O true�]�i�}�^�A�h�^�� true�A�_�h�^�� false
        if (value is bool isExpanded)
            return isExpanded;
        // �w�]�����i��
        return false;
    }
    // ����k����@�A�]���u�Ω��V���ô��
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SidebarSelectedBarConverter : IValueConverter
{
    // ���ഫ���Ω��������ɤޱ����C��
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // �p�G value �O true�]�Q����^�A�h�^�Ǻ�� #2ecc40
        if (value is bool isSelected && isSelected)
            return "#2ecc40";
        // ������h�^�ǳz��
        return "Transparent";
    }
    // ����k����@�A�]���u�Ω��V���ô��
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SidebarSelectedBackgroundConverter : IValueConverter
{
    // ���ഫ���Ω�������s���I����A�ھڬO�_��������C��
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // �p�G value �O true�]�Q����^�A�h�^�Ƕ¦� #232323
        if (value is bool isSelected && isSelected)
            return "#232323";
        // ������h�^�ǳz��
        return "Transparent";
    }
    // ����k����@�A�]���u�Ω��V���ô��
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
