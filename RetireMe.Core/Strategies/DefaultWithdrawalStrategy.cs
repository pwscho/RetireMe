using RetireMe.Core;
using RetireMe.Core.Strategies;

public class DefaultWithdrawalStrategy : IWithdrawalStrategy
{
    public IEnumerable<Account> GetWithdrawalOrder(
        Scenario scenario,
        List<Account> accounts)
    {
        return accounts
            .OrderBy(a => a.Priority)
            .ThenBy(a => GetBucketRank(a.TaxBucket))
            .ToList();
    }

    private int GetBucketRank(string bucket)
    {
        switch (bucket.Trim())
        {
            case "Taxable": return 1;
            case "Tax Deferred": return 2;
            case "Tax Free": return 3;
            default: return 99;
        }
    }
}
