using System.Collections;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ExcelPasteTool;

/// <summary>
/// 開發者工具（開發者模式開啟後在主視窗按 F12）：簡易版的 Avalonia DevTools。
/// <list type="bullet">
/// <item>左邊：主視窗的視覺樹，展開時才載入子節點，大畫面也不會卡。</item>
/// <item>右邊：選取控制項的常用資訊（型別、名稱、AutomationId、Bounds、Margin…）與全部 Avalonia 屬性，可篩選；有設定值的屬性用強調色。</item>
/// <item>選取的控制項會在 App 畫面上用半透明藍框標出來；在 App 上 Ctrl+Shift+點選也能直接選到那個控制項。</item>
/// </list>
/// 整個視窗用程式碼組出來，不另外放 XAML，也不依賴任何診斷套件，Release 發佈版一樣能用。
/// </summary>
public sealed class DevToolsWindow : Window
{
    /// <summary>標示框的名稱：視覺樹裡不列出它自己。</summary>
    internal const string HighlightName = "__DevToolsHighlight";

    private const string Placeholder = "…";
    private const int MaxValueLength = 300;

    /// <summary>被檢視的視窗（主視窗）。</summary>
    public Window Target { get; }

    internal TreeView Tree { get; }
    internal TextBox FilterBox { get; }

    /// <summary>目前選取的控制項。</summary>
    internal Visual? SelectedVisual { get; private set; }

    /// <summary>目前畫在 App 上的標示框（沒有選取時為 null）。</summary>
    internal Border? Highlight { get; private set; }

    /// <summary>目前列出的屬性（已套用篩選），測試用。</summary>
    internal IReadOnlyList<PropertyRow> VisibleProperties { get; private set; } = Array.Empty<PropertyRow>();

    private readonly StackPanel _propertyRows = new() { Spacing = 0 };
    private readonly TextBlock _selectedTitle = new() { FontWeight = FontWeight.Bold, TextTrimming = TextTrimming.CharacterEllipsis };
    private List<PropertyRow> _allProperties = new();

    internal readonly record struct PropertyRow(string Name, string Value, bool IsSet, bool IsHeader = false);

    public DevToolsWindow(Window target)
    {
        Target = target;
        Title = $"{LanguageManager.GetText("DevToolsTitle", "開發者工具")} - {target.Title}";
        Width = 1000;
        Height = 680;
        MinWidth = 600;
        MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        Tree = new TreeView { Name = "DevToolsTree" };
        Tree.SelectionChanged += (_, _) => OnTreeSelectionChanged();

        FilterBox = new TextBox
        {
            Name = "DevToolsFilterBox",
            PlaceholderText = LanguageManager.GetText("DevToolsFilterWatermark", "篩選屬性"),
            Margin = new Thickness(0, 0, 0, 8)
        };
        FilterBox.TextChanged += (_, _) => RenderProperties();

        Content = BuildLayout();
        BuildTree();
        SelectVisual(target);

        // App 上 Ctrl+Shift+點選：直接選取滑鼠底下的控制項（用 Tunnel 並攔下，避免同時觸發按鈕）
        Target.AddHandler(PointerPressedEvent, OnTargetPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        KeyDown += OnKeyDown;
        Closed += (_, _) =>
        {
            Target.RemoveHandler(PointerPressedEvent, OnTargetPointerPressed);
            ClearHighlight();
        };
    }

    // ------------------------------------------------------------------
    // 版面
    // ------------------------------------------------------------------

    private Control BuildLayout()
    {
        var refresh = new Button
        {
            Name = "DevToolsRefreshButton",
            Content = $"{LanguageManager.GetText("DevToolsRefresh", "重新整理")} (F5)",
            VerticalAlignment = VerticalAlignment.Center
        };
        refresh.Click += (_, _) => Refresh();

        var hint = new TextBlock
        {
            Text = LanguageManager.GetText("DevToolsHint", "Ctrl+Shift+點選 App 畫面可直接選取控制項"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            TextWrapping = TextWrapping.Wrap
        };
        hint.Bind(TextBlock.ForegroundProperty, hint.GetResourceObservable("TextSecondaryBrush"));

        var toolbar = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        DockPanel.SetDock(refresh, Dock.Left);
        toolbar.Children.Add(refresh);
        toolbar.Children.Add(hint);

        var treePanel = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = Tree
            }
        };
        treePanel.Bind(Border.BorderBrushProperty, treePanel.GetResourceObservable("PanelBorderBrush"));

        var propertyPanel = new DockPanel();
        DockPanel.SetDock(_selectedTitle, Dock.Top);
        DockPanel.SetDock(FilterBox, Dock.Top);
        _selectedTitle.Margin = new Thickness(0, 0, 0, 8);
        propertyPanel.Children.Add(_selectedTitle);
        propertyPanel.Children.Add(FilterBox);
        var propertyBorder = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _propertyRows
            }
        };
        propertyBorder.Bind(Border.BorderBrushProperty, propertyBorder.GetResourceObservable("PanelBorderBrush"));
        propertyPanel.Children.Add(propertyBorder);

        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,6,3*") };
        Grid.SetColumn(treePanel, 0);
        var splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns, Background = Brushes.Transparent };
        Grid.SetColumn(splitter, 1);
        Grid.SetColumn(propertyPanel, 2);
        body.Children.Add(treePanel);
        body.Children.Add(splitter);
        body.Children.Add(propertyPanel);

        var root = new DockPanel { Margin = new Thickness(12) };
        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);
        root.Children.Add(body);
        return root;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.None) return;
        if (e.Key == Key.F5)
        {
            e.Handled = true;
            Refresh();
        }
        else if (e.Key is Key.F12 or Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    // ------------------------------------------------------------------
    // 視覺樹
    // ------------------------------------------------------------------

    private void BuildTree()
    {
        Tree.Items.Clear();
        var root = CreateItem(Target);
        Tree.Items.Add(root);
        root.IsExpanded = true;
    }

    /// <summary>重新讀取視覺樹（畫面切換頁面後用），盡量保留原本選取的控制項。</summary>
    internal void Refresh()
    {
        var previous = SelectedVisual;
        BuildTree();
        if (previous == null || !SelectVisual(previous))
            SelectVisual(Target);
    }

    private TreeViewItem CreateItem(Visual visual)
    {
        var item = new TreeViewItem { Header = Describe(visual), Tag = visual };
        if (GetChildren(visual).Any())
            item.Items.Add(Placeholder);
        item.PropertyChanged += (_, e) =>
        {
            if (e.Property == TreeViewItem.IsExpandedProperty && e.NewValue is true)
                EnsureChildren(item);
        };
        return item;
    }

    private void EnsureChildren(TreeViewItem item)
    {
        if (item.Items.Count != 1 || item.Items[0] is not string) return;
        item.Items.Clear();
        foreach (var child in GetChildren((Visual)item.Tag!))
            item.Items.Add(CreateItem(child));
    }

    private static IEnumerable<Visual> GetChildren(Visual visual) =>
        visual.GetVisualChildren().Where(v => v is not Control { Name: HighlightName });

    /// <summary>樹狀節點的文字：型別 #名稱 .樣式類別，文字類控制項再附上前幾個字。</summary>
    internal static string Describe(Visual visual)
    {
        var text = visual.GetType().Name;
        if (visual is StyledElement { Name: { Length: > 0 } name })
            text += $" #{name}";
        if (visual is StyledElement element && element.Classes.Count > 0)
            text += " " + string.Join(" ", element.Classes.Where(c => !c.StartsWith(':')).Select(c => "." + c));
        var preview = visual switch
        {
            TextBlock tb => tb.Text,
            TextBox box => box.Text,
            _ => null
        };
        if (!string.IsNullOrEmpty(preview))
        {
            preview = preview.ReplaceLineEndings(" ");
            text += $"  \"{(preview.Length > 24 ? preview[..24] + "…" : preview)}\"";
        }
        return text;
    }

    /// <summary>在樹上選取指定的控制項（自動展開路徑）。不在主視窗視覺樹裡（例如彈出選單）時回傳 false。</summary>
    internal bool SelectVisual(Visual visual)
    {
        var path = new List<Visual> { visual };
        if (visual != Target)
        {
            foreach (var ancestor in visual.GetVisualAncestors())
            {
                path.Add(ancestor);
                if (ancestor == Target) break;
            }
            if (path[^1] != Target) return false;
        }
        path.Reverse();

        if (Tree.Items.Count == 0 || Tree.Items[0] is not TreeViewItem item || item.Tag != Target)
            return false;
        for (var i = 1; i < path.Count; i++)
        {
            EnsureChildren(item);
            item.IsExpanded = true;
            var next = item.Items.OfType<TreeViewItem>().FirstOrDefault(c => c.Tag == path[i]);
            if (next == null) return false;
            item = next;
        }

        Tree.SelectedItem = item;
        if (Tree.SelectedItem != item)
        {
            // 容器還沒建立時 TreeView 可能選不到，至少讓右邊的屬性與標示框跟上
            ShowSelection(visual);
        }
        Dispatcher.UIThread.Post(() => item.BringIntoView(), DispatcherPriority.Background);
        return true;
    }

    private void OnTreeSelectionChanged()
    {
        if (Tree.SelectedItem is TreeViewItem { Tag: Visual visual })
            ShowSelection(visual);
    }

    private void ShowSelection(Visual visual)
    {
        SelectedVisual = visual;
        _selectedTitle.Text = Describe(visual);
        _allProperties = CollectProperties(visual, Target);
        RenderProperties();
        ShowHighlight(visual);
    }

    private void OnTargetPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        const KeyModifiers pick = KeyModifiers.Control | KeyModifiers.Shift;
        if ((e.KeyModifiers & pick) != pick) return;
        e.Handled = true;
        if (Target.InputHitTest(e.GetPosition(Target)) is Visual hit)
        {
            SelectVisual(hit);
            Activate();
        }
    }

    // ------------------------------------------------------------------
    // 屬性
    // ------------------------------------------------------------------

    /// <summary>常用資訊在最上面，接著是全部的 Avalonia 屬性（依名稱排序）。</summary>
    internal static List<PropertyRow> CollectProperties(Visual visual, Visual? root)
    {
        var rows = new List<PropertyRow>();
        void Add(string name, object? value) => rows.Add(new PropertyRow(name, Format(value), true));

        Add("Type", visual.GetType().FullName);
        if (visual is StyledElement element)
        {
            Add("Name", element.Name);
            Add("Classes", string.Join(" ", element.Classes));
            Add("DataContext", element.DataContext?.GetType().FullName);
        }
        if (visual is Control control)
        {
            Add("AutomationId", AutomationProperties.GetAutomationId(control));
            Add("AutomationName", AutomationProperties.GetName(control));
        }
        Add("Bounds", visual.Bounds);
        if (root != null && visual.TranslatePoint(default, root) is { } position)
            Add("Position (window)", position);
        if (visual is Layoutable layoutable)
        {
            Add("Margin", layoutable.Margin);
            Add("DesiredSize", layoutable.DesiredSize);
            Add("Width × Height", $"{Format(layoutable.Width)} × {Format(layoutable.Height)}");
            Add("HorizontalAlignment", layoutable.HorizontalAlignment);
            Add("VerticalAlignment", layoutable.VerticalAlignment);
        }
        Add("IsVisible", visual.IsVisible);
        Add("IsEffectivelyVisible", visual.IsEffectivelyVisible);
        if (visual is InputElement input)
        {
            Add("IsEnabled", input.IsEnabled);
            Add("IsFocused", input.IsFocused);
        }

        rows.Add(new PropertyRow("Avalonia properties", "", false, IsHeader: true));
        foreach (var property in AvaloniaPropertyRegistry.Instance.GetRegistered(visual)
                     .DistinctBy(p => p.Name).OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            string value;
            try { value = Format(visual.GetValue(property)); }
            catch (Exception ex) { value = $"<{ex.GetType().Name}>"; }
            rows.Add(new PropertyRow(property.Name, value, visual.IsSet(property)));
        }
        return rows;
    }

    internal static string Format(object? value)
    {
        string text;
        try
        {
            text = value switch
            {
                null => "(null)",
                string s => s,
                double d => d.ToString(CultureInfo.InvariantCulture),
                ISolidColorBrush brush => $"SolidColorBrush {brush.Color}",
                IEnumerable and not string and not IFormattable => value.GetType().Name,
                _ => value.ToString() ?? ""
            };
        }
        catch (Exception ex)
        {
            text = $"<{ex.GetType().Name}>";
        }
        text = text.ReplaceLineEndings(" ");
        return text.Length > MaxValueLength ? text[..MaxValueLength] + "…" : text;
    }

    private void RenderProperties()
    {
        var filter = FilterBox.Text?.Trim() ?? "";
        VisibleProperties = filter.Length == 0
            ? _allProperties
            : _allProperties.Where(p => !p.IsHeader && p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        _propertyRows.Children.Clear();
        if (SelectedVisual == null)
        {
            _propertyRows.Children.Add(new TextBlock
            {
                Text = LanguageManager.GetText("DevToolsNoSelection", "請在左側選取一個控制項"),
                Margin = new Thickness(10)
            });
            return;
        }

        foreach (var row in VisibleProperties)
            _propertyRows.Children.Add(CreateRow(row));
    }

    private static Control CreateRow(PropertyRow row)
    {
        if (row.IsHeader)
        {
            var header = new TextBlock { Text = row.Name, FontWeight = FontWeight.Bold, Margin = new Thickness(10, 12, 10, 4) };
            return header;
        }

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("200,*"), Margin = new Thickness(10, 2) };
        var name = new SelectableTextBlock { Text = row.Name, TextTrimming = TextTrimming.CharacterEllipsis };
        var value = new SelectableTextBlock { Text = row.Value, TextWrapping = TextWrapping.Wrap };
        name.Bind(TextBlock.ForegroundProperty, name.GetResourceObservable(row.IsSet ? "AccentTextBrush" : "TextSecondaryBrush"));
        Grid.SetColumn(value, 1);
        grid.Children.Add(name);
        grid.Children.Add(value);
        return grid;
    }

    // ------------------------------------------------------------------
    // 在 App 上標出選取的控制項
    // ------------------------------------------------------------------

    private void ShowHighlight(Visual visual)
    {
        ClearHighlight();
        if (visual == Target || AdornerLayer.GetAdornerLayer(visual) is not { } layer) return;

        var border = new Border
        {
            Name = HighlightName,
            Background = new SolidColorBrush(Color.FromArgb(0x40, 0x1F, 0x6F, 0xEB)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xEB)),
            BorderThickness = new Thickness(2),
            IsHitTestVisible = false
        };
        AdornerLayer.SetAdornedElement(border, visual);
        layer.Children.Add(border);
        Highlight = border;
    }

    private void ClearHighlight()
    {
        if (Highlight?.Parent is AdornerLayer layer)
            layer.Children.Remove(Highlight);
        Highlight = null;
    }
}
