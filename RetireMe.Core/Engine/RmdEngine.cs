using System;
using System.Collections.Generic;
using System.Linq;

namespace RetireMe.Core.Engine
{
    public class RmdEngine
    {
        public void ApplyRmdsForYear(
            int yearIndex,
            List<Account> workingAccounts,
            List<AccountProjection> projections,
            SimulationResult result,
            Dictionary<Guid, decimal> deferredWithdrawnThisYear,
            RmdPolicy rmdPolicy,
            SocialSecuritySettings primary,
            SocialSecuritySettings? spouse)
        {
            if (rmdPolicy == null)
                return;

            // Primary
            int primaryAge = primary.CurrentAge + yearIndex;
            ApplyRmdForOwner(
                ownerId: primary.UserId,
                ownerAgeThisYear: primaryAge,
                yearIndex: yearIndex,
                workingAccounts: workingAccounts,
                projections: projections,
                result: result,
                deferredWithdrawnThisYear: deferredWithdrawnThisYear,
                policy: rmdPolicy,
                deathAge: primary.DeathAge);

            // Spouse
            if (spouse != null)
            {
                int spouseAge = spouse.CurrentAge + yearIndex;
                ApplyRmdForOwner(
                    ownerId: spouse.UserId,
                    ownerAgeThisYear: spouseAge,
                    yearIndex: yearIndex,
                    workingAccounts: workingAccounts,
                    projections: projections,
                    result: result,
                    deferredWithdrawnThisYear: deferredWithdrawnThisYear,
                    policy: rmdPolicy,
                    deathAge: spouse.DeathAge);
            }
        }


        private void ApplyRmdForOwner(
            Guid ownerId,
            int ownerAgeThisYear,
            int yearIndex,
            List<Account> workingAccounts,
            List<AccountProjection> projections,
            SimulationResult result,
            Dictionary<Guid, decimal> deferredWithdrawnThisYear,
            RmdPolicy policy,
            int deathAge)
        {
            if (policy == null)
                return;

            if (ownerAgeThisYear < policy.RmdStartAge || ownerAgeThisYear > deathAge)
                return;

            if (!policy.LifeExpectancyDivisors.TryGetValue(ownerAgeThisYear, out var divisor) ||
                divisor <= 0m)
                return;

            int priorYear = yearIndex - 1;
            if (priorYear < 0)
                return;

            // Owner's tax-deferred working accounts
            var deferredAccounts = workingAccounts
                .Select((acct, index) => new { acct, index })
                .Where(x => x.acct.OwnerId == ownerId && x.acct.TaxBucket.Trim() == "Tax Deferred")
                .ToList();

            if (deferredAccounts.Count == 0)
                return;

            // Total prior-year balance
            decimal totalPriorYearBalance = 0m;

            foreach (var x in deferredAccounts)
            {
                var proj = projections[x.index];
                if (proj.ValuesByYear.Count > priorYear)
                    totalPriorYearBalance += proj.ValuesByYear[priorYear];
            }

            if (totalPriorYearBalance <= 0m)
                return;

            // Required RMD
            decimal rmdAmount = totalPriorYearBalance / divisor;

            if (rmdAmount <= 0m)
                return;

            // Already withdrawn this year
            deferredWithdrawnThisYear.TryGetValue(ownerId, out decimal alreadyWithdrawn);

            decimal remainingRmd = Math.Max(0m, rmdAmount - alreadyWithdrawn);

            if (remainingRmd <= 0m)
                return;

            // Destination taxable cash
            var taxableCash = workingAccounts.FirstOrDefault(a =>
                a.OwnerId == ownerId &&
                a.TaxBucket.Trim() == "Taxable" &&
                a.AssetClass.Trim() == "Cash");

            if (taxableCash == null)
                return;

            decimal remaining = remainingRmd;

            // Withdraw proportionally
            foreach (var x in deferredAccounts)
            {
                if (remaining <= 0m)
                    break;

                var acct = x.acct;
                var proj = projections[x.index];

                if (proj.ValuesByYear.Count <= priorYear)
                    continue;

                decimal priorBalance = proj.ValuesByYear[priorYear];
                if (priorBalance <= 0m)
                    continue;

                decimal share = remainingRmd * (priorBalance / totalPriorYearBalance);
                decimal toWithdraw = Math.Min(share, remaining);

                acct.Value -= toWithdraw;
                taxableCash.Value += toWithdraw;

                remaining -= toWithdraw;
            }

            decimal executedRmd = remainingRmd - remaining;

            if (executedRmd <= 0m)
                return;

            // Add to income
            result.OrdinaryIncomeByYear[yearIndex] += executedRmd;
            result.RmdsByYear[yearIndex] += executedRmd;
        }
    }
}
