using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace RetireMe.Core
{
    // ============================================================
    // GUARDRAIL SETTINGS
    // ============================================================
    public class GuardrailSettings
    {
        // Baseline Portfolio Value (used for guardrail calculations)
        public decimal InitialPortfolioValue { get; set; } = 0m;

        // Lower guardrail
        public decimal LowerTrigger { get; set; } = 0.90m;          // 90% of initial portfolio
        public decimal LowerCut { get; set; } = -0.02m;             // -2% inflation
        public decimal MinSpendingFloor { get; set; } = 0.90m;      // 90% of initial spending

        // Upper guardrail
        public decimal UpperTrigger { get; set; } = 1.10m;          // 110% of initial portfolio
        public decimal UpperBonus { get; set; } = 0.01m;            // +1% bonus inflation

        public GuardrailSettings DeepCopy()
        {
            return new GuardrailSettings
            {
                InitialPortfolioValue = this.InitialPortfolioValue,
                LowerTrigger = this.LowerTrigger,
                LowerCut = this.LowerCut,
                MinSpendingFloor = this.MinSpendingFloor,
                UpperTrigger = this.UpperTrigger,
                UpperBonus = this.UpperBonus
            };
        }
    }

    // ============================================================
    // SCENARIO
    // ============================================================
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

        // Base year for tax policy lookup
        public int BaseYear { get; set; } = DateTime.Now.Year;

        // RMD policy (user‑editable)
        [JsonIgnore]
        public RmdPolicy RmdPolicy { get; set; }

        // ============================================================
        // NEW: Guardrails + Initial Portfolio Value
        // ============================================================
        public GuardrailSettings Guardrails { get; set; } = new GuardrailSettings();

        public decimal InitialPortfolioValue { get; set; } = 0m;

        // ============================================================
        // Deep Copy
        // ============================================================
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
                WithdrawalAdjustmentRate = this.WithdrawalAdjustmentRate,

                RebalanceAnnually = this.RebalanceAnnually,
                TargetEquityPercentage = this.TargetEquityPercentage,
                TargetBondPercentage = this.TargetBondPercentage,

                BaseYear = this.BaseYear,

                RmdPolicy = this.RmdPolicy, // if mutable, consider cloning

                InitialPortfolioValue = this.InitialPortfolioValue,

                Guardrails = this.Guardrails.DeepCopy(),

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



