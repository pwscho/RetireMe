using System;
using System.Collections.Generic;
using System.Linq;

namespace RetireMe.Core.Engine
{
    public class RebalancingEngine
    {
        public void Rebalance(
            List<Account> accounts,
            Scenario scenario)
        {
            foreach (var bucket in new[] { "Tax Free", "Tax Deferred", "Taxable" })
            {
                var eq = accounts.FirstOrDefault(a =>
                    a.TaxBucket.Trim() == bucket &&
                    a.AssetClass.Trim() == "Equities");

                var bo = accounts.FirstOrDefault(a =>
                    a.TaxBucket.Trim() == bucket &&
                    a.AssetClass.Trim() == "Bonds");

                if (eq == null || bo == null)
                    continue;

                decimal total = eq.Value + bo.Value;
                if (total <= 0)
                    continue;

                decimal targetEquity = total * scenario.TargetEquityPercentage;
                decimal delta = targetEquity - eq.Value;

                eq.Value += delta;
                bo.Value -= delta;
            }
        }
    }
}

