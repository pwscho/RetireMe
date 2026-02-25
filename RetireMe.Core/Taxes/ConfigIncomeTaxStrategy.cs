using System.Linq;
using System.Collections.Generic;

namespace RetireMe.Core.Taxes
{
    public sealed class ConfigIncomeTaxStrategy : IIncomeTaxStrategy
    {
        public TaxComputationResult Compute(TaxComputationInput input, TaxPolicy policy)
        {
            var table = input.FilingStatus == FilingStatus.Single
                ? policy.Single
                : policy.MarriedFilingJointly;

            // 1) Taxable Social Security
            var taxableSs = SocialSecurityTaxHelper.ComputeTaxableSocialSecurity(
                input.SocialSecurityBenefits,
                input.OrdinaryIncome,
                input.FilingStatus,
                policy);

            // 2) Ordinary taxable income
            var ordinaryBase = input.OrdinaryIncome + taxableSs - table.StandardDeduction;
            if (ordinaryBase < 0)
                ordinaryBase = 0;

            // 3) Ordinary tax
            var ordinaryTax = ApplyBrackets(ordinaryBase, table.OrdinaryBrackets);

            // 4) Capital gains tax (stacked on top of ordinary taxable income)
            var capitalGainsTax = ApplyCapitalGainsTax(
                ordinaryBase,
                input.CapitalGains,
                table.CapitalGainsBrackets);

            // 5) IRMAA (based on MAGI and IRMAA brackets)
            // Simple MAGI approximation: ordinary income + capital gains + full SS benefits
            var magi = input.OrdinaryIncome + input.CapitalGains + input.SocialSecurityBenefits;
            var irmaaAnnual = ComputeIrmaaAnnual(magi, table.IrmaaBrackets);

            var totalFederalTax = ordinaryTax + capitalGainsTax + irmaaAnnual;

            return new TaxComputationResult
            {
                FederalTax = totalFederalTax,
                TaxableOrdinaryIncome = ordinaryBase,
                TaxableCapitalGains = input.CapitalGains,
                TaxableSocialSecurity = taxableSs,
                IrmaaSurcharge = irmaaAnnual,
                Magi = magi
            };
        }

        private static decimal ApplyBrackets(decimal amount, List<TaxBracket> brackets)
        {
            decimal tax = 0m;
            decimal remaining = amount;
            decimal lower = 0m;

            foreach (var bracket in brackets.OrderBy(b => b.Threshold))
            {
                if (remaining <= 0)
                    break;

                var span = System.Math.Min(remaining, bracket.Threshold - lower);
                if (span > 0)
                {
                    tax += span * bracket.Rate;
                    remaining -= span;
                    lower = bracket.Threshold;
                }
            }

            // If income exceeds last bracket threshold
            if (remaining > 0 && brackets.Count > 0)
            {
                var lastRate = brackets.OrderBy(b => b.Threshold).Last().Rate;
                tax += remaining * lastRate;
            }

            return tax;
        }

        private static decimal ApplyCapitalGainsTax(
            decimal ordinaryTaxable,
            decimal capitalGains,
            List<TaxBracket> cgBrackets)
        {
            if (capitalGains <= 0)
                return 0m;

            decimal tax = 0m;
            decimal remaining = capitalGains;
            decimal incomeSoFar = ordinaryTaxable;

            foreach (var bracket in cgBrackets.OrderBy(b => b.Threshold))
            {
                if (remaining <= 0)
                    break;

                var roomInBracket = bracket.Threshold - incomeSoFar;
                if (roomInBracket <= 0)
                {
                    incomeSoFar = bracket.Threshold;
                    continue;
                }

                var span = System.Math.Min(remaining, roomInBracket);
                tax += span * bracket.Rate;

                remaining -= span;
                incomeSoFar += span;
            }

            // If gains exceed last bracket threshold
            if (remaining > 0 && cgBrackets.Count > 0)
            {
                var lastRate = cgBrackets.OrderBy(b => b.Threshold).Last().Rate;
                tax += remaining * lastRate;
            }

            return tax;
        }

        private static decimal ComputeIrmaaAnnual(decimal magi, List<IrmaaBracket> irmaaBrackets)
        {
            if (irmaaBrackets == null || irmaaBrackets.Count == 0)
                return 0m;

            decimal monthly = 0m;

            foreach (var bracket in irmaaBrackets.OrderBy(b => b.Threshold))
            {
                monthly = bracket.MonthlySurcharge;

                if (magi <= bracket.Threshold)
                    break;
            }

            return monthly * 12m;
        }
    }
}

