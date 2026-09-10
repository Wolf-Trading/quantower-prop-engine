# Backtest protocol

Quantower Backtest & Optimize can host the strategy wrapper if your license includes Algo. The Core library can also be driven from a CSV of 1-minute bars via `SessionEngine.OnBar`.

## Platform settings

- Session template: CME RTH 09:30–16:00 ET
- Netting: one net position (matches one-direction)
- Fees: use your real Rithmic/Tradovate/TopstepX commission
- Slippage stress: ES 0.25–0.50 pt, NQ 0.50–1.50 pt round-turn
- Build from 1-minute or ticks, not daily bars

## Objective function (do not maximize net profit)

Reject a parameter set unless **all** hold on a 2026 holdout:

1. Max closed-trade drawdown < 60% of the firm trail
2. Worst session loss ≤ coded daily stop
3. Trades / session between 0 and 5
4. Best session < 40% of total holdout profit (consistency rehearsal)
5. Profit factor > 1.2 after fees/slippage

## Walk-forward

Quantower’s optimizer is not a full walk-forward product. Do folds yourself:

- Train 6 months, test 2 months, roll
- Freeze parameters that only work in one fold
- Keep Opening Range at 15 minutes unless a fold is dramatically better at 30

## What curve-fit looks like

- 15+ sensitive parameters
- Works only with zero slippage
- 20+ trades/day
- All profit from one FOMC Thursday

Delete that version. The risk overlay is the product; the signal is allowed to be average.
