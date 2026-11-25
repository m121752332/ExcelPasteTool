using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;

namespace ExcelPasteTool.Helpers;

/// <summary>
/// 設定頁可展開列 / 群組卡片的展開與收合動畫（同 Win11 設定）：
/// <list type="bullet">
/// <item>內容區高度從 0 平滑長到實際高度（收合反過來），同時淡入淡出。</item>
/// <item>箭頭跟著旋轉 180 度，跟高度用同一個時間與緩動，看起來是同一個動作。</item>
/// <item>標題列的圓角（expanded 樣式）在展開一開始就改成上圓下直；收合時等動畫結束才改回四角圓，避免底部先露出一截直角。</item>
/// </list>
/// 動畫結束後把 MaxHeight 放回無限大，之後內容變高（例如字體變大）也不會被裁掉。
/// </summary>
public sealed class ExpanderAnimator
{
    public static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(220);
    private static readonly Easing Ease = new CubicEaseOut();

    /// <summary>測試用：關掉動畫時直接套用最終狀態，畫面截圖與斷言不用等時間。</summary>
    internal static bool AnimationsEnabled { get; set; } = true;

    private readonly Button _header;
    private readonly Border _body;
    private readonly Visual _chevron;
    private int _version;

    public ExpanderAnimator(Button header, Border body, Visual chevron, bool expanded)
    {
        _header = header;
        _body = body;
        _chevron = chevron;
        SetFinalState(expanded);
    }

    /// <summary>目前（或動畫結束後）是否為展開狀態。</summary>
    public bool IsExpanded { get; private set; }

    public void SetExpanded(bool expanded)
    {
        if (expanded == IsExpanded) return;
        if (!AnimationsEnabled || TopLevel.GetTopLevel(_header) == null)
        {
            SetFinalState(expanded);
            return;
        }

        IsExpanded = expanded;
        var version = ++_version;

        // 起點：目前的實際高度（動畫進行中再點一次，就從當下的高度接著走）
        var from = _body.IsVisible ? _body.Bounds.Height : 0;
        double to;
        if (expanded)
        {
            _header.Classes.Add("expanded");
            _body.IsVisible = true;
            to = MeasureContentHeight();
        }
        else
        {
            to = 0;
        }

        SetTransitions(enabled: false);
        _body.MaxHeight = from;
        _body.Opacity = expanded ? Math.Min(_body.Opacity, 0) : 1;
        SetTransitions(enabled: true);

        _body.MaxHeight = to;
        _body.Opacity = expanded ? 1 : 0;
        _chevron.RenderTransform = Rotation(expanded);

        DispatcherTimer.RunOnce(() =>
        {
            if (version != _version) return; // 動畫中又被切換，交給新的那次收尾
            SetFinalState(expanded);
        }, Duration + TimeSpan.FromMilliseconds(30));
    }

    private void SetFinalState(bool expanded)
    {
        IsExpanded = expanded;
        SetTransitions(enabled: false);
        _header.Classes.Set("expanded", expanded);
        _body.IsVisible = expanded;
        _body.MaxHeight = double.PositiveInfinity;
        _body.Opacity = 1;
        _chevron.RenderTransform = Rotation(expanded);
        _chevron.RenderTransformOrigin = RelativePoint.Center;
        SetTransitions(enabled: true);
    }

    private double MeasureContentHeight()
    {
        var width = _header.Bounds.Width > 0 ? _header.Bounds.Width : double.PositiveInfinity;
        var saved = _body.MaxHeight;
        _body.MaxHeight = double.PositiveInfinity;
        _body.Measure(new Size(width, double.PositiveInfinity));
        _body.MaxHeight = saved;
        return _body.DesiredSize.Height;
    }

    private void SetTransitions(bool enabled)
    {
        if (!enabled)
        {
            _body.Transitions = null;
            _chevron.Transitions = null;
            return;
        }
        _body.Transitions = new Transitions
        {
            new DoubleTransition { Property = Avalonia.Layout.Layoutable.MaxHeightProperty, Duration = Duration, Easing = Ease },
            new DoubleTransition { Property = Visual.OpacityProperty, Duration = Duration, Easing = Ease }
        };
        _chevron.Transitions = new Transitions
        {
            new TransformOperationsTransition { Property = Visual.RenderTransformProperty, Duration = Duration, Easing = Ease }
        };
    }

    private static ITransform Rotation(bool expanded) =>
        TransformOperations.Parse(expanded ? "rotate(180deg)" : "rotate(0deg)");
}
