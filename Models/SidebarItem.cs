using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Threading;

namespace ExcelPasteTool;

public class SidebarItem : INotifyPropertyChanged
{
    private readonly string _nameKey;
    private readonly string _defaultName;
    private readonly string? _tooltipKey;
    private readonly string? _defaultTooltip;

    public SidebarPage Page { get; }
    // 名稱依目前語系取得，切換語言時會通知 UI 更新
    public string Name => LanguageManager.GetText(_nameKey, _defaultName);
    // 滑鼠提示：有指定時用自己的文字（例如附上快捷鍵），否則跟名稱相同
    public string ToolTipText => _tooltipKey is null ? Name : LanguageManager.GetText(_tooltipKey, _defaultTooltip ?? _defaultName);
    public string Icon { get; }
    public SidebarItem(SidebarPage page, string nameKey, string defaultName, string icon,
        string? tooltipKey = null, string? defaultTooltip = null)
    {
        _tooltipKey = tooltipKey;
        _defaultTooltip = defaultTooltip;
        Page = page;
        _nameKey = nameKey;
        _defaultName = defaultName;
        Icon = icon;
        // 側邊列項目與主視窗同生命週期，不需解除訂閱
        LanguageManager.LanguageChanged += () => Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(ToolTipText));
        });
    }
    public bool IsSelected => SidebarViewModelInstance?.SelectedPage == Page;
    public SidebarViewModel? SidebarViewModelInstance { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
