using Avalonia.Controls;
using Avalonia.Input;
using ExcelPasteTool.Helpers;
using System.Diagnostics;

namespace ExcelPasteTool;

public partial class SettingsView : UserControl
{
    private ToastQueueHelper _toastHelper;

    public SettingsView()
    {
        Debug.WriteLine($"SettingsView ctor: {SettingsViewModel.Instance.GetHashCode()}");
        InitializeComponent();
        _toastHelper = new ToastQueueHelper(this);
        var vm = SettingsViewModel.Instance;
        vm.EnqueueToast = _toastHelper.EnqueueToast;
        DataContext = vm;

        // 第 2 層 → 第 3 層
        GeneralCategoryButton.Click += (_, _) => NavigateTo(SettingsCategory.General);
        PreferencesCategoryButton.Click += (_, _) => NavigateTo(SettingsCategory.Preferences);
        UpdateCategoryButton.Click += (_, _) => NavigateTo(SettingsCategory.Update);
        DeveloperCategoryButton.Click += (_, _) => NavigateTo(SettingsCategory.Developer);
        BreadcrumbRootButton.Click += (_, _) => NavigateTo(SettingsCategory.Home);

        FontSizeExpandButton.Click += (_, _) => vm.IsFontSizeExpanded = !vm.IsFontSizeExpanded;
        DeveloperGroupExpandButton.Click += (_, _) => vm.IsDeveloperGroupExpanded = !vm.IsDeveloperGroupExpanded;

        // 展開 / 收合動畫：狀態仍存在 ViewModel，畫面跟著動畫過去
        var fontSizeAnimator = new ExpanderAnimator(FontSizeExpandButton, FontSizeDetail, FontSizeChevron, vm.IsFontSizeExpanded);
        var developerAnimator = new ExpanderAnimator(DeveloperGroupExpandButton, DeveloperGroupBody, DeveloperGroupChevron, vm.IsDeveloperGroupExpanded);
        void OnVmChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsViewModel.IsFontSizeExpanded))
                fontSizeAnimator.SetExpanded(vm.IsFontSizeExpanded);
            else if (e.PropertyName == nameof(SettingsViewModel.IsDeveloperGroupExpanded))
                developerAnimator.SetExpanded(vm.IsDeveloperGroupExpanded);
        }
        // ViewModel 是單例，頁面卸載時要退訂，避免舊頁面一直被留著
        AttachedToVisualTree += (_, _) =>
        {
            vm.PropertyChanged -= OnVmChanged;
            vm.PropertyChanged += OnVmChanged;
            fontSizeAnimator.SetExpanded(vm.IsFontSizeExpanded);
            developerAnimator.SetExpanded(vm.IsDeveloperGroupExpanded);
        };
        DetachedFromVisualTree += (_, _) => vm.PropertyChanged -= OnVmChanged;
        DecreaseFontButton.Click += (_, _) => vm.DecreaseFontSize();
        IncreaseFontButton.Click += (_, _) => vm.IncreaseFontSize();
        ResetFontButton.Click += (_, _) => vm.ResetFont();

        CheckUpdateButton.Click += CheckUpdateButton_OnClick;
        ChangelogButton.Click += ChangelogButton_OnClick;

        // Alt+← 與滑鼠側鍵「上一頁」回到設定首頁（同 Win11）
        AddHandler(KeyDownEvent, OnKeyDownTunnel, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        PointerPressed += OnPointerPressed;
    }

    private void NavigateTo(SettingsCategory category)
    {
        SettingsViewModel.Instance.NavigateTo(category);
        SettingsScroll.Offset = default;
    }

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Left && e.KeyModifiers == KeyModifiers.Alt && SettingsViewModel.Instance.IsCategoryPage)
        {
            NavigateTo(SettingsCategory.Home);
            e.Handled = true;
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsXButton1Pressed && SettingsViewModel.Instance.IsCategoryPage)
        {
            NavigateTo(SettingsCategory.Home);
            e.Handled = true;
        }
    }

    private async void CheckUpdateButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        var vm = SettingsViewModel.Instance;
        if (vm.IsCheckingUpdate) return;
        vm.IsCheckingUpdate = true;
        try
        {
            await MainWindow.CheckForUpdateManuallyAsync(owner);
        }
        finally
        {
            vm.IsCheckingUpdate = false;
        }
    }

    private async void ChangelogButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner) return;
        await new ChangelogWindow().ShowDialog(owner);
    }
}
