using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExcelPasteTool;

public class SidebarItem : INotifyPropertyChanged
{
    public SidebarPage Page { get; }
    public string Name { get; }
    public string Icon { get; }
    public SidebarItem(SidebarPage page, string name, string icon)
    {
        Page = page;
        Name = name;
        Icon = icon;
    }
    public bool IsSelected => SidebarViewModelInstance?.SelectedPage == Page;
    public SidebarViewModel? SidebarViewModelInstance { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
