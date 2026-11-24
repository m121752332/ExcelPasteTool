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
        new SidebarItem(SidebarPage.Tools, "SidebarTools", "工具", IconManager.DataIcon),
        // 設定：使用 SettingsIcon
        new SidebarItem(SidebarPage.Settings, "SidebarSettings", "設定", IconManager.SettingsIcon, "SidebarSettingsTooltip", "設定 (Ctrl+,)"),
        // 說明：使用 BookIcon（F1 也會開啟）；標籤不放快捷鍵，只在 ToolTip 顯示
        new SidebarItem(SidebarPage.Help, "SidebarHelp", "說明", IconManager.BookIcon, "SidebarHelpTooltip", "說明 (F1)"),
        // GitHub 專案頁：GitHub 貓圖示，在 App 內載入 GitHub 上的 README（需要網路）；名稱固定為 GitHub，不做多語系
        new SidebarItem(SidebarPage.GitHub, "SidebarGitHub", "GitHub", IconManager.GitHubIcon),
        // 關於：使用 HelpIcon（已內建）
        new SidebarItem(SidebarPage.About, "SidebarAbout", "關於", IconManager.GetIcon("help")),
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
        // F1：開啟說明；Alt 快捷鍵。用 Tunnel 攔截，焦點在輸入框時也有效
        AddHandler(KeyDownEvent, OnWindowKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        // 導覽中的按鍵放開事件也攔下，避免焦點所在的按鈕在空白鍵放開時再觸發一次點擊
        AddHandler(KeyUpEvent, (_, e) => { if (Tour.IsActive) e.Handled = true; }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        InitTour();
        InitTitleBar();
    }

    /// <summary>平台的主要修飾鍵：Windows / Linux 是 Ctrl，macOS 是 Command（⌘）。</summary>
    public static Avalonia.Input.KeyModifiers PrimaryModifier =>
        OperatingSystem.IsMacOS() ? Avalonia.Input.KeyModifiers.Meta : Avalonia.Input.KeyModifiers.Control;

    private void OnWindowKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        // Alt+F4 / Alt+Q / Alt+P（見 MainWindow.TitleBar.cs）
        if (HandleAltShortcut(e)) return;

        // 導覽進行中：方向鍵 / Enter / 空白鍵 / Esc 操作導覽，其餘按鍵不傳到底下的元件
        if (Tour.IsActive)
        {
            HandleTourKey(e);
            e.Handled = true;
            return;
        }
        // Ctrl+,（macOS 是 ⌘+,）：開啟設定頁，同 VS Code / 多數桌面 App 的習慣
        if (e.Key == Avalonia.Input.Key.OemComma && e.KeyModifiers == PrimaryModifier && !Tour.IsActive)
        {
            e.Handled = true;
            OnSidebarItemClick(SidebarPage.Settings);
            return;
        }
        // Insert：在工具頁資料表選取的列下面插入一筆（焦點在輸入框時不攔，避免干擾打字）
        if (e.Key == Avalonia.Input.Key.Insert && e.KeyModifiers == Avalonia.Input.KeyModifiers.None
            && Sidebar.SelectedPage == SidebarPage.Tools
            && FocusManager?.GetFocusedElement() is not TextBox
            && this.FindControl<ContentControl>("MainContent")?.Content is DataToolView insertTool)
        {
            e.Handled = true;
            insertTool.InsertBelowSelected();
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
        if (page == SidebarPage.GitHub && HelpService.ViewerMode == HelpViewerMode.Browser && HelpService.OpenExternal(HelpService.GitHubHelpUrl))
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