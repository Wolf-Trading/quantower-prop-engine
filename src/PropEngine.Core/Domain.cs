namespace PropEngine.Core;

public enum SideKind
{
    Flat = 0,
    Buy = 1,
    Sell = -1
}

public enum ExecutionMode
{
    /// <summary>Compute levels and setups only. Never send orders.</summary>
    SignalsOnly = 0,
    /// <summary>Manage / flatten existing exposure. No new entries.</summary>
    ManageOnly = 1,
    /// <summary>New entries require EnableEntries = true. Always manages risk, flatten, stops.</summary>
    SemiAuto = 2,
    /// <summary>Entries fire on bar close when risk allows. Still respects flatten / news / daily locks.</summary>
    Auto = 3
}

public enum FirmKind
{
    Generic = 0,
    Topstep = 1,
    ApexEod = 2,
    ApexIntraday = 3
}

public enum DrawdownStyle
{
    EndOfDay = 0,
    IntradayTrailing = 1
}

public enum RegimeKind
{
    Unknown = 0,
    Balance = 1,
    Trend = 2
}

public enum SetupKind
{
    None = 0,
    OrBreakout = 1,
    VwapFade = 2,
    ProfileRejection = 3
}

public enum EngineState
{
    Idle = 0,
    SessionArmed = 1,
    SetupReady = 2,
    InTrade = 3,
    LockedDailyStop = 4,
    LockedDailyGoal = 5,
    LockedNews = 6,
    LockedFlatten = 7,
    LockedCooldown = 8,
    Halted = 9
}

public readonly record struct Bar(
    DateTime TimeUtc,
    double Open,
    double High,
    double Low,
    double Close,
    double Volume)
{
    public double Typical => (High + Low + Close) / 3.0;
    public double Range => High - Low;
    public bool Bullish => Close >= Open;
}

public sealed class InstrumentSpec
{
    public required string Root { get; init; }
    public required double TickSize { get; init; }
    public required double TickValue { get; init; }
    public required int MicroPerMini { get; init; }
    public bool IsMicro { get; init; }

    public static InstrumentSpec Es { get; } = new() { Root = "ES", TickSize = 0.25, TickValue = 12.50, MicroPerMini = 10, IsMicro = false };
    public static InstrumentSpec Mes { get; } = new() { Root = "MES", TickSize = 0.25, TickValue = 1.25, MicroPerMini = 10, IsMicro = true };
    public static InstrumentSpec Nq { get; } = new() { Root = "NQ", TickSize = 0.25, TickValue = 5.00, MicroPerMini = 10, IsMicro = false };
    public static InstrumentSpec Mnq { get; } = new() { Root = "MNQ", TickSize = 0.25, TickValue = 0.50, MicroPerMini = 10, IsMicro = true };

    public static InstrumentSpec FromSymbolName(string? name)
    {
        var n = (name ?? string.Empty).ToUpperInvariant();
        if (n.Contains("MNQ")) return Mnq;
        if (n.Contains("MES")) return Mes;
        if (n.Contains("NQ")) return Nq;
        if (n.Contains("ES")) return Es;
        return Mnq;
    }

    public double PointsToDollars(double points, double quantity)
        => (points / TickSize) * TickValue * quantity;

    public double DollarsToPoints(double dollars, double quantity)
    {
        if (quantity <= 0 || TickValue <= 0) return 0;
        return (dollars / (TickValue * quantity)) * TickSize;
    }

    public int PointsToTicks(double points)
        => (int)Math.Round(points / TickSize);
}
