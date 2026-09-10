# Samples

Drop 1-minute RTH bars here if you write a thin replay harness around `SessionEngine`.

Expected columns:

```
TimeUtc,Open,High,Low,Close,Volume
2026-09-10T13:30:00Z,24680.00,24684.25,24678.50,24682.00,1842
```

`TimeUtc` should be the bar **open** (Quantower `HistoryItemBar.TimeLeft` after converting to UTC).
