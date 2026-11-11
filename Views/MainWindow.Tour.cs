using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ExcelPasteTool;

/// <summary>
/// MainWindow 的 App 導覽部分：
/// 第一次開啟（config.json 的 TourCompleted 為 false）自動播放；之後可按右上角「導覽」按鈕重看。
/// </summary>
public partial class MainWindow
{
    private TourOverlay Tour => this.FindControl<TourOverlay>("TourLayer")!;

    /// <summary>導覽結束後要執行的動作（首次啟動時延後的版本檢查）。</summary>
    private Action? _afterTour;

    private void InitTour()
    {
        Tour.StepChanging += _ => EnsureToolsPage();
        Tour.Closed += OnTourClosed;
        UpdateTourButtonText();
        UpdateTourButtonVisibility();
        LanguageManager.LanguageChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(UpdateTourButtonText);
    }

    private void UpdateTourButtonText()
    {
        if (this.FindControl<TextBlock>("TourButtonText") is { } text)
            text.Text = LanguageManager.GetText("TourButton", "導覽");
        if (this.FindControl<Button>("TourButton") is { } button)
            ToolTip.SetTip(button, LanguageManager.GetText("TourButtonTooltip", "重看操作導覽"));
    }

    /// <summary>導覽只介紹「工具」頁，所以按鈕也只在「工具」頁顯示。</summary>
    private void UpdateTourButtonVisibility()
    {
        if (this.FindControl<Button>("TourButton") is { } button)
            button.IsVisible = Sidebar.SelectedPage == SidebarPage.Tools;
    }

    /// <summary>首次啟動時是否需要自動播放導覽。</summary>
    private static bool ShouldAutoStartTour => !ConfigServices.Config.TourCompleted;

    public void StartTour()
    {
        if (Tour.IsActive) return;
        EnsureToolsPage();
        Tour.Start(BuildTourSteps());
    }

    private void TourButton_Click(object? sender, RoutedEventArgs e) => StartTour();

    private void OnTourClosed(bool completed)
    {
        // 走完或略過都視為看過，下次啟動不再自動播放
        if (!ConfigServices.Config.TourCompleted)
        {
            ConfigServices.Config.TourCompleted = true;
            ConfigServices.Save();
        }
        var next = _afterTour;
        _afterTour = null;
        next?.Invoke();
    }

    /// <summary>導覽期間的鍵盤操作：Esc 略過、← 上一步、→ / Enter 下一步。回傳 true 代表已處理。</summary>
    private bool HandleTourKey(KeyEventArgs e)
    {
        if (!Tour.IsActive) return false;
        switch (e.Key)
        {
            case Key.Escape: Tour.Close(false); break;
            case Key.Left: Tour.Previous(); break;
            case Key.Right:
            case Key.Enter: Tour.Next(); break;
            case Key.Tab: break; // 不讓焦點跳到遮罩底下的元件
            default: return false;
        }
        return true;
    }

    private void EnsureToolsPage()
    {
        if (Sidebar.SelectedPage != SidebarPage.Tools)
            OnSidebarItemClick(SidebarPage.Tools);
    }

    private DataToolView? ToolsView => this.FindControl<ContentControl>("MainContent")?.Content as DataToolView;

    private Control? ToolsControl(string name) => ToolsView?.FindControl<Control>(name);

    private List<TourStep> BuildTourSteps() => new()
    {
        new TourStep
        {
            TitleKey = "TourWelcomeTitle", TitleDefault = "歡迎使用 DataSplitter Pro 👋",
            BodyKey = "TourWelcomeBody",
            BodyDefault = "這個工具能把從 Excel 複製的一欄資料，轉成以分隔符號串接的字串與 SQL 條件。\n接下來花 30 秒帶你看一遍主要操作，可隨時按 Esc 略過。",
        },
        new TourStep
        {
            TitleKey = "TourSidebarTitle", TitleDefault = "功能選單",
            BodyKey = "TourSidebarBody",
            BodyDefault = "從左側切換「工具」「設定」「說明」「關於」頁面；點最上方的按鈕可以展開或收合選單。",
            Targets = () => new Control?[] { this.FindControl<Control>("SidebarBorder") },
        },
        new TourStep
        {
            TitleKey = "TourPasteTitle", TitleDefault = "① 貼上 Excel 資料",
            BodyKey = "TourPasteBody",
            BodyDefault = "先在 Excel 選取並複製一欄（Ctrl+C），再按「資料貼上」。每一格會變成一筆資料，重複的值會自動排除；若複製了多欄，只取第一欄。",
            Targets = () => new[] { ToolsControl("PasteDataButton") },
        },
        new TourStep
        {
            TitleKey = "TourTableTitle", TitleDefault = "資料清單",
            BodyKey = "TourTableBody",
            BodyDefault = "貼上的資料會列在這裡。滑鼠移到某一列，右側會出現 ✕ 可移除該筆；點表頭的「行號」可複製全部資料。",
            Targets = () => new[] { ToolsControl("TableContainer") },
        },
        new TourStep
        {
            TitleKey = "TourSeparatorTitle", TitleDefault = "② 選擇分隔符號",
            BodyKey = "TourSeparatorBody",
            BodyDefault = "決定資料要用哪個符號串接：豎線 |、分號 ;、冒號 : 或空白，切換後結果立即更新。",
            Targets = () => new[] { ToolsControl("SeparatorPanel") },
        },
        new TourStep
        {
            TitleKey = "TourTopBoxTitle", TitleDefault = "③ 分隔資料",
            BodyKey = "TourTopBoxBody",
            BodyDefault = "資料會自動串成一行，例如 A001|A002|A003。也可以直接在這裡輸入或修改，右邊的清單會跟著更新。",
            Targets = () => new[] { ToolsControl("TopBox") },
        },
        new TourStep
        {
            TitleKey = "TourSqlTitle", TitleDefault = "④ SQL 條件",
            BodyKey = "TourSqlBody",
            BodyDefault = "同時產生 SQL 的 IN 條件，例如 ('A001', 'A002')，單引號會自動跳脫，可直接貼到查詢語法中。",
            Targets = () => new[] { ToolsControl("BottomBox") },
        },
        new TourStep
        {
            TitleKey = "TourCopyTitle", TitleDefault = "一鍵複製",
            BodyKey = "TourCopyBody",
            BodyDefault = "按標題右側的複製鈕，就能把分隔資料或 SQL 條件複製到剪貼簿。",
            Targets = () => new[] { ToolsControl("CopyTopBoxButton") },
        },
        new TourStep
        {
            TitleKey = "TourClearTitle", TitleDefault = "清空資料",
            BodyKey = "TourClearBody",
            BodyDefault = "要處理下一批資料時，按這裡清除畫面並還原分隔符號。",
            Targets = () => new[] { ToolsControl("ClearDataButton") },
        },
        new TourStep
        {
            TitleKey = "TourAppearanceTitle", TitleDefault = "語言與主題",
            BodyKey = "TourAppearanceBody",
            BodyDefault = "這裡可以臨時切換介面語言與配色主題；要永久保存，或調整字型、字體大小，請到「設定」頁。",
            Targets = () => new[] { ToolsControl("LanguagePanel"), ToolsControl("ThemePanel") },
        },
        new TourStep
        {
            TitleKey = "TourReplayTitle", TitleDefault = "隨時重看導覽",
            BodyKey = "TourReplayBody",
            BodyDefault = "之後想再看一次，按右上角的「導覽」按鈕即可；按 F1 可以開啟完整的使用說明。祝使用愉快！",
            Targets = () => new Control?[] { this.FindControl<Control>("TourButton") },
        },
    };
}
