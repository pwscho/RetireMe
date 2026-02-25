namespace RetireMe.Core.Taxes
{
    public static class SocialSecurityTaxHelper
    {
        public static decimal ComputeTaxableSocialSecurity(
            decimal ssBenefits,
            decimal ordinaryIncomeExcludingSs,
            FilingStatus filingStatus,
            TaxPolicy policy)
        {
            if (ssBenefits <= 0) return 0m;

            decimal baseThreshold;
            decimal secondThreshold;

            if (filingStatus == FilingStatus.Single)
            {
                baseThreshold = policy.SingleSsBaseThreshold;
                secondThreshold = policy.SingleSsSecondThreshold;
            }
            else
            {
                baseThreshold = policy.MarriedSsBaseThreshold;
                secondThreshold = policy.MarriedSsSecondThreshold;
            }

            decimal provisional = ordinaryIncomeExcludingSs + 0.5m * ssBenefits;
            decimal taxable;

            if (provisional <= baseThreshold)
            {
                taxable = 0m;
            }
            else if (provisional <= secondThreshold)
            {
                taxable = 0.5m * (provisional - baseThreshold);
            }
            else
            {
                taxable = 0.85m * (provisional - secondThreshold)
                          + 0.5m * (secondThreshold - baseThreshold);

                taxable = Math.Min(taxable, 0.85m * ssBenefits);
            }

            return taxable;
        }
    }
}
