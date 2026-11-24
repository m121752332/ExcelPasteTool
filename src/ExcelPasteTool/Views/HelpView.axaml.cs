using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Diagnostics;

namespace ExcelPasteTool;

/// <summary>
/// 說明頁與 GitHub 專案頁共用的檢視：用系統內建的瀏覽器引擎
/// （Windows：WebView2、macOS：WKWebView、Linux：WebKitGTK）顯示內容。
///   - <see cref="HelpSource.Local"/>：側邊欄「說明」，顯示 HelpService 產生的 App 功能說明（本機 HTML）。
///   - <see cref="HelpSource.GitHub"/>：側邊欄「GitHub 專案頁」，載入 GitHub 上的 README，
///     專案底下的連結留在 App 內，其他網站一律交給預設瀏覽器。
/// 這台電腦沒有可用引擎（或建立失敗）時，改用預設瀏覽器開啟並顯示提示。
/// </summary>
public partial class HelpView : UserControl
{
    private readonly HelpSource _source;
    private NativeWebView? _webView;
    private bool _embeddedFailed;
    // 內容是否需要重新載入（語言、主題、字型變更後）
    private bool _contentStale = true;
    private bool _fallbackOpened;
    // WebView 目前顯示的網址（「用瀏覽器開啟」會開同一頁）
    private Uri? _currentUri;

    public HelpView() : this(HelpSource.Local) { }

    public HelpView(HelpSource source)
    {
        _source = source;
        InitializeComponent();
        OpenInBrowserButton.Click += (_, _) => OpenCurrentInBrowser();
        BackButton.Click += (_, _) =>
        {
            if (_webView?.CanGoBack == true) _webView.GoBack();
        };

        // 頁面有快取，離開畫面時仍要知道內容已過期，所以這些事件不解除訂閱
        LanguageManager.LanguageChanged += OnAppearanceChanged;
        // GitHub 專案頁的配色由 GitHub 決定，只有本機說明需要跟著主題與字型重新產生
        if (source == HelpSource.Local)
        {
            ThemeManager.ThemeChanged += OnAppearanceChanged;
            FontManager.FontChanged += OnAppearanceChanged;
        }
    }

    /// <summary>這一頁顯示的內容來源（UI 測試用）。</summary>
    internal HelpSource Source => _source;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateTexts();
        ShowContent();
    }

    private void OnAppearanceChanged()
    {
        _contentStale = true;
        Dispatcher.UIThread.Post(() =>
        {
            UpdateTexts();
            if (this.IsAttachedToVisualTree())
                ShowContent();
        });
    }

    private void UpdateTexts()
    {
        TitleText.Text = _source == HelpSource.GitHub
            ? "GitHub"
            : LanguageManager.GetText("HelpTitle", "說明");
        ToolTip.SetTip(BackButton, LanguageManager.GetText("HelpBackTooltip", "上一頁"));
        OpenInBrowserButton.Content = LanguageManager.GetText("HelpOpenInBrowser", "用瀏覽器開啟");
        FallbackText.Text = LanguageManager.GetText("HelpUnavailableText",
            "這台電腦沒有可用的內嵌瀏覽器元件，已改用預設瀏覽器開啟說明。\n之後也可以按右上角的「用瀏覽器開啟」。");
    }

    /// <summary>這一頁的起始網址：本機說明檔或 GitHub 專案頁（依介面語言選 README）。</summary>
    private Uri StartUri => _source == HelpSource.GitHub
        ? new Uri(HelpService.GitHubHelpUrl)
        : new Uri(HelpService.WriteHelpFile());

    private void ShowContent()
    {
        if (!_embeddedFailed && _webView == null && !TryCreateWebView())
            _embeddedFailed = true;

        if (_embeddedFailed || _webView == null)
        {
            ShowFallback();
            return;
        }

        if (!_contentStale) return;
        try
        {
            _currentUri = StartUri;
            _webView.Navigate(_currentUri);
            _contentStale = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Help] 載入 {_source} 失敗：{ex.Message}");
            DisableEmbedded();
        }
    }

    private bool TryCreateWebView()
    {
        if (!HelpService.IsEmbeddedViewerAvailable()) return false;
        try
        {
            var webView = new NativeWebView();
            webView.EnvironmentRequested += (_, args) =>
            {
                if (args is WindowsWebView2EnvironmentRequestedEventArgs webView2)
                    webView2.UserDataFolder = HelpService.WebViewUserDataFolder;
            };
            webView.NavigationStarted += OnNavigationStarted;
            webView.NavigationCompleted += (_, _) => UpdateBackButton();
            webView.NewWindowRequested += (_, args) =>
            {
                // 另開視窗的連結：GitHub 專案底下的留在這一頁，其他交給預設瀏覽器
                args.Handled = true;
                if (args.Request == null) return;
                if (_source == HelpSource.GitHub && HelpService.IsGitHubHelpUri(args.Request))
                {
                    _currentUri = args.Request;
                    webView.Navigate(args.Request);
                }
                else
                {
                    HelpService.OpenExternal(args.Request.ToString());
                }
            };
            _webView = webView;
            WebViewHost.Child = webView;
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Help] 無法建立 WebView：{ex.Message}");
            _webView = null;
            WebViewHost.Child = null;
            return false;
        }
    }

    private void OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs args)
    {
        var uri = args.Request;
        if (uri == null) return;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            _currentUri = uri;
            return;
        }
        // GitHub 專案頁（含 README、Releases、Issues）在 App 內瀏覽；其他網站交給預設瀏覽器
        if (_source == HelpSource.GitHub && HelpService.IsGitHubHelpUri(uri))
        {
            _currentUri = uri;
            return;
        }
        args.Cancel = true;
        HelpService.OpenExternal(uri.ToString());
    }

    private void UpdateBackButton()
    {
        BackButton.IsVisible = _source == HelpSource.GitHub && _webView?.CanGoBack == true;
    }

    private void OpenCurrentInBrowser()
    {
        if (_source == HelpSource.GitHub)
            HelpService.OpenExternal(_currentUri is { } uri && HelpService.IsGitHubHelpUri(uri) ? uri.ToString() : HelpService.GitHubHelpUrl);
        else
            HelpService.OpenInBrowser();
    }

    private void DisableEmbedded()
    {
        _embeddedFailed = true;
        _webView = null;
        WebViewHost.Child = null;
        ShowFallback();
    }

    private void ShowFallback()
    {
        WebViewHost.IsVisible = false;
        BackButton.IsVisible = false;
        FallbackPanel.IsVisible = true;
        // 只自動開一次，避免每次切回這一頁都跳出瀏覽器
        if (_fallbackOpened) return;
        _fallbackOpened = true;
        if (_source == HelpSource.GitHub) HelpService.OpenExternal(HelpService.GitHubHelpUrl);
        else HelpService.OpenInBrowser();
    }
}