using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Reflection;

namespace ExcelPasteTool;

public partial class MainWindow : Window
{
    public SidebarViewModel Sidebar { get; } = new();
    public ObservableCollection<SidebarItem> SidebarItems { get; } = new()
    {
        // �u��G�ϥ� DataIcon
        new SidebarItem(SidebarPage.Tools, "�u��", IconManager.DataIcon),
        // �]�w�G�ϥ� SettingsIcon
        new SidebarItem(SidebarPage.Settings, "�]�w", IconManager.SettingsIcon),
        // ����G�ϥ� HelpIcon (�w�])
        new SidebarItem(SidebarPage.About, "����", IconManager.GetIcon("help")),
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
        // ���J������D�D�˦�
        LoadSidebarTheme();
        // �]�w�������D�� AssemblyName
        this.Title = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "App";
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

        // �ϥ� Global.AssemblyName
        var assemblyName = Global.AssemblyName;

        // �������ª� SidebarTheme
        for (int i = app.Styles.Count - 1; i >= 0; i--)
        {
            if (app.Styles[i] is StyleInclude styleInclude &&
                styleInclude.Source != null &&
                styleInclude.Source.AbsolutePath.Contains("SidebarTheme"))
            {
                app.Styles.RemoveAt(i);
            }
        }
        // �ʺA�զX avares ���|
        var sidebarTheme = new StyleInclude(new System.Uri($"avares://{assemblyName}/"))
        {
            Source = new System.Uri($"avares://{assemblyName}/Themes/SidebarTheme.axaml")
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