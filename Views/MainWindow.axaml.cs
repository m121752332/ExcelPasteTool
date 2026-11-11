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
        // 工具：使用 DataIcon
        new SidebarItem(SidebarPage.Tools, "工具", IconManager.DataIcon),
        // 設定：使用 SettingsIcon
        new SidebarItem(SidebarPage.Settings, "設定", IconManager.SettingsIcon),
        // 說明：使用 BookIcon（F1 也會開啟）
        new SidebarItem(SidebarPage.Help, "說明 (F1)", IconManager.BookIcon),
        // 關於：使用 HelpIcon（已內建）
        new SidebarItem(SidebarPage.About, "關於", IconManager.GetIcon("help")),
    };
    public ICommand SidebarItemClickCommand { get; }

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = this;
        foreach (var item in SidebarItems)
            item.SidebarViewModelInstance = Sidebar;
        SidebarItemClickCommand = new RelayCommand<SidebarPage>(OnSidebarItemClick);
        // 設定視窗標題為組件名稱
        this.Title = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "App";
        // F1：開啟說明。用 Tunnel 攔截，焦點在輸入框時也有效
        AddHandler(KeyDownEvent, OnWindowKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        InitTour();
    }

    private void OnWindowKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        // 導覽進行中：方向鍵 / Enter / Esc 操作導覽，其餘按鍵不傳到底下的元件
        if (Tour.IsActive)
        {
            HandleTourKey(e);
            e.Handled = true;
            return;
        }
        if (e.Key == Avalonia.Input.Key.F1 && e.KeyModifiers == Avalonia.Input.KeyModifiers.None)
        {
            e.Handled = true;
            OnSidebarItemClick(SidebarPage.Help);
        }
    }

    private void OnSidebarItemClick(SidebarPage page)
    {
        // 說明設定為「預設瀏覽器」時不切換頁面，直接開瀏覽器；開啟失敗才退回內建說明頁
        if (page == SidebarPage.Help && HelpService.ViewerMode == HelpViewerMode.Browser && HelpService.OpenInBrowser())
            return;

        Sidebar.SelectedPage = page;
        UpdateTourButtonVisibility();
        foreach (var item in SidebarItems)
            item.OnPropertyChanged(nameof(item.IsSelected));
    }

    private void SidebarToggleButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Sidebar.Toggle();
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
#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
}