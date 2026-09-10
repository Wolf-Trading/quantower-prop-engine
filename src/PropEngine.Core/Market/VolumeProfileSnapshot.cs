namespace PropEngine.Core.Market;

public sealed class VolumeProfileLevels
{
    public double Poc { get; init; }
    public double Vah { get; init; }
    public double Val { get; init; }
    public double TotalVolume { get; init; }
    public bool Valid { get; init; }
}

public static class VolumeProfileSnapshot
{
    public static VolumeProfileLevels BuildFromBars(IReadOnlyList<Bar> bars, double tickSize, double valueAreaPct = 0.70)
    {
        if (bars.Count == 0 || tickSize <= 0)
            return new VolumeProfileLevels();

        var buckets = new Dictionary<long, double>();
        double total = 0;
        foreach (var b in bars)
        {
            var key = (long)Math.Round(b.Typical / tickSize);
            buckets.TryGetValue(key, out var v);
            buckets[key] = v + Math.Max(b.Volume, 1);
            total += Math.Max(b.Volume, 1);
        }

        if (buckets.Count == 0) return new VolumeProfileLevels();

        var pocKey = buckets.Aggregate((a, b) => a.Value >= b.Value ? a : b).Key;
        var poc = pocKey * tickSize;
        var ordered = buckets.OrderBy(kv => kv.Key).ToList();
        var pocIndex = ordered.FindIndex(kv => kv.Key == pocKey);
        double covered = ordered[pocIndex].Value;
        int lo = pocIndex, hi = pocIndex;
        var target = total * valueAreaPct;

        while (covered < target && (lo > 0 || hi < ordered.Count - 1))
        {
            var takeLo = lo > 0 ? ordered[lo - 1].Value : -1;
            var takeHi = hi < ordered.Count - 1 ? ordered[hi + 1].Value : -1;
            if (takeHi >= takeLo) { hi++; covered += ordered[hi].Value; }
            else { lo--; covered += ordered[lo].Value; }
        }

        return new VolumeProfileLevels
        {
            Poc = poc,
            Val = ordered[lo].Key * tickSize,
            Vah = ordered[hi].Key * tickSize,
            TotalVolume = total,
            Valid = true
        };
    }
}
