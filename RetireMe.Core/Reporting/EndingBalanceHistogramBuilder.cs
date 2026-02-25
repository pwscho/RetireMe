using System;
using System.Collections.Generic;
using System.Linq;

namespace RetireMe.Core.Reporting
{
    public class EndingBalanceHistogramBuilder
    {
        // ------------------------------------------------------------
        // Build histogram from List<ResultRow> (original version)
        // ------------------------------------------------------------
        public List<HistogramBin> Build(List<ResultRow> monteCarloResults, int binCount)
        {
            if (monteCarloResults == null || monteCarloResults.Count == 0)
                return new List<HistogramBin>();

            // Extract ending balances per simulation
            var endings = monteCarloResults
                .GroupBy(r => r.SimNo)
                .Select(sim => sim.Last().EndingBalance)
                .ToList();

            return Build(endings, binCount);
        }

        // ------------------------------------------------------------
        // Build histogram from List<decimal> (needed for winsorization)
        // ------------------------------------------------------------
        public List<HistogramBin> Build(List<decimal> values, int binCount)
        {
            if (values == null || values.Count == 0)
                return new List<HistogramBin>();

            decimal min = values.Min();
            decimal max = values.Max();

            if (min == max)
            {
                return new List<HistogramBin>
                {
                    new HistogramBin
                    {
                        BinStart = min,
                        BinEnd = min,
                        Count = values.Count
                    }
                };
            }

            decimal binSize = (max - min) / binCount;

            var bins = new List<HistogramBin>();

            for (int i = 0; i < binCount; i++)
            {
                bins.Add(new HistogramBin
                {
                    BinStart = min + i * binSize,
                    BinEnd = min + (i + 1) * binSize,
                    Count = 0
                });
            }

            foreach (var v in values)
            {
                int index = (int)((v - min) / binSize);
                if (index == binCount) index--; // edge case
                bins[index].Count++;
            }

            return bins;
        }
    }
}

