using RetireMe.Core.Strategies;

namespace RetireMe.Core
    
{
    public class RothConversionWithdrawalStrategy : IRothConversionWithdrawalStrategy
    {
        private readonly IWithdrawalStrategy _priorityStrategy;

        public RothConversionWithdrawalStrategy(IWithdrawalStrategy priorityStrategy)
        {
            _priorityStrategy = priorityStrategy;
        }

        public decimal Convert(
            Guid ownerId,
            decimal requestedAmount,
            List<Account> workingAccounts,
            TaxYearAccumulator tax,
            SimulationResult result,
            int yearIndex)
        {
            // Only tax-deferred accounts for this owner
            var eligibleAccounts = workingAccounts
                .Where(a => a.OwnerId == ownerId &&
                            a.TaxBucket.Trim() == "Tax Deferred")
                .ToList();

            if (!eligibleAccounts.Any())
                return 0m;

            // Use the existing priority ordering
            var ordered = eligibleAccounts
                .OrderBy(a => a.Priority)
                .ThenBy(a => a.TaxBucket)
                .ThenBy(a => a.AssetClass)
                .ToList();

            decimal remaining = requestedAmount;
            decimal actualConverted = 0m;

            foreach (var acct in ordered)
            {
                if (remaining <= 0)
                    break;

                decimal available = acct.Value;
                if (available <= 0)
                    continue;

                decimal toWithdraw = Math.Min(available, remaining);

                // Withdraw from tax-deferred
                acct.Value -= toWithdraw;

                // Find matching Roth account
                var rothAcct = workingAccounts.FirstOrDefault(a =>
                    a.OwnerId == ownerId &&
                    a.TaxBucket.Trim() == "Tax Free" &&
                    a.AssetClass.Trim() == acct.AssetClass.Trim());

                if (rothAcct != null)
                    rothAcct.Value += toWithdraw;

                // ⭐ Conversion is taxable as ordinary income
                tax.OrdinaryIncome += toWithdraw;
                //result.OrdinaryIncomeByYear[yearIndex] += toWithdraw;

                actualConverted += toWithdraw;
                remaining -= toWithdraw;
            }

            return actualConverted;
        }
    }
}
