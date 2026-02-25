using RetireMe.Core.Engine;
using RetireMe.Core.MarketData;
using RetireMe.Core;
using RetireMe.Core.Services;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace RetireMe.Core.Simulation
{
    public class SimulationRunner
    {
        // ------------------------------------------------------------
        // FIXED SIMULATION
        // ------------------------------------------------------------
        public List<ResultRow> RunFixed(
            Scenario scenario,
            List<Account> accounts,
            SocialSecuritySettings primary,
            SocialSecuritySettings? spouse)
        {
            var market = new FixedMarketService();

            var engine = new ComputeEngine(
                scenario,
                accounts.Select(a => a.DeepCopy()).ToList(),
                market,
                primary,
                spouse);

            var output = engine.Run(simNumber: 1, "fixed");
            return output.Results;
        }

        // ------------------------------------------------------------
        // HISTORICAL SIMULATION
        // ------------------------------------------------------------
        public List<ResultRow> RunHistorical(
            Scenario scenario,
            List<Account> accounts,
            SocialSecuritySettings primary,
            SocialSecuritySettings? spouse,
            MarketHistory history)
        {
            var list = new List<ResultRow>();

            int yearsNeeded = scenario.YearsToCalculate;
            int maxStartIndex = history.Years.Count - yearsNeeded;

            int simNumber = 1;

            for (int startIndex = 0; startIndex <= maxStartIndex; startIndex++)
            {
                var market = new HistoricalMarketService(history, startIndex);
                
              
                var engine = new ComputeEngine(
                    scenario,
                    accounts.Select(a => a.DeepCopy()).ToList(),
                    market,
                    primary,
                    spouse);

                var output = engine.Run(simNumber, "historical");
                list.AddRange(output.Results);

                simNumber++;
            }
            int successes = list.GroupBy(r => r.SimNo).Count(g => g.OrderBy(r => r.Iteration).Last().EndingBalance > 0); Debug.Print($"Successes: {successes}");
            return list;
        }

        // ------------------------------------------------------------
        // MONTE CARLO SIMULATION
        // ------------------------------------------------------------
        public List<ResultRow> RunMonteCarlo(
            Scenario scenario,
            List<Account> accounts,
            SocialSecuritySettings primary,
            SocialSecuritySettings? spouse,
            MarketHistory history,
            int simulations)
        {
            var list = new List<ResultRow>();

            for (int sim = 1; sim <= simulations; sim++)
            {
                var market = new MonteCarloMarketService(history);

                var engine = new ComputeEngine(
                    scenario,
                    accounts.Select(a => a.DeepCopy()).ToList(),
                    market,
                    primary,
                    spouse);

                var output = engine.Run(sim, "random");
                list.AddRange(output.Results);
            }

           // int successes = list.GroupBy(r => r.SimNo).Count(g => g.OrderBy(r => r.Iteration).Last().EndingBalance > 0); Debug.Print($"Successes: {successes}");
            return list;
        }
    }
}

