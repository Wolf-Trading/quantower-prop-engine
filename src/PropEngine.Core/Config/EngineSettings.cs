namespace PropEngine.Core.Config;

public sealed class EngineSettings
{
    public FirmKind Firm { get; set; } = FirmKind.Topstep;
    public ExecutionMode Mode { get; set; } = ExecutionMode.SemiAuto;
    public bool EnableEntries { get; set; } = false;

    public string TimeZoneId { get; set; } = "America/New_York";

    public int SessionOpenHour { get; set; } = 9;
    public int SessionOpenMinute { get; set; } = 30;
    public int SessionCloseHour { get; set; } = 16;
    public int SessionCloseMinute { get; set; } = 0;

    public int FlattenHour { get; set; } = 15;
    public int FlattenMinute { get; set; } = 50;
    public int LastEntryHour { get; set; } = 15;
    public int LastEntryMinute { get; set; } = 30;

    public int LunchStartHour { get; set; } = 11;
    public int LunchStartMinute { get; set; } = 30;
    public int LunchEndHour { get; set; } = 13;
    public int LunchEndMinute { get; set; } = 30;
    public bool SkipLunchEntries { get; set; } = true;

    public int OpeningRangeMinutes { get; set; } = 15;
    public double VwapBandSigma { get; set; } = 2.0;
    public double NqFadeSigma { get; set; } = 2.25;
    public double TrendOrAtrFraction { get; set; } = 0.85;
    public int AtrLookback { get; set; } = 20;

    public bool AllowOrBreakout { get; set; } = true;
    public bool AllowVwapFade { get; set; } = true;
    public bool AllowProfileRejection { get; set; } = true;

    public double RiskDollarsPerTrade { get; set; } = 100;
    public double DailyStopDollars { get; set; } = 500;
    public double DailyGoalDollars { get; set; } = 400;
    public double MaxContracts { get; set; } = 10;
    public int MaxTradesPerDay { get; set; } = 4;
    public int CooldownMinutesAfterLoss { get; set; } = 8;
    public int ConsecutiveLossLock { get; set; } = 2;

    public double AccountStartBalance { get; set; } = 50_000;
    public double FirmTrailDollars { get; set; } = 2_000;
    public double FirmDailyLossLimit { get; set; } = 1_000;
    public double FirmProfitTarget { get; set; } = 3_000;
    public double ConsistencyCapPct { get; set; } = 0.50;
    public double IntradayTrailHeadroomPct { get; set; } = 0.35;

    public double StopBufferTicks { get; set; } = 2;
    public double MinStopTicks { get; set; } = 8;
    public double MaxStopTicks { get; set; } = 80;
    public double RewardRisk { get; set; } = 1.6;
    public double ScaleOutR { get; set; } = 1.0;
    public double ScaleOutFraction { get; set; } = 0.5;
    public bool MoveStopToBreakevenAt1R { get; set; } = true;
    public int TimeStopMinutes { get; set; } = 12;
    public double TimeStopMinR { get; set; } = 0.35;

    public int NewsBlackoutMinutes { get; set; } = 5;
    public string ExtraBlackoutWindows { get; set; } = "";

    public bool OneDirectionOnly { get; set; } = true;
    public bool FlattenOnStop { get; set; } = true;

    public static EngineSettings Topstep50k() => new()
    {
        Firm = FirmKind.Topstep,
        AccountStartBalance = 50_000,
        FirmTrailDollars = 2_000,
        FirmDailyLossLimit = 1_000,
        FirmProfitTarget = 3_000,
        ConsistencyCapPct = 0.50,
        DailyStopDollars = 500,
        DailyGoalDollars = 400,
        MaxContracts = 10,
        RiskDollarsPerTrade = 100
    };

    public static EngineSettings ApexEod50k() => new()
    {
        Firm = FirmKind.ApexEod,
        AccountStartBalance = 50_000,
        FirmTrailDollars = 2_000,
        FirmDailyLossLimit = 1_000,
        FirmProfitTarget = 3_000,
        ConsistencyCapPct = 0.50,
        DailyStopDollars = 450,
        DailyGoalDollars = 350,
        MaxContracts = 10,
        RiskDollarsPerTrade = 80
    };

    public static EngineSettings ApexIntraday50k() => new()
    {
        Firm = FirmKind.ApexIntraday,
        AccountStartBalance = 50_000,
        FirmTrailDollars = 2_000,
        FirmDailyLossLimit = 1_000,
        FirmProfitTarget = 3_000,
        ConsistencyCapPct = 0.50,
        DailyStopDollars = 350,
        DailyGoalDollars = 300,
        MaxContracts = 6,
        RiskDollarsPerTrade = 70,
        IntradayTrailHeadroomPct = 0.30,
        RewardRisk = 1.3
    };
}
