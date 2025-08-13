using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ExcelPasteTool;

public partial class MainWindow : Window
{
    public SidebarViewModel Sidebar { get; } = new();
    public ObservableCollection<SidebarItem> SidebarItems { get; } = new()
    {
        new SidebarItem(SidebarPage.Tools, "工具", "M3,6h18v2H3V6zm0,5h18v2H3v-2zm0,5h18v2H3v-2z"),
        new SidebarItem(SidebarPage.Settings, "設定", "M12,8a4,4 0 1,0 0,8a4,4 0 1,0 0,-8zm8.94,4a7.97,7.97 0 0,0 -.34-2l2.12-1.65a.5.5 0 0,0 .12-.64l-2-3.46a.5.5 0 0,0 -.6-.22l-2.49,1a8.12,8.12 0 0,0 -1.7-.99l-.38-2.65A.5.5 0 0,0 14,2h-4a.5.5 0 0,0 -.5.42l-.38,2.65a8.12,8.12 0 0,0 -1.7.99l-2.49-1a.5.5 0 0,0 -.6.22l-2,3.46a.5.5 0 0,0 .12.64l2.12,1.65a7.97,7.97 0 0,0 -.34,2l-2.12,1.65a.5.5 0 0,0 -.12.64l2,3.46a.5.5 0 0,0 .6.22l2.49-1c.54.38,1.12.71,1.7.99l.38,2.65A.5.5 0 0,0 10,22h4a.5.5 0 0,0 .5-.42l.38-2.65c.58-.28,1.16-.61,1.7-.99l2.49,1a.5.5 0 0,0 .6-.22l2-3.46a.5.5 0 0,0 -.12-.64l-2.12-1.65z"),
        new SidebarItem(SidebarPage.About, "關於", "M12,2a10,10 0 1,0 0,20a10,10 0 1,0 0,-20zm1,17h-2v-2h2v2zm1.07-7.75l-.9.92C12.45,12.9 12,13.5 12,15h-2v-.5c0-1 .45-1.99 1.17-2.71l1.24-1.26a2,2 0 1,0 -2.83-2.83a2,2 0 0,0 0,2.83l.88.88"),
    };
    public ICommand SidebarItemClickCommand { get; }

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = this;
        foreach (var item in SidebarItems)
            item.SidebarViewModelInstance = Sidebar;
        SidebarItemClickCommand = new RelayCommand<SidebarPage>(OnSidebarItemClick);
        ThemeManager.ThemeChanged += OnThemeChanged;
        // 載入側邊欄主題樣式
        LoadSidebarTheme();
    }

    private void OnSidebarItemClick(SidebarPage page)
    {
        Sidebar.SelectedPage = page;
        foreach (var item in SidebarItems)
            item.OnPropertyChanged(nameof(item.IsSelected));
    }

    private void SidebarToggleButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Sidebar.Toggle();
    }

    private void OnThemeChanged()
    {
        LoadSidebarTheme();
    }

    private void LoadSidebarTheme()
    {
        var app = Avalonia.Application.Current;
        if (app == null) return;
        // 先移除舊的 SidebarTheme
        for (int i = app.Styles.Count - 1; i >= 0; i--)
        {
            if (app.Styles[i] is StyleInclude styleInclude &&
                styleInclude.Source != null &&
                styleInclude.Source.AbsolutePath.Contains("SidebarTheme"))
            {
                app.Styles.RemoveAt(i);
            }
        }
        // 重新載入 SidebarTheme.axaml
        var sidebarTheme = new StyleInclude(new System.Uri("avares://ExcelPasteTool/"))
        {
            Source = new System.Uri("avares://ExcelPasteTool/Themes/SidebarTheme.axaml")
        };
        app.Styles.Add(sidebarTheme);
    }
}

public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Func<T, bool>? _canExecute;
    public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }
    public bool CanExecute(object? parameter) => _canExecute?.Invoke((T)parameter!) ?? true;
    public void Execute(object? parameter) => _execute((T)parameter!);
    public event EventHandler? CanExecuteChanged;
}