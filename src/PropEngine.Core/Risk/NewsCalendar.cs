using PropEngine.Core.Config;

namespace PropEngine.Core.Risk;

public readonly record struct NewsWindow(DateTime StartUtc, DateTime EndUtc, string Label);

public sealed class NewsCalendar
{
    private readonly EngineSettings _s;
    private readonly SessionClock _clock;
    private readonly List<NewsWindow> _windows = new();

    public NewsCalendar(EngineSettings settings, SessionClock clock)
    {
        _s = settings;
        _clock = clock;
        SeedKnown2026H2();
        ParseExtra(settings.ExtraBlackoutWindows);
    }

    public IReadOnlyList<NewsWindow> Windows => _windows;

    public bool InBlackout(DateTime utc, out string reason)
    {
        foreach (var w in _windows)
        {
            if (utc >= w.StartUtc && utc <= w.EndUtc)
            {
                reason = w.Label;
                return true;
            }
        }
        reason = "";
        return false;
    }

    public void AddEt(DateTime etLocal, string label)
    {
        var unspecified = DateTime.SpecifyKind(etLocal, DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, _clock.TimeZone);
        var pad = TimeSpan.FromMinutes(_s.NewsBlackoutMinutes);
        _windows.Add(new NewsWindow(utc - pad, utc + pad, label));
    }

    private void ParseExtra(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;
        foreach (var part in raw.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (DateTime.TryParse(part.Trim(), out var et))
                AddEt(et, "custom:" + part.Trim());
        }
    }

    private void SeedKnown2026H2()
    {
        AddEt(new DateTime(2026, 9, 11, 8, 30, 0), "CPI");
        AddEt(new DateTime(2026, 10, 2, 8, 30, 0), "NFP");
        AddEt(new DateTime(2026, 10, 14, 8, 30, 0), "CPI");
        AddEt(new DateTime(2026, 10, 29, 14, 00, 0), "FOMC");
        AddEt(new DateTime(2026, 11, 6, 8, 30, 0), "NFP");
        AddEt(new DateTime(2026, 11, 12, 8, 30, 0), "CPI");
        AddEt(new DateTime(2026, 12, 4, 8, 30, 0), "NFP");
        AddEt(new DateTime(2026, 12, 10, 8, 30, 0), "CPI");
        AddEt(new DateTime(2026, 12, 16, 14, 00, 0), "FOMC");
    }
}
