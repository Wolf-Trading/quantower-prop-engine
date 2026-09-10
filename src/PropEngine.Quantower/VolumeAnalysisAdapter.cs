using System;
using System.Collections.Generic;
using PropEngine.Core.Market;
using TradingPlatform.BusinessLayer;

namespace PropEngine.Quantower
{
    public sealed class VolumeAnalysisAdapter
    {
        private IVolumeAnalysisCalculationTask _task;
        private VolumeProfileLevels _last;
        private DateTime _requestedTo;

        public VolumeProfileLevels Last => _last;
        public bool HasTickProfile => _last is { Valid: true, FromTicks: true };

        public void RequestSessionProfile(Symbol symbol, DateTime fromUtc, DateTime toUtc)
        {
            if (symbol == null) return;
            try
            {
                _requestedTo = toUtc;
                _task = Core.Instance.VolumeAnalysisManager.CalculateProfile(symbol, fromUtc, toUtc);
            }
            catch { _task = null; }
        }

        public bool TryGetCompleted(double tickSize, out VolumeProfileLevels profile)
        {
            profile = _last;
            try
            {
                if (_task == null) return _last is { Valid: true };
                VolumeAnalysisData data = null;
                var taskType = _task.GetType();
                var dataProp = taskType.GetProperty("Data") ?? taskType.GetProperty("Result") ?? taskType.GetProperty("VolumeAnalysisData");
                if (dataProp != null) data = dataProp.GetValue(_task) as VolumeAnalysisData;
                if (data?.PriceLevels == null || data.PriceLevels.Count == 0) return _last is { Valid: true };
                var levels = new List<PriceLevelVolume>(data.PriceLevels.Count);
                foreach (var kv in data.PriceLevels)
                {
                    var item = kv.Value;
                    if (item == null) continue;
                    levels.Add(new PriceLevelVolume(kv.Key, item.Volume, item.BuyVolume, item.SellVolume));
                }
                _last = VolumeProfileSnapshot.BuildFromLevels(levels, tickSize, 0.70, fromTicks: true);
                profile = _last;
                return _last.Valid;
            }
            catch { return _last is { Valid: true }; }
        }

        public void RefreshIfStale(Symbol symbol, DateTime fromUtc, DateTime toUtc, TimeSpan minGap)
        {
            if (toUtc - _requestedTo >= minGap)
                RequestSessionProfile(symbol, fromUtc, toUtc);
        }
    }
}
