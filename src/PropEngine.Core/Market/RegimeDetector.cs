using PropEngine.Core.Config;

namespace PropEngine.Core.Market;

public sealed class RegimeSnapshot
{
    public RegimeKind Kind { get; init; }
    public double Atr { get; init; }
    public double OrWidth { get; init; }
    public double VwapSlope { get; init; }
    public string Detail { get; init; } = "";
}

public static class RegimeDetector
{
    public static double Atr(IReadOnlyList<Bar> bars, int lookback)
    {
        if (bars.Count < 2) return 0;
        var n = Math.Min(lookback, bars.Count - 1);
        double sum = 0;
        for (var i = bars.Count - n; i < bars.Count; i++)
        {
            var b = bars[i];
            var prev = bars[i - 1];
            var tr = Math.Max(b.High - b.Low, Math.Max(Math.Abs(b.High - prev.Close), Math.Abs(b.Low - prev.Close)));
            sum += tr;
        }
        return n <= 0 ? 0 : sum / n;
    }

    public static RegimeSnapshot Detect(EngineSettings s, OpeningRangeState or, VwapSnapshot vwap, IReadOnlyList<Bar> recent)
    {
        var atr = Atr(recent, s.AtrLookback);
        if (!or.Complete || atr <= 0)
            return new RegimeSnapshot { Kind = RegimeKind.Unknown, Atr = atr, OrWidth = or.Width, VwapSlope = vwap.Slope, Detail = "warming-up" };

        var wideOr = or.Width >= atr * s.TrendOrAtrFraction;
        var sloped = Math.Abs(vwap.Slope) >= atr * 0.08;
        var kind = (wideOr && sloped) ? RegimeKind.Trend : RegimeKind.Balance;
        return new RegimeSnapshot
        {
            Kind = kind,
            Atr = atr,
            OrWidth = or.Width,
            VwapSlope = vwap.Slope,
            Detail = wideOr ? "wide-OR" : "narrow-OR"
        };
    }
}
