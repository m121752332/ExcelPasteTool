using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace ExcelPasteTool;

/// <summary>
/// 導覽的一個步驟：要高亮的元件（可多個，取聯集範圍）與說明文字。
/// Targets 回傳空集合或找不到可見元件時，說明卡片會置中顯示（例如歡迎頁）。
/// </summary>
public sealed class TourStep
{
    public required string TitleKey { get; init; }
    public required string TitleDefault { get; init; }
    public required string BodyKey { get; init; }
    public required string BodyDefault { get; init; }
    public Func<IEnumerable<Control?>> Targets { get; init; } = () => Array.Empty<Control?>();
}

/// <summary>
/// App 導覽圖層（類似 driver.js）：
/// 整個視窗蓋上半透明遮罩，目前步驟的元件挖空並加上外框，旁邊顯示說明卡片與「上一步／下一步／略過」。
/// 遮罩會攔截所有滑鼠操作，避免導覽途中誤觸底下的按鈕。
/// </summary>
public sealed class TourOverlay : Panel
{
    private const double HolePadding = 6;
    private const double CardWidth = 360;
    private const double CardGap = 14;
    private const double EdgeMargin = 12;

    private readonly TourDimLayer _dim = new();
    private readonly Border _ring;
    private readonly Canvas _canvas = new();
    private readonly Border _card;
    private readonly TextBlock _stepText = new() { Classes = { "tour-step" } };
    private readonly TextBlock _titleText = new() { Classes = { "tour-title" }, TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _bodyText = new() { Classes = { "tour-body" }, TextWrapping = TextWrapping.Wrap };
    private readonly Button _skipButton = new() { Classes = { "tour-link" } };
    private readonly Button _prevButton = new() { Classes = { "ghost" } };
    private readonly Button _nextButton = new() { Classes = { "primary" } };
    private readonly ProgressBar _progress = new() { Classes = { "tour-progress" }, Minimum = 0, Height = 3, MinHeight = 3, MinWidth = 0, Margin = new Thickness(0, 2, 0, 6) };

    private IReadOnlyList<TourStep> _steps = Array.Empty<TourStep>();
    private int _index;
    private bool _firstPlacement;

    /// <summary>導覽結束（完成或略過都會觸發），參數 true 代表走完全部步驟。</summary>
    public event Action<bool>? Closed;

    /// <summary>每次切換步驟前呼叫，讓主視窗有機會切換到該步驟需要的頁面。</summary>
    public event Action<int>? StepChanging;

    public bool IsActive => IsVisible;

    public TourOverlay()
    {
        IsVisible = false;
        // 透明背景讓整個圖層都能接收滑鼠事件，底下的元件在導覽期間不會被點到
        Background = Brushes.Transparent;
        ZIndex = 10000;

        var transitionDuration = TimeSpan.FromMilliseconds(260);
        var easing = new CubicEaseOut();
        _dim.Transitions = new Transitions
        {
            new DoubleTransition { Property = TourDimLayer.HoleLeftProperty, Duration = transitionDuration, Easing = easing },
            new DoubleTransition { Property = TourDimLayer.HoleTopProperty, Duration = transitionDuration, Easing = easing },
            new DoubleTransition { Property = TourDimLayer.HoleWidthProperty, Duration = transitionDuration, Easing = easing },
            new DoubleTransition { Property = TourDimLayer.HoleHeightProperty, Duration = transitionDuration, Easing = easing },
        };

        _ring = new Border
        {
            Classes = { "tour-ring" },
            IsHitTestVisible = false,
            Transitions = new Transitions
            {
                new DoubleTransition { Property = Canvas.LeftProperty, Duration = transitionDuration, Easing = easing },
                new DoubleTransition { Property = Canvas.TopProperty, Duration = transitionDuration, Easing = easing },
                new DoubleTransition { Property = WidthProperty, Duration = transitionDuration, Easing = easing },
                new DoubleTransition { Property = HeightProperty, Duration = transitionDuration, Easing = easing },
                new DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(160) },
            }
        };

        _skipButton.Click += (_, _) => Close(false);
        _prevButton.Click += (_, _) => Previous();
        _nextButton.Click += (_, _) => Next();

        var buttons = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"), Margin = new Thickness(0, 16, 0, 0) };
        Grid.SetColumn(_skipButton, 0);
        Grid.SetColumn(_prevButton, 2);
        Grid.SetColumn(_nextButton, 3);
        _prevButton.Margin = new Thickness(0, 0, 8, 0);
        buttons.Children.AddRange(new Control[] { _skipButton, _prevButton, _nextButton });

        _card = new Border
        {
            Classes = { "tour-card" },
            Width = CardWidth,
            Child = new StackPanel
            {
                Spacing = 6,
                Children = { _stepText, _progress, _titleText, _bodyText, buttons }
            },
            Transitions = new Transitions
            {
                new DoubleTransition { Property = Canvas.LeftProperty, Duration = transitionDuration, Easing = easing },
                new DoubleTransition { Property = Canvas.TopProperty, Duration = transitionDuration, Easing = easing },
                new DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(180) },
            }
        };

        _canvas.Children.Add(_ring);
        _canvas.Children.Add(_card);
        Children.Add(_dim);
        Children.Add(_canvas);

        SizeChanged += (_, _) => { if (IsVisible) Place(animate: false); };
        LanguageManager.LanguageChanged += () => Dispatcher.UIThread.Post(() => { if (IsVisible) Render(); });
    }

    public void Start(IReadOnlyList<TourStep> steps)
    {
        if (steps.Count == 0) return;
        _steps = steps;
        _index = 0;
        _firstPlacement = true;
        IsVisible = true;
        GoTo(0);
    }

    public void Next()
    {
        if (!IsVisible) return;
        if (_index >= _steps.Count - 1) Close(true);
        else GoTo(_index + 1);
    }

    public void Previous()
    {
        if (IsVisible && _index > 0) GoTo(_index - 1);
    }

    public void Close(bool completed)
    {
        if (!IsVisible) return;
        IsVisible = false;
        Closed?.Invoke(completed);
    }

    private void GoTo(int index)
    {
        _index = index;
        StepChanging?.Invoke(index);
        Render();
        // 等切換頁面與版面配置完成後再計算元件位置
        Dispatcher.UIThread.Post(() => Place(animate: !_firstPlacement), DispatcherPriority.Background);
    }

    private void Render()
    {
        var step = _steps[_index];
        _stepText.Text = string.Format(LanguageManager.GetText("TourStepFormat", "第 {0} / {1} 步"), _index + 1, _steps.Count);
        _titleText.Text = LanguageManager.GetText(step.TitleKey, step.TitleDefault);
        _bodyText.Text = LanguageManager.GetText(step.BodyKey, step.BodyDefault);
        _skipButton.Content = LanguageManager.GetText("TourSkip", "略過導覽");
        _prevButton.Content = LanguageManager.GetText("TourPrev", "上一步");
        var isLast = _index == _steps.Count - 1;
        _nextButton.Content = isLast
            ? LanguageManager.GetText("TourDone", "完成")
            : LanguageManager.GetText("TourNext", "下一步");
        // 快捷鍵只在提示中顯示（與其他按鈕一致）
        ToolTip.SetTip(_nextButton, isLast
            ? LanguageManager.GetText("TourDoneTooltip", "完成 (Space)")
            : LanguageManager.GetText("TourNextTooltip", "下一步 (Space)"));
        _prevButton.IsVisible = _index > 0;
        _skipButton.IsVisible = _index < _steps.Count - 1;

        _progress.Maximum = _steps.Count;
        _progress.Value = _index + 1;
        _nextButton.Focus();
    }

    /// <summary>計算目前步驟元件的位置，移動挖空範圍、外框與說明卡片。</summary>
    private void Place(bool animate)
    {
        if (!IsVisible || _steps.Count == 0) return;
        var size = Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0) return;

        var target = GetTargetRect(_steps[_index]);

        // 第一次顯示時不做位移動畫（避免從左上角滑進來）
        SetTransitionsEnabled(animate);

        if (target is Rect rect)
        {
            rect = rect.Inflate(HolePadding).Intersect(new Rect(size));
            _dim.HoleLeft = rect.X;
            _dim.HoleTop = rect.Y;
            _dim.HoleWidth = rect.Width;
            _dim.HoleHeight = rect.Height;
            Canvas.SetLeft(_ring, rect.X);
            Canvas.SetTop(_ring, rect.Y);
            _ring.Width = rect.Width;
            _ring.Height = rect.Height;
            _ring.Opacity = 1;
        }
        else
        {
            // 沒有目標：挖空縮成畫面中心的一點，遮罩變成全黑幕
            var center = new Point(size.Width / 2, size.Height / 2);
            _dim.HoleLeft = center.X;
            _dim.HoleTop = center.Y;
            _dim.HoleWidth = 0;
            _dim.HoleHeight = 0;
            _ring.Opacity = 0;
        }

        _card.Measure(new Size(CardWidth, double.PositiveInfinity));
        var cardSize = new Size(CardWidth, _card.DesiredSize.Height);
        var pos = target is Rect r ? PlaceCard(r.Inflate(HolePadding), cardSize, size) : new Point((size.Width - cardSize.Width) / 2, (size.Height - cardSize.Height) / 2);
        Canvas.SetLeft(_card, pos.X);
        Canvas.SetTop(_card, pos.Y);

        if (!animate)
            Dispatcher.UIThread.Post(() => SetTransitionsEnabled(true), DispatcherPriority.Background);
        _firstPlacement = false;
    }

    private void SetTransitionsEnabled(bool enabled)
    {
        // Avalonia 沒有暫停 Transitions 的開關，第一次定位時暫時拿掉，定位後再放回
        if (enabled)
        {
            _dim.Transitions = _dimTransitions ?? _dim.Transitions;
            _ring.Transitions = _ringTransitions ?? _ring.Transitions;
            _card.Transitions = _cardTransitions ?? _card.Transitions;
        }
        else
        {
            _dimTransitions ??= _dim.Transitions;
            _ringTransitions ??= _ring.Transitions;
            _cardTransitions ??= _card.Transitions;
            _dim.Transitions = null;
            _ring.Transitions = null;
            _card.Transitions = null;
        }
    }

    private Transitions? _dimTransitions, _ringTransitions, _cardTransitions;

    private Rect? GetTargetRect(TourStep step)
    {
        Rect? union = null;
        foreach (var control in step.Targets())
        {
            if (control == null || !control.IsEffectivelyVisible || control.Bounds.Width <= 0 || control.Bounds.Height <= 0)
                continue;
            var topLeft = control.TranslatePoint(new Point(0, 0), this);
            if (topLeft == null) continue;
            var rect = new Rect(topLeft.Value, control.Bounds.Size);
            union = union is Rect u ? u.Union(rect) : rect;
        }
        return union;
    }

    /// <summary>依序嘗試放在目標的下方、上方、右側、左側，都放不下就疊在目標中間。</summary>
    private static Point PlaceCard(Rect target, Size card, Size area)
    {
        double ClampX(double x) => Math.Clamp(x, EdgeMargin, Math.Max(EdgeMargin, area.Width - card.Width - EdgeMargin));
        double ClampY(double y) => Math.Clamp(y, EdgeMargin, Math.Max(EdgeMargin, area.Height - card.Height - EdgeMargin));

        var alignedX = ClampX(target.X);
        var alignedY = ClampY(target.Y);

        if (target.Bottom + CardGap + card.Height <= area.Height - EdgeMargin)
            return new Point(alignedX, target.Bottom + CardGap);
        if (target.Y - CardGap - card.Height >= EdgeMargin)
            return new Point(alignedX, target.Y - CardGap - card.Height);
        if (target.Right + CardGap + card.Width <= area.Width - EdgeMargin)
            return new Point(target.Right + CardGap, alignedY);
        if (target.X - CardGap - card.Width >= EdgeMargin)
            return new Point(target.X - CardGap - card.Width, alignedY);
        return new Point(ClampX(target.Center.X - card.Width / 2), ClampY(target.Center.Y - card.Height / 2));
    }
}

/// <summary>半透明遮罩，中間挖出一個圓角矩形；挖空位置用 StyledProperty 才能套用 Transitions 做平滑移動。</summary>
public sealed class TourDimLayer : Control
{
    public static readonly StyledProperty<double> HoleLeftProperty = AvaloniaProperty.Register<TourDimLayer, double>(nameof(HoleLeft));
    public static readonly StyledProperty<double> HoleTopProperty = AvaloniaProperty.Register<TourDimLayer, double>(nameof(HoleTop));
    public static readonly StyledProperty<double> HoleWidthProperty = AvaloniaProperty.Register<TourDimLayer, double>(nameof(HoleWidth));
    public static readonly StyledProperty<double> HoleHeightProperty = AvaloniaProperty.Register<TourDimLayer, double>(nameof(HoleHeight));

    private static readonly IBrush DimBrush = new SolidColorBrush(Color.FromArgb(0x8C, 0, 0, 0));
    private const double Radius = 8;

    static TourDimLayer()
    {
        AffectsRender<TourDimLayer>(HoleLeftProperty, HoleTopProperty, HoleWidthProperty, HoleHeightProperty);
    }

    public TourDimLayer()
    {
        IsHitTestVisible = false;
    }

    public double HoleLeft { get => GetValue(HoleLeftProperty); set => SetValue(HoleLeftProperty, value); }
    public double HoleTop { get => GetValue(HoleTopProperty); set => SetValue(HoleTopProperty, value); }
    public double HoleWidth { get => GetValue(HoleWidthProperty); set => SetValue(HoleWidthProperty, value); }
    public double HoleHeight { get => GetValue(HoleHeightProperty); set => SetValue(HoleHeightProperty, value); }

    public override void Render(DrawingContext context)
    {
        var full = new Rect(Bounds.Size);
        var w = Math.Max(0, HoleWidth);
        var h = Math.Max(0, HoleHeight);
        if (w < 1 || h < 1)
        {
            context.FillRectangle(DimBrush, full);
            return;
        }

        var hole = new Rect(HoleLeft, HoleTop, w, h);
        var r = Math.Min(Radius, Math.Min(w, h) / 2);
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.SetFillRule(FillRule.EvenOdd);
            ctx.BeginFigure(full.TopLeft, true);
            ctx.LineTo(full.TopRight);
            ctx.LineTo(full.BottomRight);
            ctx.LineTo(full.BottomLeft);
            ctx.EndFigure(true);

            var arc = new Size(r, r);
            ctx.BeginFigure(new Point(hole.Left + r, hole.Top), true);
            ctx.LineTo(new Point(hole.Right - r, hole.Top));
            ctx.ArcTo(new Point(hole.Right, hole.Top + r), arc, 0, false, SweepDirection.Clockwise);
            ctx.LineTo(new Point(hole.Right, hole.Bottom - r));
            ctx.ArcTo(new Point(hole.Right - r, hole.Bottom), arc, 0, false, SweepDirection.Clockwise);
            ctx.LineTo(new Point(hole.Left + r, hole.Bottom));
            ctx.ArcTo(new Point(hole.Left, hole.Bottom - r), arc, 0, false, SweepDirection.Clockwise);
            ctx.LineTo(new Point(hole.Left, hole.Top + r));
            ctx.ArcTo(new Point(hole.Left + r, hole.Top), arc, 0, false, SweepDirection.Clockwise);
            ctx.EndFigure(true);
        }
        context.DrawGeometry(DimBrush, null, geometry);
    }
}
