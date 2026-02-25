namespace RetireMe.Core
{
    public class ResultRow
    {
        public int SimNo { get; set; }
        public int Year { get; set; }
        public int Age { get; set; }
        
        
        public string AgeDisplay => SpouseAge > 0
            ? $"{Age}/{SpouseAge}"
            : $"{Age}";

        public int SpouseAge { get; set; }
        public int Iteration { get; set; }

        public decimal StartingBalance { get; set; }
        public decimal EndingBalance { get; set; }

        public decimal Withdrawal { get; set; }
        public decimal SocialSecurityIncome { get; set; }
        public decimal Income { get; set; }
        public decimal InvestmentReturn { get; set; }

        public decimal Magi { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal OrdinaryIncome { get; set; }
        public decimal CapitalGains { get; set; }

        public decimal TaxesDue { get; set; }
        public decimal TaxesPaid { get; set; }
        public decimal IrmaaPaid { get; set; }

        public decimal Rmds { get; set; }
        public decimal Conversions { get; set; }

        public decimal AccountWithdrawals { get; set; }     
        public decimal PercentWithdrawals { get; set; }

        public decimal BaseSpending { get; set; }
        public decimal AddedSpending { get; set; }
    }
}

