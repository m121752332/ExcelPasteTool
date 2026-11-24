using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;

namespace ExcelPasteTool.Helpers
{
    /// <summary>Toast 的種類：決定左側色條與圖示的顏色（取自各主題的 SuccessBrush / WarningBrush / DangerBrush / AccentTextBrush）。</summary>
    public enum ToastSeverity
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>應用程式內通知出現的位置（設定 › 一般設置 › 通知位置）。</summary>
    public enum ToastPosition
    {
        BottomRight,
        TopRight,
        BottomLeft
    }

    /// <summary>
    /// 全 App 共用的通知（參照 UniGetUI 的應用程式內通知）：主視窗右下角由下往上堆疊的卡片，
    /// 左側有種類色條與圖示，從右側滑入、淡出；滑鼠停在卡片上時暫停倒數，點 × 立即關閉。
    /// 卡片放在主視窗的 ToastHost（最上層，高於導覽按鈕與導覽遮罩）。
    /// </summary>
    public class ToastQueueHelper
    {
        /// <summary>「同時顯示的通知數」可選範圍與預設值。</summary>
        public const int MinVisibleLimit = 1, MaxVisibleLimit = 5, DefaultMaxVisible = 3;

        /// <summary>同時最多顯示幾張（設定 › 一般設置），超過時最舊的先關閉。</summary>
        public static int MaxVisible
        {
            get
            {
                var value = ConfigServices.Config.ToastMaxVisible;
                return value is >= MinVisibleLimit and <= MaxVisibleLimit ? value : DefaultMaxVisible;
            }
        }

        /// <summary>設定變更後，畫面上超過上限的舊通知立即關閉。</summary>
        public static void TrimToLimit(Panel host)
        {
            var open = host.Children.OfType<ToastCard>().Where(c => !c.IsClosing).ToList();
            // 從上方出現時最新的在最上面（索引 0），從下方出現時最新的在最下面
            var oldestFirst = CurrentPosition == ToastPosition.TopRight ? Enumerable.Reverse(open).ToList() : open;
            foreach (var old in oldestFirst.Take(Math.Max(0, oldestFirst.Count - MaxVisible)))
                old.Close();
        }

        /// <summary>每張卡片停留的時間。</summary>
        public static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(3);

        public const string HostName = "ToastHost";

        /// <summary>距離內容區邊緣的間距。</summary>
        private const double EdgeMargin = 20;

        /// <summary>從右上出現時距離內容區頂端的距離（避開工具頁的工具列）。</summary>
        private const double TopOffset = 80;

        /// <summary>目前設定的位置（讀設定檔，無法辨識時用右下）。</summary>
        public static ToastPosition CurrentPosition =>
            Enum.TryParse<ToastPosition>(ConfigServices.Config.ToastPosition, out var p) ? p : ToastPosition.BottomRight;

        public static IReadOnlyList<ToastPosition> AllPositions { get; } =
            new[] { ToastPosition.BottomRight, ToastPosition.TopRight, ToastPosition.BottomLeft };

        public static string GetPositionName(ToastPosition position) => position switch
        {
            ToastPosition.TopRight => LanguageManager.GetText("ToastPositionTopRight", "從右上出現"),
            ToastPosition.BottomLeft => LanguageManager.GetText("ToastPositionBottomLeft", "從左下出現"),
            _ => LanguageManager.GetText("ToastPositionBottomRight", "從右下出現")
        };

        /// <summary>把通知容器對齊到指定位置。容器放在主視窗的內容區（側邊欄右邊、標題列下方），所以不會蓋到側邊欄。</summary>
        public static void ApplyPosition(Panel host, ToastPosition position)
        {
            var right = position != ToastPosition.BottomLeft;
            var top = position == ToastPosition.TopRight;
            host.HorizontalAlignment = right ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            host.VerticalAlignment = top ? VerticalAlignment.Top : VerticalAlignment.Bottom;
            // 從上方出現時要讓開工具頁頂端的工具列與「導覽」按鈕，不要疊在上面
            host.Margin = top ? new Thickness(EdgeMargin, TopOffset, EdgeMargin, EdgeMargin) : new Thickness(EdgeMargin);
            // 已顯示的卡片換位置後要重新排列：靠近邊緣的是最新的一張
            var cards = host.Children.OfType<ToastCard>().ToList();
            host.Children.Clear();
            foreach (var card in top ? Enumerable.Reverse(cards) : cards)
                host.Children.Add(card);
        }

        private readonly Control _owner;

        public ToastQueueHelper(Control owner)
        {
            _owner = owner;
        }

        /// <summary>顯示一般的完成訊息（成功）。設定頁的「已儲存」等訊息都走這裡。</summary>
        public void EnqueueToast(string message) => EnqueueToast(message, ToastSeverity.Success);

        public void EnqueueToast(string message, ToastSeverity severity)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (Dispatcher.UIThread.CheckAccess()) Show(message, severity);
            else Dispatcher.UIThread.Post(() => Show(message, severity));
        }

        private Panel? FindHost()
        {
            var window = TopLevel.GetTopLevel(_owner) as Window
                ?? (Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            return window?.FindControl<Panel>(HostName);
        }

        private void Show(string message, ToastSeverity severity)
        {
            var host = FindHost();
            if (host == null) return;

            // 同一則訊息還在畫面上時只重新計時，不重複疊（例如連按複製）
            var open = host.Children.OfType<ToastCard>().Where(c => !c.IsClosing).ToList();
            var same = open.FirstOrDefault(c => c.Message == message && c.Severity == severity);
            if (same != null)
            {
                same.RestartTimer();
                return;
            }

            var position = CurrentPosition;
            var card = new ToastCard(message, severity, fromTop: position == ToastPosition.TopRight);
            // 最新的一張放在最靠近邊緣的位置：上方出現時插在最上面，下方出現時加在最下面
            if (position == ToastPosition.TopRight) host.Children.Insert(0, card);
            else host.Children.Add(card);
            TrimToLimit(host);
        }
    }

    /// <summary>一張通知卡片。樣式（顏色、圓角、陰影）寫在 AppStyles.axaml 的 Border.toast。</summary>
    public sealed class ToastCard : Border
    {
        private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(200);
        private readonly DispatcherTimer _timer;

        public string Message { get; }
        public ToastSeverity Severity { get; }
        public bool IsClosing { get; private set; }

        private readonly string _offscreen;

        // 繼承 Border 的控制項預設用自己的型別比對樣式，要指定回 Border，AppStyles 的 Border.toast 才會套用
        protected override Type StyleKeyOverride => typeof(Border);

        public ToastCard(string message, ToastSeverity severity, bool fromTop = false)
        {
            // 參照 Win11：靠下方時由下往上浮出，靠上方時由上往下
            _offscreen = fromTop ? "translateY(-12px)" : "translateY(12px)";
            Message = message;
            Severity = severity;
            Classes.Add("toast");
            Classes.Add(severity.ToString().ToLowerInvariant());

            var icon = new Path
            {
                Data = StreamGeometry.Parse(severity switch
                {
                    ToastSeverity.Success => IconManager.SuccessIcon,
                    ToastSeverity.Warning => IconManager.WarningIcon,
                    ToastSeverity.Error => IconManager.ErrorIcon,
                    _ => IconManager.InfoOutlineIcon
                }),
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(14, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            icon.Classes.Add("toast-icon");

            var text = new TextBlock { Text = message, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 14) };
            text.Classes.Add("toast-text");

            var close = new Button
            {
                Content = new Path { Data = StreamGeometry.Parse(IconManager.CloseSmallIcon), Width = 10, Height = 10, Stretch = Stretch.Uniform },
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 10, 0),
                Focusable = false
            };
            close.Classes.Add("toast-close");
            ToolTip.SetTip(close, LanguageManager.GetText("ToastCloseTooltip", "關閉"));
            close.Click += (_, _) => Close();

            var strip = new Border();
            strip.Classes.Add("toast-strip");

            // 依種類淡淡染一層底色（同 Win11 InfoBar），透明度寫在樣式
            var tint = new Border { IsHitTestVisible = false };
            tint.Classes.Add("toast-tint");
            Grid.SetColumnSpan(tint, 4);

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("4,Auto,*,Auto"), MinHeight = 48 };
            grid.Children.Add(tint);
            Grid.SetColumn(strip, 0);
            Grid.SetColumn(icon, 1);
            Grid.SetColumn(text, 2);
            Grid.SetColumn(close, 3);
            grid.Children.Add(strip);
            grid.Children.Add(icon);
            grid.Children.Add(text);
            grid.Children.Add(close);
            Child = grid;

            // 進場：從靠近的那一側 40px 滑入並淡入
            Opacity = 0;
            RenderTransform = TransformOperations.Parse(_offscreen);
            Transitions = new Transitions
            {
                new DoubleTransition { Property = OpacityProperty, Duration = AnimationDuration, Easing = new Avalonia.Animation.Easings.CubicEaseOut() },
                new TransformOperationsTransition { Property = RenderTransformProperty, Duration = AnimationDuration, Easing = new Avalonia.Animation.Easings.CubicEaseOut() }
            };

            _timer = new DispatcherTimer { Interval = ToastQueueHelper.DisplayDuration };
            _timer.Tick += (_, _) => Close();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Dispatcher.UIThread.Post(() =>
            {
                Opacity = 1;
                RenderTransform = TransformOperations.Parse("none");
            }, DispatcherPriority.Background);
            _timer.Start();
        }

        // 滑鼠停在卡片上時暫停倒數，方便看完
        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            _timer.Stop();
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            if (!IsClosing) RestartTimer();
        }

        public void RestartTimer()
        {
            _timer.Stop();
            _timer.Start();
        }

        public async void Close()
        {
            if (IsClosing) return;
            IsClosing = true;
            _timer.Stop();
            IsHitTestVisible = false;
            Opacity = 0;
            RenderTransform = TransformOperations.Parse(_offscreen);
            await Task.Delay(AnimationDuration);
            (Parent as Panel)?.Children.Remove(this);
        }
    }
}
