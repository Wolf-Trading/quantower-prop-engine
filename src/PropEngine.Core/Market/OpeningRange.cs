namespace PropEngine.Core.Market;

public sealed class OpeningRangeState
{
    public bool Complete { get; init; }
    public double High { get; init; }
    public double Low { get; init; }
    public double Mid => (High + Low) / 2.0;
    public double Width { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
}

public static class OpeningRange
{
    public static OpeningRangeState Build(IReadOnlyList<Bar> rthBars, DateTime rthOpenUtc, int minutes)
    {
        var end = rthOpenUtc.AddMinutes(minutes);
        double hi = double.MinValue, lo = double.MaxValue;
        var any = false;
        foreach (var b in rthBars)
        {
            if (b.TimeUtc < rthOpenUtc) continue;
            if (b.TimeUtc >= end) break;
            hi = Math.Max(hi, b.High);
            lo = Math.Min(lo, b.Low);
            any = true;
        }

        if (!any)
            return new OpeningRangeState { Complete = false, StartUtc = rthOpenUtc, EndUtc = end };

        var complete = rthBars.Count > 0 && rthBars[^1].TimeUtc >= end;
        return new OpeningRangeState
        {
            Complete = complete,
            High = hi,
            Low = lo,
            Width = hi - lo,
            StartUtc = rthOpenUtc,
            EndUtc = end
        };
    }
}
