using PropEngine.Core.Config;

namespace PropEngine.Core.Risk;

public sealed class SessionClock
{
    private readonly EngineSettings _s;
    private readonly TimeZoneInfo _tz;

    public SessionClock(EngineSettings settings)
    {
        _s = settings;
        _tz = ResolveTz(settings.TimeZoneId);
    }

    public TimeZoneInfo TimeZone => _tz;

    public DateTime ToEt(DateTime utc)
    {
        var u = utc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utc, DateTimeKind.Utc)
            : utc.ToUniversalTime();
        return TimeZoneInfo.ConvertTimeFromUtc(u, _tz);
    }

    public DateTime SessionDate(DateTime utc)
    {
        var et = ToEt(utc);
        return et.Hour >= 18 ? et.Date.AddDays(1) : et.Date;
    }

    public DateTime RthOpenUtc(DateTime utc)
    {
        var date = SessionDate(utc);
        var local = new DateTime(date.Year, date.Month, date.Day, _s.SessionOpenHour, _s.SessionOpenMinute, 0);
        return TimeZoneInfo.ConvertTimeToUtc(local, _tz);
    }

    public DateTime RthCloseUtc(DateTime utc)
    {
        var date = SessionDate(utc);
        var local = new DateTime(date.Year, date.Month, date.Day, _s.SessionCloseHour, _s.SessionCloseMinute, 0);
        return TimeZoneInfo.ConvertTimeToUtc(local, _tz);
    }

    public bool IsRth(DateTime utc)
    {
        var et = ToEt(utc);
        var open = new TimeSpan(_s.SessionOpenHour, _s.SessionOpenMinute, 0);
        var close = new TimeSpan(_s.SessionCloseHour, _s.SessionCloseMinute, 0);
        var t = et.TimeOfDay;
        return t >= open && t < close;
    }

    public bool IsLunch(DateTime utc)
    {
        var t = ToEt(utc).TimeOfDay;
        var a = new TimeSpan(_s.LunchStartHour, _s.LunchStartMinute, 0);
        var b = new TimeSpan(_s.LunchEndHour, _s.LunchEndMinute, 0);
        return t >= a && t < b;
    }

    public bool IsFlattenTime(DateTime utc)
    {
        var t = ToEt(utc).TimeOfDay;
        var flatten = new TimeSpan(_s.FlattenHour, _s.FlattenMinute, 0);
        var close = new TimeSpan(_s.SessionCloseHour, _s.SessionCloseMinute, 0);
        return t >= flatten || t >= close;
    }

    public bool CanEnterByTime(DateTime utc)
    {
        if (!IsRth(utc) || IsFlattenTime(utc)) return false;
        var t = ToEt(utc).TimeOfDay;
        var last = new TimeSpan(_s.LastEntryHour, _s.LastEntryMinute, 0);
        if (t >= last) return false;
        if (_s.SkipLunchEntries && IsLunch(utc)) return false;
        return true;
    }

    public int MinutesSinceRthOpen(DateTime utc)
    {
        var open = RthOpenUtc(utc);
        return (int)Math.Floor((utc.ToUniversalTime() - open).TotalMinutes);
    }

    private static TimeZoneInfo ResolveTz(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
            catch { return TimeZoneInfo.Utc; }
        }
    }
}
