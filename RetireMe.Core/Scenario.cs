using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RetireMe.Core
{
    public class Scenario
    {
        public string Name { get; set; } = "New Scenario";

        public int YearsToCalculate { get; set; } = 30;

        public int RetirementAge { get; set; } = 65;

        public decimal AnnualSpending { get; set; }

        public string SpendingStrategy { get; set; } = "Fixed";

        public decimal InflationRate { get; set; } = 0.02m;

        public decimal TaxesFromLastYear { get; set; } = 0;

        public decimal WithdrawalAdjustmentRate { get; set; } = 0.02m;

        // Rebalancing settings
        public bool RebalanceAnnually { get; set; } = false;

        public decimal TargetEquityPercentage { get; set; } = 0.70m;

        public decimal TargetBondPercentage { get; set; } = 0.30m;

        // Additional streams (user‑defined)
        public List<IncomeStream> IncomeStreams { get; set; } = new();
        public List<WithdrawalStream> WithdrawalStreams { get; set; } = new();
        public List<RothConversionStream> RothConversions { get; set; } = new();

        // Base year for tax policy lookup (but tax data is NOT stored here)
        public int BaseYear { get; set; } = DateTime.Now.Year;

        // RMD policy (user‑editable, so this stays)
        [JsonIgnore]
        public RmdPolicy RmdPolicy { get; set; }

        public Scenario DeepCopy()
        {
            return new Scenario
            {
                Name = this.Name,
                YearsToCalculate = this.YearsToCalculate,
                RetirementAge = this.RetirementAge,
                AnnualSpending = this.AnnualSpending,
                InflationRate = this.InflationRate,
                TaxesFromLastYear = this.TaxesFromLastYear,
                WithdrawalAdjustmentRate = this.WithdrawalAdjustmentRate,  // not used

                

                RebalanceAnnually = this.RebalanceAnnually,
                TargetEquityPercentage = this.TargetEquityPercentage,
                TargetBondPercentage = this.TargetBondPercentage,

                BaseYear = this.BaseYear,

                RmdPolicy = this.RmdPolicy, // if mutable, consider cloning

                IncomeStreams = this.IncomeStreams
                    .Select(i => new IncomeStream
                    {
                        Name = i.Name,
                        OwnerId = i.OwnerId,
                        StartAge = i.StartAge,
                        EndAge = i.EndAge,
                        AnnualAmount = i.AnnualAmount,
                        IsTaxable = i.IsTaxable
                    })
                    .ToList(),

                WithdrawalStreams = this.WithdrawalStreams
                    .Select(w => new WithdrawalStream
                    {
                        Name = w.Name,
                        OwnerId = w.OwnerId,
                        StartAge = w.StartAge,
                        EndAge = w.EndAge,
                        AnnualAmount = w.AnnualAmount
                    })
                    .ToList(),

                RothConversions = this.RothConversions
                    .Select(r => new RothConversionStream
                    {
                        Name = r.Name,
                        OwnerId = r.OwnerId,
                        StartAge = r.StartAge,
                        EndAge = r.EndAge,
                        AnnualAmount = r.AnnualAmount
                    })
                    .ToList()

            };
        }
    }
}



