# Prop VWAP-OR Engine for Quantower

RTH **opening-range + session VWAP + volume-profile filter** with a **Topstep / Apex-style risk overlay**, written so the brains compile *without* Quantower.

This is a starting system for MES / MNQ (and ES / NQ once the trail has room). It is not a vendor signal pack and not a guarantee you pass an evaluation.

Read [DISCLAIMER.md](DISCLAIMER.md) first.

## What you get

```
src/PropEngine.Core        Pure C# session engine (risk, clock, news, VWAP, OR, signals, journal)
src/PropEngine.Quantower   Strategy Runner wrapper (PlaceOrder, flatten, metrics)
src/PropEngine.Tests       xUnit coverage for risk locks and signal gates
configs/                   $50K presets for Topstep, Apex EOD, Apex Intraday
docs/                      Deploy, parameters, backtest protocol
```

### Risk overlay (the actual product)

- Daily stop inside the firm DLL, then lock
- Daily goal, then lock (consistency-friendly)
- Flatten window default 15:50 ET, last entry 15:30 ET
- News blackout around seeded 2026 CPI / NFP / FOMC plus your extra dates
- One position, one direction
- Contract cap and dollar-risk sizer for MES/MNQ/ES/NQ
- Apex *intraday* trail: flatten if open risk eats peak-equity headroom
- Cooldown after a loss, lock after two consecutive losers
- SemiAuto by default: you flip **Enable entries** to arm

### Signals

1. OR breakout that agrees with VWAP  
2. VWAP-band fade only on Balance days  
3. Prior-session / developing value rejection toward POC  

One setup at a time. Highest confidence wins.

## Build

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet test src/PropEngine.sln
```

Quantower wrapper build (Windows, after you set the DLL path):

```bash
dotnet build src/PropEngine.Quantower/PropEngine.Quantower.csproj -c Release -p:QuantowerLib=C:\Quantower\TradingPlatform.BusinessLayer.dll
```

Copy `PropEngine.Core.dll` and `PropEngine.Quantower.dll` into the Quantower Strategies folder. Details: [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).

## First session

1. Pick **MNQ** or **MES**, not a full mini.  
2. Firm code **1** (Topstep) or **2** (Apex EOD). Avoid Apex Intraday until you have watched the trail.  
3. Mode **2 SemiAuto**, Enable entries **false**.  
4. Watch metrics: `OR`, `VWAP`, `Regime`, `Setup`, `State`, `TrailRoom`.  
5. If setups match how you already trade, arm Enable entries for a single morning.  
6. When daily goal prints, leave it off.

## Platform reality (read this)

- Topstep’s Quantower bundle has been documented **without Strategy Runner**. The wrapper needs Algo / Runner. Use signals-only or another allowed API if Runner is blocked.
- Apex has a real Quantower + Rithmic connection, but official pages have also said **no automation**. Keep a human in the chair.
- Rules, drawdown math, and allowed platforms change. The JSON presets are templates, not a contract.

## License

MIT. Markets can still take the money.
