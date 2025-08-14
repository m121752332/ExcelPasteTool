using Avalonia.Controls;
using ExcelPasteTool.Helpers;

namespace ExcelPasteTool;

public partial class SettingsView : UserControl
{
    private ToastQueueHelper _toastHelper;

    public SettingsView()
    {
        InitializeComponent();
        _toastHelper = new ToastQueueHelper(this);
        var vm = new SettingsViewModel();
        vm.EnqueueToast = _toastHelper.EnqueueToast;
        this.DataContext = vm;
    }
}