using Avalonia.Controls;
using Avalonia.Threading;

namespace ExcelPasteTool;

/// <summary>
/// MainWindow 的版本檢查部分：啟動後在背景檢查 GitHub 是否有新版。
/// 獨立成 partial 檔，避免與主視窗其他邏輯互相干擾。
/// </summary>
public partial class MainWindow
{
    private static bool _startupUpdateChecked;

    /// <summary>UI 自動測試設為 true：開啟視窗時不檢查更新、不自動播放導覽，畫面才穩定可比對。</summary>
    internal static bool SkipStartupChecks { get; set; }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (_startupUpdateChecked || SkipStartupChecks) return;
        _startupUpdateChecked = true;

        var args = Environment.GetCommandLineArgs();
        if (args.Contains(UpdateService.UpdatedArgument))
        {
            // 由更新腳本重新啟動：顯示更新完成並清除暫存檔
            UpdateService.CleanupWorkDirectory();
            Dispatcher.UIThread.Post(async () =>
            {
                var dlg = UpdateDialog.ForMessage("🎉", LanguageManager.GetText("UpdateDoneTitle", "更新完成"),
                    string.Format(LanguageManager.GetText("UpdateDoneBody", "已成功更新至 v{0}，歡迎使用新版本！"), UpdateService.CurrentVersionText));
                await dlg.ShowDialog(this);
                // 從舊版更新上來、還沒看過導覽的使用者，關掉提示後播放一次
                if (ShouldAutoStartTour) StartTour();
            });
            return;
        }

        if (args.Contains(UpdateService.UpdateFailedArgument))
        {
            Dispatcher.UIThread.Post(async () =>
            {
                var log = Path.Combine(UpdateService.WorkDirectory, "update.log");
                var dlg = UpdateDialog.ForMessage("!", LanguageManager.GetText("UpdateIncompleteTitle", "更新未完成"),
                    string.Format(LanguageManager.GetText("UpdateIncompleteBody", "套用更新時發生問題，目前仍為 v{0}。"), UpdateService.CurrentVersionText),
                    File.Exists(log) ? string.Format(LanguageManager.GetText("UpdateLogPath", "詳細記錄：{0}"), log) : null);
                await dlg.ShowDialog(this);
            });
            return;
        }

        if (ShouldAutoStartTour)
        {
            // 第一次開啟：先播放導覽，看完（或略過）後再檢查更新，避免更新視窗蓋住導覽
            _afterTour = () => _ = CheckForUpdateOnStartupAsync();
            Dispatcher.UIThread.Post(StartTour, DispatcherPriority.Background);
            return;
        }

        _ = CheckForUpdateOnStartupAsync();
    }

    /// <summary>
    /// 啟動時靜默檢查；沒有新版或網路不通都不打擾使用者。
    /// </summary>
    private async Task CheckForUpdateOnStartupAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var info = await UpdateService.CheckForUpdateAsync(cts.Token);
            if (info == null) return;

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (!IsVisible) return;
                await UpdateDialog.ForUpdate(info).ShowDialog(this);
            });
        }
        catch
        {
            // 離線或 GitHub API 限流時忽略，使用者仍可在「關於」頁手動檢查
        }
    }

    /// <summary>
    /// 手動檢查更新（「關於」頁的按鈕使用），會顯示結果。
    /// </summary>
    public static async Task CheckForUpdateManuallyAsync(Window owner)
    {
        UpdateDialog dlg;
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var info = await UpdateService.CheckForUpdateAsync(cts.Token);
            dlg = info != null
                ? UpdateDialog.ForUpdate(info)
                : UpdateDialog.ForMessage("✓", LanguageManager.GetText("UpdateUpToDateTitle", "已是最新版本"),
                    string.Format(LanguageManager.GetText("UpdateUpToDateBody", "目前版本 v{0} 已是最新。"), UpdateService.CurrentVersionText));
        }
        catch (Exception ex)
        {
            dlg = UpdateDialog.ForMessage("!", LanguageManager.GetText("UpdateCheckFailedTitle", "無法檢查更新"),
                LanguageManager.GetText("UpdateCheckFailedBody", "請確認網路連線後再試一次。"), ex.Message);
        }
        await dlg.ShowDialog(owner);
    }
}
