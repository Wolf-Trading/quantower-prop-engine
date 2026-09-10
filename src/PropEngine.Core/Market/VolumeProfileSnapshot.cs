namespace PropEngine.Core.Market;

public readonly record struct PriceLevelVolume(
    double Price,
    double Volume,
    double BuyVolume,
    double SellVolume)
{
    public double Delta => BuyVolume - SellVolume;
    public double Total => Volume > 0 ? Volume : BuyVolume + SellVolume;
}

public sealed class VolumeProfileLevels
{
    public double Poc { get; init; }
    public double Vah { get; init; }
    public double Val { get; init; }
    public double TotalVolume { get; init; }
    public double DeltaAtVah { get; init; }
    public double DeltaAtVal { get; init; }
    public double DeltaAtPoc { get; init; }
    public double SessionDelta { get; init; }
    public bool FromTicks { get; init; }
    public bool Valid { get; init; }
    public IReadOnlyList<PriceLevelVolume> Levels { get; init; } = Array.Empty<PriceLevelVolume>();
}

public static class VolumeProfileSnapshot
{
    public static VolumeProfileLevels BuildFromBars(IReadOnlyList<Bar> bars, double tickSize, double valueAreaPct = 0.70)
    {
        if (bars.Count == 0 || tickSize <= 0) return new VolumeProfileLevels();
        var buckets = new Dictionary<long, PriceLevelVolume>();
        foreach (var b in bars)
        {
            var key = (long)Math.Round(b.Typical / tickSize);
            var price = key * tickSize;
            buckets.TryGetValue(key, out var cur);
            var buy = b.Bullish ? Math.Max(b.Volume, 1) : 0;
            var sell = b.Bullish ? 0 : Math.Max(b.Volume, 1);
            buckets[key] = new PriceLevelVolume(price, cur.Volume + Math.Max(b.Volume, 1), cur.BuyVolume + buy, cur.SellVolume + sell);
        }
        return BuildFromLevels(buckets.Values.ToList(), tickSize, valueAreaPct, fromTicks: false);
    }

    public static VolumeProfileLevels BuildFromLevels(IReadOnlyList<PriceLevelVolume> raw, double tickSize, double valueAreaPct = 0.70, bool fromTicks = true)
    {
        if (raw.Count == 0 || tickSize <= 0) return new VolumeProfileLevels();
        var buckets = new Dictionary<long, PriceLevelVolume>();
        foreach (var lvl in raw)
        {
            var key = (long)Math.Round(lvl.Price / tickSize);
            var price = key * tickSize;
            buckets.TryGetValue(key, out var cur);
            buckets[key] = new PriceLevelVolume(price, cur.Volume + lvl.Total, cur.BuyVolume + lvl.BuyVolume, cur.SellVolume + lvl.SellVolume);
        }
        var ordered = buckets.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
        if (ordered.Count == 0) return new VolumeProfileLevels();
        var poc = ordered.Aggregate((a, b) => a.Total >= b.Total ? a : b);
        var pocIndex = ordered.FindIndex(l => Math.Abs(l.Price - poc.Price) < tickSize * 0.1);
        if (pocIndex < 0) pocIndex = ordered.Count / 2;
        double total = ordered.Sum(l => l.Total);
        double covered = ordered[pocIndex].Total;
        int lo = pocIndex, hi = pocIndex;
        var target = total * valueAreaPct;
        while (covered < target && (lo > 0 || hi < ordered.Count - 1))
        {
            var takeLo = lo > 0 ? ordered[lo - 1].Total : -1;
            var takeHi = hi < ordered.Count - 1 ? ordered[hi + 1].Total : -1;
            if (takeHi >= takeLo) { hi++; covered += ordered[hi].Total; }
            else { lo--; covered += ordered[lo].Total; }
        }
        var vah = ordered[hi]; var val = ordered[lo];
        return new VolumeProfileLevels
        {
            Poc = poc.Price, Vah = vah.Price, Val = val.Price, TotalVolume = total,
            DeltaAtPoc = poc.Delta, DeltaAtVah = vah.Delta, DeltaAtVal = val.Delta,
            SessionDelta = ordered.Sum(l => l.Delta), FromTicks = fromTicks, Valid = true, Levels = ordered
        };
    }
}
