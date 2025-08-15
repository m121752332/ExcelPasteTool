using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Reflection;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Styling;
using Avalonia.Threading;
using System.Threading;

namespace ExcelPasteTool;

public partial class AboutView : UserControl
{
    private TextBlock? _appTitleText;
    private TextBlock? _versionText;
    private TextBlock? _copyright;
    private TextBlock? _description;

    private Canvas? _bubbleLayer;
    private Grid? _rootGrid;
    private bool _isPlaying;
    private readonly string[] _messages = new[]
    {
        "這個讚",
        "非常實用",
        "你好棒",
        "這工具真好用",
        "這就是我要的APP"
    };

    public AboutView()
    {
        AvaloniaXamlLoader.Load(this);
        _appTitleText = this.FindControl<TextBlock>("AppTitleText");
        _versionText = this.FindControl<TextBlock>("VersionText");
        _copyright = this.FindControl<TextBlock>("CopyrightText");
        _description = this.FindControl<TextBlock>("DescriptionText");
        _bubbleLayer = this.FindControl<Canvas>("BubbleLayer");
        _rootGrid = this.FindControl<Grid>("RootGrid");

        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var name = asm.GetName();
        _appTitleText!.Text = name.Name ?? "DataSplitter Pro";
        _versionText!.Text = $"v{(name.Version?.ToString(3) ?? "1.0.0")}";

        _copyright!.Text = $"{GetAssemblyAttribute<AssemblyCopyrightAttribute>(asm)?.Copyright}";
        var desc = asm.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "Excel Data Splitter Tool";
        _description!.Text = desc;
    }

    private async void DeveloperHotArea_OnPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (_isPlaying) return;
        _isPlaying = true;
        try
        {
            for (int i = 0; i < _messages.Length; i++)
            {
                ShowBubble(_messages[i]);
                await Task.Delay(1000);
            }
        }
        finally { _isPlaying = false; }
    }

    private void ShowBubble(string text)
    {
        if (_bubbleLayer == null || _rootGrid == null) return;

        var rnd = new Random();
        var startX = rnd.Next(100, Math.Max(120, (int)_rootGrid.Bounds.Width - 100));
        var startY = (int)_rootGrid.Bounds.Height - 120;

        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(230, 59, 130, 246)), // #3B82F6 with alpha
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(12, 6),
            Child = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                FontSize = 12
            }
        };

        Canvas.SetLeft(border, startX);
        Canvas.SetTop(border, startY);
        _bubbleLayer.Children.Add(border);

        // Extend float duration by +1s (from 3000ms -> 4000ms)
        var durationMs = 4000;
        var anim = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(durationMs),
            Easing = new CubicEaseInOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(Visual.OpacityProperty, 0d) } },
                new KeyFrame { Cue = new Cue(0.1d), Setters = { new Setter(Visual.OpacityProperty, 1d) } },
                new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(Visual.OpacityProperty, 0d) } }
            }
        };

        // Vertical translate animation by updating Canvas.Top
        var totalMove = rnd.Next(80, 120);
        var startTop = Canvas.GetTop(border);
        var start = DateTimeOffset.Now;
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        timer.Tick += (_, __) =>
        {
            var t = (DateTimeOffset.Now - start).TotalMilliseconds / durationMs;
            if (t >= 1)
            {
                timer.Stop();
                _bubbleLayer.Children.Remove(border);
                return;
            }
            var y = startTop - totalMove * EaseOutCubic(t);
            Canvas.SetTop(border, y);
        };
        timer.Start();

        anim.RunAsync(border, CancellationToken.None);
    }

    private static double EaseOutCubic(double t)
    {
        t = Math.Clamp(t, 0, 1);
        t = 1 - Math.Pow(1 - t, 3);
        return t;
    }

    private static T? GetAssemblyAttribute<T>(Assembly assembly) where T : Attribute
    {
        return assembly.GetCustomAttribute<T>();
    }

    private static string GetFriendlyAppName(string assemblyName)
    {
        // 將程式集名稱轉換為更友好的顯示名稱
        return assemblyName switch
        {
            "ExcelPasteTool" => "DataSplitter Pro",
            "DataSplitter" => "DataSplitter Pro",
            _ => assemblyName
        };
    }
}