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
        /// <summary>同時最多顯示幾張，超過時最舊的先關閉。</summary>
        public const int MaxVisible = 3;

        /// <summary>每張卡片停留的時間。</summary>
        public static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(3);

        public const string HostName = "ToastHost";

        /// <summary>距離內容區邊緣的間距。</summary>
        private const double EdgeMargin = 20;

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
            host.Margin = new Thickness(EdgeMargin);
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
            var card = new ToastCard(message, severity, fromLeft: position == ToastPosition.BottomLeft);
            // 最新的一張放在最靠近邊緣的位置：上方出現時插在最上面，下方出現時加在最下面
            if (position == ToastPosition.TopRight) host.Children.Insert(0, card);
            else host.Children.Add(card);
            foreach (var old in open.Take(Math.Max(0, open.Count + 1 - MaxVisible)))
                old.Close();
        }
    }

    /// <summary>一張通知卡片。樣式（顏色、圓角、陰影）寫在 AppStyles.axaml 的 Border.toast。</summary>
    public sealed class ToastCard : Border
    {
        private static readonly TimeSpan AnimationDuration = TimeSpan.FromMilliseconds(220);
        private readonly DispatcherTimer _timer;

        public string Message { get; }
        public ToastSeverity Severity { get; }
        public bool IsClosing { get; private set; }

        private readonly string _offscreen;

        public ToastCard(string message, ToastSeverity severity, bool fromLeft = false)
        {
            _offscreen = fromLeft ? "translateX(-40px)" : "translateX(40px)";
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
                Margin = new Thickness(12, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            icon.Classes.Add("toast-icon");

            var text = new TextBlock { Text = message, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 11) };
            text.Classes.Add("toast-text");

            var close = new Button
            {
                Content = new Path { Data = StreamGeometry.Parse(IconManager.CloseSmallIcon), Width = 10, Height = 10, Stretch = Stretch.Uniform },
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 6, 0),
                Focusable = false
            };
            close.Classes.Add("toast-close");
            ToolTip.SetTip(close, LanguageManager.GetText("ToastCloseTooltip", "關閉"));
            close.Click += (_, _) => Close();

            var strip = new Border();
            strip.Classes.Add("toast-strip");

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("4,Auto,*,Auto") };
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
                new DoubleTransition { Property = OpacityProperty, Duration = AnimationDuration },
                new TransformOperationsTransition { Property = RenderTransformProperty, Duration = AnimationDuration }
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
