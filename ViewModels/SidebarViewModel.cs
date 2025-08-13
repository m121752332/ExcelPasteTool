using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExcelPasteTool;

public enum SidebarPage
{
    Tools,
    Settings,
    About
}

public class SidebarViewModel : INotifyPropertyChanged
{
    private bool _isExpanded = false;
    private SidebarPage _selectedPage = SidebarPage.Tools;

    public bool IsExpanded
    {
        get => _isExpanded;
        set { if (_isExpanded != value) { _isExpanded = value; OnPropertyChanged(); } }
    }

    public SidebarPage SelectedPage
    {
        get => _selectedPage;
        set { if (_selectedPage != value) { _selectedPage = value; OnPropertyChanged(); } }
    }

    public void Toggle()
    {
        IsExpanded = !IsExpanded;
    }

    public void Select(SidebarPage page)
    {
        SelectedPage = page;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
