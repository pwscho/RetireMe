namespace RetireMe.Core.Taxes
{
    public sealed class TaxComputationInput
    {
        public int Year { get; init; }
        public FilingStatus FilingStatus { get; init; }

        public decimal OrdinaryIncome { get; init; }
        public decimal CapitalGains { get; init; }
        public decimal SocialSecurityBenefits { get; init; }
    }

    public sealed class TaxComputationResult
    {
        // Total federal tax including IRMAA
        public decimal FederalTax { get; init; }
        public decimal Magi { get; init; }

        // NEW: Annual IRMAA surcharge (added to FederalTax)
        public decimal IrmaaSurcharge { get; init; }

        public decimal TaxableOrdinaryIncome { get; init; }
        public decimal TaxableCapitalGains { get; init; }
        public decimal TaxableSocialSecurity { get; init; }

        // Optional convenience property
        public decimal TotalFederalTax => FederalTax;
    }
}

