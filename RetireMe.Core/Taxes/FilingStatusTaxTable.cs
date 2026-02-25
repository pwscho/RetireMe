using System.Collections.Generic;

namespace RetireMe.Core.Taxes
{
    public sealed class FilingStatusTaxTable
    {
        public decimal StandardDeduction { get; set; }

        public List<TaxBracket> OrdinaryBrackets { get; set; } = new();
        public List<TaxBracket> CapitalGainsBrackets { get; set; } = new();

        // NEW: IRMAA surcharge brackets (MAGI-based)
        public List<IrmaaBracket> IrmaaBrackets { get; set; } = new();
    }

    // NEW: IRMAA bracket definition
    public sealed class IrmaaBracket
    {
        public decimal Threshold { get; set; }
        public decimal MonthlySurcharge { get; set; }
    }
}
