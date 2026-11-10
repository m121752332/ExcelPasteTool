using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace ExcelPasteTool;

/// <summary>
/// 軟體更新對話框：顯示新版資訊 → 下載（進度條）→ 解壓 → 重新啟動。
/// 也用來顯示「已是最新版本」、「更新完成」等訊息。
/// </summary>
public partial class UpdateDialog : Window
{
    private enum DialogState
    {
        Available,   // 有新版，等待使用者按「立即更新」
        Working,     // 下載 / 解壓中
        Ready,       // 已準備好，等待「重新啟動」
        ManualOnly,  // 沒有適用的 zip，只能開下載頁
        Error,       // 發生錯誤
        Message,     // 單純訊息（已是最新、更新完成…）
    }

    private readonly UpdateInfo? _info;
    private DialogState _state;
    private CancellationTokenSource? _cts;
    private string? _preparedRoot;

    private readonly TextBlock _iconText;
    private readonly TextBlock _titleText;
    private readonly TextBlock _subtitleText;
    private readonly Border _notesPanel;
    private readonly SelectableTextBlock _notesText;
    private readonly StackPanel _progressPanel;
    private readonly ProgressBar _progressBar;
    private readonly TextBlock _statusText;
    private readonly TextBlock _percentText;
    private readonly Button _primaryButton;
    private readonly Button _secondaryButton;

    // 設計工具需要無參數建構子
    public UpdateDialog() : this(null) { }

    private UpdateDialog(UpdateInfo? info)
    {
        AvaloniaXamlLoader.Load(this);
        _info = info;

        _iconText = this.FindControl<TextBlock>("IconText")!;
        _titleText = this.FindControl<TextBlock>("TitleText")!;
        _subtitleText = this.FindControl<TextBlock>("SubtitleText")!;
        _notesPanel = this.FindControl<Border>("NotesPanel")!;
        _notesText = this.FindControl<SelectableTextBlock>("NotesText")!;
        _progressPanel = this.FindControl<StackPanel>("ProgressPanel")!;
        _progressBar = this.FindControl<ProgressBar>("DownloadProgressBar")!;
        _statusText = this.FindControl<TextBlock>("StatusText")!;
        _percentText = this.FindControl<TextBlock>("PercentText")!;
        _primaryButton = this.FindControl<Button>("PrimaryButton")!;
        _secondaryButton = this.FindControl<Button>("SecondaryButton")!;

        Closing += OnClosing;

        if (info != null)
            ShowAvailable(info);
    }

    // ------------------------------------------------------------------
    // 建立對話框
    // ------------------------------------------------------------------

    /// <summary>有新版本時的對話框。</summary>
    public static UpdateDialog ForUpdate(UpdateInfo info) => new(info);

    /// <summary>單純訊息對話框（已是最新版本 / 更新完成 / 錯誤）。</summary>
    public static UpdateDialog ForMessage(string icon, string title, string subtitle, string? body = null)
    {
        var dlg = new UpdateDialog(null);
        dlg.SetState(DialogState.Message);
        dlg._iconText.Text = icon;
        dlg._titleText.Text = title;
        dlg._subtitleText.Text = subtitle;
        dlg._notesPanel.IsVisible = !string.IsNullOrWhiteSpace(body);
        dlg._notesText.Text = body ?? string.Empty;
        return dlg;
    }

    // ------------------------------------------------------------------
    // 狀態切換
    // ------------------------------------------------------------------

    private void ShowAvailable(UpdateInfo info)
    {
        _iconText.Text = "⬆";
        _titleText.Text = $"發現新版本 v{info.Version.ToString(3)}";
        var published = info.PublishedAt?.ToLocalTime().ToString("yyyy/MM/dd") ?? string.Empty;
        _subtitleText.Text = $"目前版本 v{UpdateService.CurrentVersionText}" +
                             (published.Length > 0 ? $"　·　發佈於 {published}" : string.Empty) +
                             (info.AssetSize > 0 ? $"　·　{FormatSize(info.AssetSize)}" : string.Empty);
        _notesText.Text = string.IsNullOrWhiteSpace(info.ReleaseNotes) ? "（此版本沒有提供更新說明）" : info.ReleaseNotes.Trim();
        _notesPanel.IsVisible = true;

        if (info.CanAutoUpdate)
        {
            SetState(DialogState.Available);
        }
        else
        {
            SetState(DialogState.ManualOnly);
            _progressPanel.IsVisible = true;
            _progressBar.IsVisible = false;
            _statusText.Text = "這個版本沒有提供適用於目前系統的 .zip 更新檔，請到下載頁手動下載。";
        }
    }

    private void SetState(DialogState state)
    {
        _state = state;
        _secondaryButton.IsVisible = true;
        _secondaryButton.IsEnabled = true;
        _primaryButton.IsEnabled = true;

        switch (state)
        {
            case DialogState.Available:
                _primaryButton.Content = "立即更新";
                _secondaryButton.Content = "稍後再說";
                break;
            case DialogState.Working:
                _primaryButton.Content = "更新中…";
                _primaryButton.IsEnabled = false;
                _secondaryButton.Content = "取消";
                break;
            case DialogState.Ready:
                _primaryButton.Content = "重新啟動";
                _secondaryButton.Content = "稍後";
                break;
            case DialogState.ManualOnly:
                _primaryButton.Content = "前往下載頁";
                _secondaryButton.Content = "關閉";
                break;
            case DialogState.Error:
                _primaryButton.Content = _info?.CanAutoUpdate == true ? "重試" : "前往下載頁";
                _secondaryButton.Content = "關閉";
                break;
            case DialogState.Message:
                _primaryButton.Content = "確定";
                _secondaryButton.IsVisible = false;
                break;
        }
    }

    // ------------------------------------------------------------------
    // 按鈕
    // ------------------------------------------------------------------

    private async void PrimaryButton_Click(object? sender, RoutedEventArgs e)
    {
        switch (_state)
        {
            case DialogState.Available:
                await RunUpdateAsync();
                break;
            case DialogState.Error when _info?.CanAutoUpdate == true:
                await RunUpdateAsync();
                break;
            case DialogState.Ready:
                Restart();
                break;
            case DialogState.ManualOnly:
            case DialogState.Error:
                UpdateService.OpenUrl(_info?.ReleasePageUrl ?? UpdateService.ReleasesPageUrl);
                Close();
                break;
            default:
                Close();
                break;
        }
    }

    private void SecondaryButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_state == DialogState.Working)
        {
            _cts?.Cancel();
            return;
        }
        Close();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // 下載中關閉視窗 = 取消下載
        _cts?.Cancel();
    }

    // ------------------------------------------------------------------
    // 更新流程
    // ------------------------------------------------------------------

    private async Task RunUpdateAsync()
    {
        if (_info == null) return;

        SetState(DialogState.Working);
        _iconText.Text = "⬇";
        _progressPanel.IsVisible = true;
        _progressBar.IsVisible = true;
        _progressBar.IsIndeterminate = false;
        _progressBar.Value = 0;
        _percentText.Text = string.Empty;
        _statusText.Text = "正在連線下載伺服器…";

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            // 1. 下載
            var progress = new Progress<DownloadProgress>(p =>
            {
                _progressBar.Value = p.Percent;
                _percentText.Text = $"{p.Percent:0}%";
                var speed = p.BytesPerSecond > 0 ? $"　{FormatSize((long)p.BytesPerSecond)}/s" : string.Empty;
                _statusText.Text = $"下載中 {FormatSize(p.BytesReceived)} / {FormatSize(p.TotalBytes)}{speed}";
            });
            var zipPath = await UpdateService.DownloadAsync(_info, progress, ct);

            // 2. 解壓與驗證（在背景執行緒，避免畫面卡住）
            _progressBar.IsIndeterminate = true;
            _percentText.Text = string.Empty;
            _statusText.Text = "正在解壓縮並驗證更新檔…";
            _preparedRoot = await Task.Run(() => UpdateService.PrepareUpdate(zipPath), ct);

            // 3. 完成，等待重新啟動
            _progressBar.IsIndeterminate = false;
            _progressBar.Value = 100;
            _percentText.Text = "100%";
            _iconText.Text = "✓";
            _titleText.Text = "更新已準備完成";
            _statusText.Text = $"v{_info.Version.ToString(3)} 已下載完成，按「重新啟動」即可完成安裝。";
            SetState(DialogState.Ready);
        }
        catch (OperationCanceledException)
        {
            _progressPanel.IsVisible = false;
            _iconText.Text = "⬆";
            SetState(DialogState.Available);
        }
        catch (Exception ex)
        {
            _progressBar.IsIndeterminate = false;
            _iconText.Text = "!";
            _statusText.Text = $"更新失敗：{ex.Message}";
            _percentText.Text = string.Empty;
            SetState(DialogState.Error);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void Restart()
    {
        if (_preparedRoot == null) return;
        try
        {
            ConfigServices.Save();
            UpdateService.LaunchUpdater(_preparedRoot);
        }
        catch (Exception ex)
        {
            _iconText.Text = "!";
            _statusText.Text = $"無法啟動更新程式：{ex.Message}";
            SetState(DialogState.Error);
            return;
        }

        _primaryButton.IsEnabled = false;
        _secondaryButton.IsEnabled = false;
        _statusText.Text = "正在關閉程式並套用更新…";

        Dispatcher.UIThread.Post(() =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
            else
                Environment.Exit(0);
        });
    }

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "0 KB";
        double mb = bytes / 1024d / 1024d;
        return mb >= 1 ? $"{mb:0.0} MB" : $"{bytes / 1024d:0} KB";
    }
}
