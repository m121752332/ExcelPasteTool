using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input.Platform;
using System.Collections.ObjectModel;
using System.Linq;

namespace ExcelPasteTool;

public partial class MainWindow : Window
{
    public ObservableCollection<string> Items { get; } = new();
    private char _separator = '|';

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private async void PasteData_Click(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            var text = await clipboard.GetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                var lines = text.Split(new[] {'\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                Items.Clear();
                foreach (var line in lines)
                    Items.Add(line.Trim());

                UpdateTopAndBottom();
            }
        }
    }

    private void TopBoxChanged(object? sender, TextChangedEventArgs e)
    {
        var text = TopBox.Text ?? string.Empty;
        var parts = text.Split(_separator).Select(x => x.Trim()).Where(x => x.Length > 0);
        Items.Clear();
        foreach (var p in parts)
            Items.Add(p);

        UpdateBottom();
    }

    private void SeparatorChanged(object? sender, TextChangedEventArgs e)
    {
        var input = SeparatorBox.Text ?? "";
        _separator = (input.Length == 1) ? input[0] : '|';
        SeparatorBox.Text = _separator.ToString();
        UpdateTopAndBottom();
    }

    private void UpdateTopAndBottom()
    {
        TopBox.Text = string.Join(_separator, Items);
        UpdateBottom();
    }

    private void UpdateBottom()
    {
        BottomBox.Text = $"({string.Join(", ", Items.Select(x => $"'{x}'"))})";
    }
}