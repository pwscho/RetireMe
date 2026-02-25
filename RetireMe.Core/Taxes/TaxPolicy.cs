namespace RetireMe.Core.Taxes
{
    public sealed class TaxPolicy
    {
        public int Year { get; set; }

        // Filing status tables now include IRMAA brackets (added in FilingStatusTaxTable)
        public FilingStatusTaxTable Single { get; set; } = new();
        public FilingStatusTaxTable MarriedFilingJointly { get; set; } = new();

        // Social Security taxation thresholds
        public decimal SingleSsBaseThreshold { get; set; }
        public decimal SingleSsSecondThreshold { get; set; }

        public decimal MarriedSsBaseThreshold { get; set; }
        public decimal MarriedSsSecondThreshold { get; set; }
    }
}
