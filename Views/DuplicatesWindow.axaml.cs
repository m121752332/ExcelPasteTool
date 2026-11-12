using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Collections.Generic;
using System.Linq;

namespace ExcelPasteTool;

/// <summary>
/// 重複項視窗：列出最近一次「資料貼上」時被濾掉的重複值，每個值一列（出現次數、被移除筆數）。
/// 純參考用，不會改動資料表。
/// </summary>
public partial class DuplicatesWindow : Window
{
    // 給 XAML 載入器用
    public DuplicatesWindow() : this(new List<DuplicateEntry>(), 0) { }

    /// <param name="duplicates">有重複的值（已排序）。</param>
    /// <param name="pastedCount">貼上的總筆數（去重複前）。</param>
    public DuplicatesWindow(IReadOnlyList<DuplicateEntry> duplicates, int pastedCount)
    {
        AvaloniaXamlLoader.Load(this);

        var title = LanguageManager.GetText("DuplicatesTitle", "重複項");
        Title = title;
        this.FindControl<TextBlock>("HeaderText")!.Text = title;
        this.FindControl<TextBlock>("ValueHeader")!.Text = LanguageManager.GetText("DataContentHeader", "資料內容");
        this.FindControl<TextBlock>("CountHeader")!.Text = LanguageManager.GetText("DuplicatesCountHeader", "出現次數");
        this.FindControl<TextBlock>("RemovedHeader")!.Text = LanguageManager.GetText("DuplicatesRemovedHeader", "被移除");
        this.FindControl<Button>("CloseButton")!.Content = LanguageManager.GetText("ChangelogCloseButton", "關閉");

        var removed = duplicates.Sum(d => d.Removed);
        this.FindControl<TextBlock>("SummaryText")!.Text = string.Format(
            LanguageManager.GetText("DuplicatesSummaryFormat", "共貼上 {0} 筆，保留 {1} 筆，移除 {2} 筆"),
            pastedCount, pastedCount - removed, removed);

        var rows = this.FindControl<StackPanel>("RowList")!;
        for (int i = 0; i < duplicates.Count; i++)
            rows.Children.Add(BuildRow(duplicates[i], isEven: i % 2 == 1));
    }

    private static Grid BuildRow(DuplicateEntry entry, bool isEven)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,96,96"), MinHeight = 30 };
        row.Classes.Add("data-row");
        if (isEven) row.Classes.Add("even");

        row.Children.Add(Cell(0, new Avalonia.Thickness(0, 0, 1, 1), new SelectableTextBlock
        {
            Text = entry.Value,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(10, 5),
            VerticalAlignment = VerticalAlignment.Center
        }));
        row.Children.Add(Cell(1, new Avalonia.Thickness(0, 0, 1, 1), Number(entry.Count)));
        row.Children.Add(Cell(2, new Avalonia.Thickness(0, 0, 0, 1), Number(entry.Removed)));
        return row;
    }

    private static TextBlock Number(int value) => new()
    {
        Text = value.ToString(),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static Border Cell(int column, Avalonia.Thickness border, Control child)
    {
        var cell = new Border { BorderThickness = border, Child = child };
        cell.Classes.Add("grid-cell");
        Grid.SetColumn(cell, column);
        return cell;
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();
}
