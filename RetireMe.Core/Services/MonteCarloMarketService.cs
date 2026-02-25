using RetireMe.Core.MarketData;
using System;
using System.Collections.Generic;

namespace RetireMe.Core.Services
{
    public class MonteCarloMarketService : IMarketService
    {
        private readonly List<MarketYear> _years;
        private readonly Random _rng;

        public MonteCarloMarketService(MarketHistory history)
        {
            _years = history.Years;
            // unique seed per simulation
            _rng = new Random(Guid.NewGuid().GetHashCode());
        }

        public decimal GetEquityReturnForYear(int yearIndex)
        {
            var y = _years[_rng.Next(_years.Count)];
            
            return y.EquityReturn;
        }

        public decimal GetBondReturnForYear(int yearIndex)
        {
            var y = _years[_rng.Next(_years.Count)];
            return y.BondReturn;
        }

        public decimal GetInflationForYear(int yearIndex)
        {
            var y = _years[_rng.Next(_years.Count)];
            return y.InflationRate;
        }
    }
}
