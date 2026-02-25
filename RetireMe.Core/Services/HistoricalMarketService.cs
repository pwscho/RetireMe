using RetireMe.Core.MarketData;
using System;

namespace RetireMe.Core.Services
{
    public class HistoricalMarketService : IMarketService
    {
        private readonly MarketHistory _history;
        private readonly int _startIndex;

        public HistoricalMarketService(MarketHistory history, int startIndex)
        {
            _history = history;
            _startIndex = startIndex;
        }

        public decimal GetEquityReturnForYear(int yearIndex)
        {
            int index = _startIndex + yearIndex;
            var y = _history.Years[index];
            return y.EquityReturn;
        }

        public decimal GetBondReturnForYear(int yearIndex)
        {
            int index = _startIndex + yearIndex;
            return _history.Years[index].BondReturn;
        }

        public decimal GetInflationForYear(int yearIndex)
        {
            int index = _startIndex + yearIndex;
            return _history.Years[index].InflationRate;
        }
    }
}
