using System.Collections.Generic;
using RetireMe.Core;

namespace RetireMe.UI.Reporting
{
    public class ReportData
    {
        public Scenario Scenario { get; set; }

        // These are the year‑level results from the flat engine
        public List<ResultRow> FixedResults { get; set; } = new();
        public List<ResultRow> HistoricalResults { get; set; } = new();
        public List<ResultRow> MonteCarloResults { get; set; } = new();

        public ReportData(Scenario scenario)
        {
            Scenario = scenario;
        }
    }
}

