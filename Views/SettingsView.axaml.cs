using Avalonia.Controls;
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
        SettingsViewModel.Instance.EnqueueToast = _toastHelper.EnqueueToast;
        DataContext = SettingsViewModel.Instance;
    }
}