using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace ExcelPasteTool;

/// <summary>
/// 改版資訊視窗：每個版本一個可收合區塊（標題 = 版本 + 日期），預設只展開最新版。
/// 資料來自 ChangelogService（Resources/Changelog.json）；視窗文字跟隨 App 語言。
/// </summary>
public partial class ChangelogWindow : Window
{
    private readonly TextBlock _headerText;
    private readonly StackPanel _versionList;
    private readonly Button _closeButton;

    public ChangelogWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _headerText = this.FindControl<TextBlock>("HeaderText")!;
        _versionList = this.FindControl<StackPanel>("VersionList")!;
        _closeButton = this.FindControl<Button>("CloseButton")!;

        Build(expandedVersion: null);
        LanguageManager.LanguageChanged += OnLanguageChanged;
        Closed += (_, _) => LanguageManager.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged() => Dispatcher.UIThread.Post(() =>
    {
        // 保留目前展開的版本
        var expanded = _versionList.Children.OfType<Expander>().FirstOrDefault(e => e.IsExpanded)?.Tag as string;
        Build(expanded);
    });

    private void Build(string? expandedVersion)
    {
        var title = LanguageManager.GetText("ChangelogTitle", "改版資訊");
        Title = title;
        _headerText.Text = title;
        _closeButton.Content = LanguageManager.GetText("ChangelogCloseButton", "關閉");

        _versionList.Children.Clear();
        var entries = ChangelogService.Load();
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var isLatest = i == 0;

            var header = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
            header.Children.Add(new TextBlock
            {
                Text = $"v{entry.Version}  ·  {entry.Date}",
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            });
            if (isLatest)
            {
                header.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#0ABAB5")),
                    CornerRadius = new Avalonia.CornerRadius(8),
                    Padding = new Avalonia.Thickness(8, 1),
                    Child = new TextBlock
                    {
                        Text = LanguageManager.GetText("ChangelogLatestBadge", "最新"),
                        Foreground = Brushes.White,
                        FontSize = 11
                    }
                });
            }

            var body = new StackPanel { Spacing = 4, Margin = new Avalonia.Thickness(4, 2) };
            foreach (var note in ChangelogService.GetNotes(entry, LanguageManager.CurrentLanguage))
            {
                body.Children.Add(new TextBlock { Text = "•  " + note, TextWrapping = TextWrapping.Wrap });
            }

            _versionList.Children.Add(new Expander
            {
                Tag = entry.Version,
                Header = header,
                Content = body,
                IsExpanded = expandedVersion != null ? entry.Version == expandedVersion : isLatest,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            });
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();
}
