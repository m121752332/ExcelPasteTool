using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Diagnostics;

namespace ExcelPasteTool;

/// <summary>
/// 說明頁：用系統內建的瀏覽器引擎顯示 HelpService 產生的 HTML。
/// 這台電腦沒有可用引擎（或建立失敗）時，改用預設瀏覽器開啟並顯示提示。
/// </summary>
public partial class HelpView : UserControl
{
    private NativeWebView? _webView;
    private bool _embeddedFailed;
    // 內容是否需要重新產生（語言、主題、字型變更後）
    private bool _contentStale = true;
    private bool _fallbackOpened;

    public HelpView()
    {
        InitializeComponent();
        OpenInBrowserButton.Click += (_, _) => HelpService.OpenInBrowser();

        // 頁面有快取，離開畫面時仍要知道內容已過期，所以這些事件不解除訂閱
        LanguageManager.LanguageChanged += OnAppearanceChanged;
        ThemeManager.ThemeChanged += OnAppearanceChanged;
        FontManager.FontChanged += OnAppearanceChanged;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateTexts();
        ShowHelp();
    }

    private void OnAppearanceChanged()
    {
        _contentStale = true;
        Dispatcher.UIThread.Post(() =>
        {
            UpdateTexts();
            if (this.IsAttachedToVisualTree())
                ShowHelp();
        });
    }

    private void UpdateTexts()
    {
        TitleText.Text = LanguageManager.GetText("HelpTitle", "說明");
        OpenInBrowserButton.Content = LanguageManager.GetText("HelpOpenInBrowser", "用瀏覽器開啟");
        FallbackText.Text = LanguageManager.GetText("HelpUnavailableText",
            "這台電腦沒有可用的內嵌瀏覽器元件，已改用預設瀏覽器開啟說明。\n之後也可以按右上角的「用瀏覽器開啟」。");
    }

    private void ShowHelp()
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
            _webView.Navigate(new Uri(HelpService.WriteHelpFile()));
            _contentStale = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Help] 載入說明失敗：{ex.Message}");
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
            webView.NewWindowRequested += (_, args) =>
            {
                // 說明裡的外部連結一律交給預設瀏覽器
                args.Handled = true;
                if (args.Request != null) HelpService.OpenExternal(args.Request.ToString());
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
        if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        {
            args.Cancel = true;
            HelpService.OpenExternal(uri.ToString());
        }
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
        FallbackPanel.IsVisible = true;
        // 只自動開一次，避免每次切回說明頁都跳出瀏覽器
        if (_fallbackOpened) return;
        _fallbackOpened = true;
        HelpService.OpenInBrowser();
    }
}
