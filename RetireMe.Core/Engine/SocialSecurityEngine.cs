using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RetireMe.Core.Engine
{
    public class SocialSecurityEngine
    {
        public decimal CalculateBenefits(SocialSecuritySettings settings)
        {
            decimal fraMonthly = settings.BenefitAtFullRetirementAge;
            decimal fraAnnual = fraMonthly * 12m;

            int ageDiff = settings.ClaimAge - 67;

            decimal adjustment = ageDiff switch
            {
                > 0 => 0.08m * ageDiff,
                < 0 => -0.06m * Math.Abs(ageDiff),
                _ => 0m
            };

            decimal adjustedAnnual = fraAnnual * (1 + adjustment);
            Debug.Print(fraAnnual + " / " + adjustedAnnual);
            return Math.Max(0, adjustedAnnual);
        }


        public void DepositSocialSecurityAndIncome(
            int year,
            Scenario scenario,
            SimulationResult result,
            List<Account> workingAccounts,
            SocialSecuritySettings primary,
            SocialSecuritySettings? spouse,
            Func<Guid, int, int> getAgeForOwner,
            TaxYearAccumulator tax)
        {
            // Primary SS
            if (result.PrimarySocialSecurity > 0)
            {
                int primaryAge = primary.CurrentAge + year;

                if (primaryAge >= primary.ClaimAge && primaryAge <= primary.DeathAge)
                {
                    var acct = workingAccounts.First(a =>
                        a.OwnerId == primary.UserId &&
                        a.TaxBucket.Trim() == "Tax Free" &&
                        a.AssetClass.Trim() == "Cash");

                    acct.Value += result.PrimarySocialSecurity;
                    result.SocialSecurityByYear[year] += result.PrimarySocialSecurity;
                }
            }

            // Income Streams
            foreach (var income in scenario.IncomeStreams)
            {
                int ownerAge = getAgeForOwner(income.OwnerId, year);

                if (ownerAge >= income.StartAge && ownerAge <= income.EndAge)
                {
                    var acct = workingAccounts.First(a =>
                        a.OwnerId == income.OwnerId &&
                        a.TaxBucket.Trim() == "Tax Free" &&
                        a.AssetClass.Trim() == "Cash");

                    acct.Value += income.AnnualAmount;

                    // UI reporting
                    result.IncomeByYear[year] += income.AnnualAmount;

                    // Taxable portion
                    if (income.IsTaxable)
                    {
                        result.OrdinaryIncomeByYear[year] += income.AnnualAmount;
                        tax.OrdinaryIncome += income.AnnualAmount;
                    }
                }
            }

            // Spouse SS
            if (spouse != null && result.SpouseSocialSecurity.HasValue)
            {
                int spouseAge = spouse.CurrentAge + year;

                if (spouseAge >= spouse.ClaimAge && spouseAge <= spouse.DeathAge)
                {
                    var acct = workingAccounts.First(a =>
                        a.OwnerId == spouse.UserId &&
                        a.TaxBucket.Trim() == "Tax Free" &&
                        a.AssetClass.Trim() == "Cash");

                    acct.Value += result.SpouseSocialSecurity.Value;
                    result.SocialSecurityByYear[year] += result.SpouseSocialSecurity.Value;
                }
            }
        }
    }
}
