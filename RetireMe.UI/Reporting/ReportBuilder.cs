using RetireMe.Core;
using RetireMe.Core.MarketData;
using RetireMe.Core.Simulation;
using System.Collections.Generic;

namespace RetireMe.UI.Reporting
{
    public class ReportBuilder
    {
        public ReportData BuildReport(
            Scenario scenario,
            List<Account> accounts,
            SocialSecuritySettings primary,
            SocialSecuritySettings? spouse,
            MarketHistory history,
            int monteCarloCount)
        {
            var data = new ReportData(scenario);

            var runner = new SimulationRunner();

            // ------------------------------------------------------------
            // FIXED
            // ------------------------------------------------------------
            var fixedResults = runner.RunFixed(
                scenario,
                accounts,
                primary,
                spouse);

            data.FixedResults = fixedResults;

            // ------------------------------------------------------------
            // HISTORICAL
            // ------------------------------------------------------------
            var historicalResults = runner.RunHistorical(
                scenario,
                accounts,
                primary,
                spouse,
                history);

            data.HistoricalResults = historicalResults;

            // ------------------------------------------------------------
            // MONTE CARLO
            // ------------------------------------------------------------
            var monteCarloResults = runner.RunMonteCarlo(
                scenario,
                accounts,
                primary,
                spouse,
                history,
                monteCarloCount);

            data.MonteCarloResults = monteCarloResults;

            return data;
        }
    }
}


