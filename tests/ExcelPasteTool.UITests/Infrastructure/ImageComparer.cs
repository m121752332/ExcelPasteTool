using SkiaSharp;

namespace ExcelPasteTool.UITests.Infrastructure;

/// <summary>視覺比對的容許值。</summary>
public sealed record VisualCompareOptions
{
    /// <summary>單一像素任一色版（R/G/B/A）差多少以內視為相同（0~255）。吸收反鋸齒造成的微小差異。</summary>
    public int ChannelTolerance { get; init; } = 16;

    /// <summary>不同像素占全部像素的比例上限（0.002 = 0.2%）。</summary>
    public double MaxDiffRatio { get; init; } = 0.002;

    public static VisualCompareOptions Default { get; } = new();
}

/// <summary>一次比對的結果；同時是寫進 TestResults/ui/visual/*.json 的內容。</summary>
public sealed record VisualCompareResult
{
    public required string Name { get; init; }
    /// <summary>passed / failed / size-mismatch / baseline-missing / baseline-updated</summary>
    public required string Status { get; init; }
    public bool Passed => Status is "passed" or "baseline-updated";
    public string? Baseline { get; init; }
    public string? Actual { get; init; }
    public string? Diff { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public int BaselineWidth { get; init; }
    public int BaselineHeight { get; init; }
    public long DiffPixels { get; init; }
    public double DiffRatio { get; init; }
    public int MaxChannelDelta { get; init; }
    public int ChannelTolerance { get; init; }
    public double MaxDiffRatio { get; init; }
    public string? Message { get; init; }
}

/// <summary>用 SkiaSharp（Avalonia.Skia 本來就會帶進來）逐像素比對兩張 PNG，並輸出差異圖（不同處標紅，其餘淡化成灰階）。</summary>
public static class ImageComparer
{
    public static VisualCompareResult Compare(string name, string baselinePath, string actualPath, string diffPath,
        VisualCompareOptions options)
    {
        using var expected = SKBitmap.Decode(baselinePath)
            ?? throw new InvalidOperationException($"無法讀取基準圖：{baselinePath}");
        using var actual = SKBitmap.Decode(actualPath)
            ?? throw new InvalidOperationException($"無法讀取擷取圖：{actualPath}");

        if (expected.Width != actual.Width || expected.Height != actual.Height)
        {
            return new VisualCompareResult
            {
                Name = name,
                Status = "size-mismatch",
                Baseline = baselinePath,
                Actual = actualPath,
                Width = actual.Width,
                Height = actual.Height,
                BaselineWidth = expected.Width,
                BaselineHeight = expected.Height,
                ChannelTolerance = options.ChannelTolerance,
                MaxDiffRatio = options.MaxDiffRatio,
                Message = $"畫面尺寸不同：基準 {expected.Width}x{expected.Height}，實際 {actual.Width}x{actual.Height}"
            };
        }

        int width = actual.Width, height = actual.Height;
        long diffPixels = 0;
        int maxDelta = 0;
        using var diff = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var e = expected.GetPixel(x, y);
                var a = actual.GetPixel(x, y);
                int delta = Math.Max(
                    Math.Max(Math.Abs(e.Red - a.Red), Math.Abs(e.Green - a.Green)),
                    Math.Max(Math.Abs(e.Blue - a.Blue), Math.Abs(e.Alpha - a.Alpha)));
                if (delta > maxDelta) maxDelta = delta;

                if (delta > options.ChannelTolerance)
                {
                    diffPixels++;
                    diff.SetPixel(x, y, new SKColor(255, 0, 64, 255));
                }
                else
                {
                    // 相同的地方淡化成灰階，讓紅色差異一眼就看得到
                    var gray = (byte)(200 + (a.Red * 0.299 + a.Green * 0.587 + a.Blue * 0.114) / 255 * 55);
                    diff.SetPixel(x, y, new SKColor(gray, gray, gray, 255));
                }
            }
        }

        double ratio = (double)diffPixels / ((long)width * height);
        bool passed = ratio <= options.MaxDiffRatio;
        if (!passed)
        {
            using var image = SKImage.FromBitmap(diff);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.Create(diffPath);
            data.SaveTo(stream);
        }

        return new VisualCompareResult
        {
            Name = name,
            Status = passed ? "passed" : "failed",
            Baseline = baselinePath,
            Actual = actualPath,
            Diff = passed ? null : diffPath,
            Width = width,
            Height = height,
            BaselineWidth = expected.Width,
            BaselineHeight = expected.Height,
            DiffPixels = diffPixels,
            DiffRatio = Math.Round(ratio, 6),
            MaxChannelDelta = maxDelta,
            ChannelTolerance = options.ChannelTolerance,
            MaxDiffRatio = options.MaxDiffRatio,
            Message = passed
                ? null
                : $"與基準圖差異 {ratio:P3}（{diffPixels} 個像素，上限 {options.MaxDiffRatio:P3}），差異圖：{diffPath}"
        };
    }
}
