using PropEngine.Core.Config;

namespace PropEngine.Core.Risk;

public readonly record struct RiskSnapshot(
    EngineState State,
    double StartOfDayEquity,
    double RealizedPnl,
    double OpenPnl,
    double Equity,
    double PeakEquity,
    double TrailFloor,
    double RemainingTrailRoom,
    double RemainingDailyStopRoom,
    int TradesToday,
    int ConsecutiveLosses,
    DateTime? CooldownUntilUtc,
    string LockReason);

public readonly record struct EntryPermission(
    bool Allowed,
    double Quantity,
    string Reason);

public sealed class PropRiskEngine
{
    private readonly EngineSettings _s;
    private readonly SessionClock _clock;
    private readonly NewsCalendar _news;
    private readonly InstrumentSpec _spec;

    private DateTime _sessionDate;
    private double _startOfDayEquity;
    private double _realizedPnl;
    private double _openPnl;
    private double _peakEquity;
    private double _eodPeakEquity;
    private int _tradesToday;
    private int _consecutiveLosses;
    private DateTime? _cooldownUntil;
    private EngineState _state = EngineState.Idle;
    private string _lockReason = "";
    private SideKind _openSide = SideKind.Flat;
    private double _openQty;

    public PropRiskEngine(EngineSettings settings, SessionClock clock, NewsCalendar news, InstrumentSpec spec)
    {
        _s = settings;
        _clock = clock;
        _news = news;
        _spec = spec;
    }

    public EngineState State => _state;
    public string LockReason => _lockReason;
    public int TradesToday => _tradesToday;
    public SideKind OpenSide => _openSide;

    public void StartOrRollSession(DateTime utc, double currentEquity)
    {
        var day = _clock.SessionDate(utc);
        if (day != _sessionDate)
        {
            _sessionDate = day;
            _startOfDayEquity = currentEquity;
            _realizedPnl = 0;
            _openPnl = 0;
            _tradesToday = 0;
            _consecutiveLosses = 0;
            _cooldownUntil = null;
            _state = EngineState.SessionArmed;
            _lockReason = "";
            if (_s.Firm != FirmKind.ApexIntraday)
                _peakEquity = Math.Max(_peakEquity, currentEquity);
        }
        if (_eodPeakEquity <= 0) _eodPeakEquity = currentEquity;
        if (_peakEquity <= 0) _peakEquity = currentEquity;
    }

    public RiskSnapshot Mark(DateTime utc, double openPnl)
    {
        _openPnl = openPnl;
        var equity = _startOfDayEquity + _realizedPnl + _openPnl;
        if (_s.Firm == FirmKind.ApexIntraday)
            _peakEquity = Math.Max(_peakEquity, equity);
        else
            _peakEquity = Math.Max(_peakEquity, _eodPeakEquity);

        var floor = TrailFloor();
        var trailRoom = equity - floor;
        var dayPnl = _realizedPnl + _openPnl;
        var dailyStopRoom = _s.DailyStopDollars + dayPnl;
        if (_state is EngineState.Halted)
            return Snap(equity, floor, trailRoom, dailyStopRoom);
        if (_news.InBlackout(utc, out var newsLabel))
            Lock(EngineState.LockedNews, "news:" + newsLabel);
        else if (_state == EngineState.LockedNews && !_news.InBlackout(utc, out _))
            UnlockIfNoPosition();
        if (_clock.IsFlattenTime(utc))
            Lock(EngineState.LockedFlatten, "flatten-window");
        if (dayPnl <= -_s.DailyStopDollars)
            Lock(EngineState.LockedDailyStop, $"daily-stop {dayPnl:F0}");
        if (_s.FirmDailyLossLimit > 0 && dayPnl <= -(_s.FirmDailyLossLimit * 0.85))
            Lock(EngineState.LockedDailyStop, $"firm-dll-buffer {dayPnl:F0}");
        if (_realizedPnl >= _s.DailyGoalDollars && _openSide == SideKind.Flat)
            Lock(EngineState.LockedDailyGoal, $"daily-goal {_realizedPnl:F0}");
        if (trailRoom <= Math.Max(25, _s.RiskDollarsPerTrade * 0.5))
            Lock(EngineState.Halted, $"trail-room {trailRoom:F0}");
        if (_cooldownUntil is DateTime cd && utc < cd && _openSide == SideKind.Flat)
            Lock(EngineState.LockedCooldown, "cooldown");
        else if (_state == EngineState.LockedCooldown && (_cooldownUntil is null || utc >= _cooldownUntil))
            UnlockIfNoPosition();
        return Snap(equity, floor, trailRoom, dailyStopRoom);
    }

    public bool MustFlatten(DateTime utc, out string reason)
    {
        var snap = Mark(utc, _openPnl);
        if (_clock.IsFlattenTime(utc)) { reason = "flatten-window"; return true; }
        if (_state is EngineState.LockedDailyStop or EngineState.Halted or EngineState.LockedNews)
        { reason = _lockReason; return true; }
        if (_s.Firm == FirmKind.ApexIntraday)
        {
            var cap = snap.RemainingTrailRoom * _s.IntradayTrailHeadroomPct;
            var openRisk = Math.Max(0, -_openPnl);
            if (_openSide != SideKind.Flat && openRisk > Math.Max(20, cap))
            { reason = "intraday-trail-headroom"; return true; }
        }
        reason = "";
        return false;
    }

    public EntryPermission CanEnter(DateTime utc, SideKind side, double stopPoints, double requestedQty)
    {
        if (side == SideKind.Flat) return new(false, 0, "flat-side");
        if (_state is EngineState.LockedDailyStop or EngineState.LockedDailyGoal
            or EngineState.LockedFlatten or EngineState.Halted or EngineState.LockedNews
            or EngineState.LockedCooldown)
            return new(false, 0, _state + ":" + _lockReason);
        if (!_clock.CanEnterByTime(utc)) return new(false, 0, "outside-entry-window");
        if (_tradesToday >= _s.MaxTradesPerDay) return new(false, 0, "max-trades");
        if (_consecutiveLosses >= _s.ConsecutiveLossLock) return new(false, 0, "consecutive-losses");
        if (_s.OneDirectionOnly && _openSide != SideKind.Flat && _openSide != side) return new(false, 0, "one-direction");
        if (_openSide != SideKind.Flat) return new(false, 0, "already-in-trade");
        var qty = requestedQty;
        if (qty <= 0)
        {
            var perContract = _spec.PointsToDollars(stopPoints, 1);
            qty = perContract <= 0 ? 0 : Math.Floor(_s.RiskDollarsPerTrade / perContract);
        }
        qty = Math.Min(qty, _s.MaxContracts);
        if (qty < 1) return new(false, 0, "size-zero");
        var risk = _spec.PointsToDollars(stopPoints, qty);
        if (risk > _s.RiskDollarsPerTrade * 1.15)
        {
            qty = Math.Floor(qty * (_s.RiskDollarsPerTrade / risk));
            risk = _spec.PointsToDollars(stopPoints, qty);
        }
        var snap = Mark(utc, _openPnl);
        if (risk > snap.RemainingDailyStopRoom * 0.6) return new(false, 0, "daily-stop-headroom");
        if (risk > snap.RemainingTrailRoom * (_s.Firm == FirmKind.ApexIntraday ? _s.IntradayTrailHeadroomPct : 0.5))
            return new(false, 0, "trail-headroom");
        if (_s.Mode is ExecutionMode.SignalsOnly or ExecutionMode.ManageOnly) return new(false, qty, "mode-blocks-entry");
        if (_s.Mode == ExecutionMode.SemiAuto && !_s.EnableEntries) return new(false, qty, "enable-entries-off");
        return new(true, qty, "ok");
    }

    public void OnEntryFilled(SideKind side, double qty)
    {
        _openSide = side; _openQty = qty; _tradesToday++; _state = EngineState.InTrade; _lockReason = "";
    }

    public void OnTradeClosed(double realizedDollars)
    {
        _realizedPnl += realizedDollars; _openPnl = 0; _openSide = SideKind.Flat; _openQty = 0;
        if (realizedDollars < 0) { _consecutiveLosses++; _cooldownUntil = DateTime.UtcNow.AddMinutes(_s.CooldownMinutesAfterLoss); }
        else { _consecutiveLosses = 0; _cooldownUntil = null; }
        if (_realizedPnl >= _s.DailyGoalDollars) Lock(EngineState.LockedDailyGoal, $"daily-goal {_realizedPnl:F0}");
        else if (_realizedPnl <= -_s.DailyStopDollars) Lock(EngineState.LockedDailyStop, $"daily-stop {_realizedPnl:F0}");
        else _state = EngineState.SessionArmed;
    }

    public void CloseSessionMark(double equity)
    {
        _eodPeakEquity = Math.Max(_eodPeakEquity, equity);
        if (_s.Firm != FirmKind.ApexIntraday) _peakEquity = _eodPeakEquity;
    }

    public void Halt(string reason) => Lock(EngineState.Halted, reason);

    private double TrailFloor()
    {
        var highWater = _s.Firm == FirmKind.ApexIntraday ? _peakEquity : _eodPeakEquity;
        if (highWater <= 0) highWater = _startOfDayEquity;
        return Math.Max(highWater - _s.FirmTrailDollars, _s.AccountStartBalance - _s.FirmTrailDollars);
    }

    private void Lock(EngineState state, string reason) { _state = state; _lockReason = reason; }
    private void UnlockIfNoPosition()
    {
        if (_openSide != SideKind.Flat) return;
        if (_realizedPnl <= -_s.DailyStopDollars) return;
        if (_realizedPnl >= _s.DailyGoalDollars) return;
        _state = EngineState.SessionArmed; _lockReason = "";
    }
    private RiskSnapshot Snap(double equity, double floor, double trailRoom, double dailyStopRoom)
        => new(_state, _startOfDayEquity, _realizedPnl, _openPnl, equity, _peakEquity, floor, trailRoom, dailyStopRoom, _tradesToday, _consecutiveLosses, _cooldownUntil, _lockReason);
}
