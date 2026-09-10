namespace PropEngine.Core.Market;

public sealed class VwapSnapshot
{
    public double Vwap { get; init; }
    public double Sigma { get; init; }
    public double Upper1 { get; init; }
    public double Lower1 { get; init; }
    public double Upper2 { get; init; }
    public double Lower2 { get; init; }
    public double Slope { get; init; }
    public double CumulativeVolume { get; init; }
}

public sealed class VwapCalculator
{
    private double _sumPv;
    private double _sumV;
    private double _sumPv2;
    private double? _prevVwap;
    private readonly Queue<double> _vwapTrail = new();

    public void Reset()
    {
        _sumPv = _sumV = _sumPv2 = 0;
        _prevVwap = null;
        _vwapTrail.Clear();
    }

    public VwapSnapshot Add(Bar bar)
    {
        var tp = bar.Typical;
        var v = Math.Max(bar.Volume, 1);
        _sumPv += tp * v;
        _sumPv2 += tp * tp * v;
        _sumV += v;

        var vwap = _sumV <= 0 ? tp : _sumPv / _sumV;
        var variance = Math.Max(0, (_sumPv2 / _sumV) - (vwap * vwap));
        var sigma = Math.Sqrt(variance);

        _vwapTrail.Enqueue(vwap);
        while (_vwapTrail.Count > 10) _vwapTrail.Dequeue();
        var slope = _vwapTrail.Count >= 2 ? vwap - _vwapTrail.Peek() : 0;
        _prevVwap = vwap;
        return new VwapSnapshot
        {
            Vwap = vwap,
            Sigma = sigma,
            Upper1 = vwap + sigma,
            Lower1 = vwap - sigma,
            Upper2 = vwap + 2 * sigma,
            Lower2 = vwap - 2 * sigma,
            Slope = slope,
            CumulativeVolume = _sumV
        };
    }
}
