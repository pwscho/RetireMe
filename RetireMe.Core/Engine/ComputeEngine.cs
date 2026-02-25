using RetireMe.Core.Services;
using RetireMe.Core.Strategies;
using RetireMe.Core.Taxes;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace RetireMe.Core.Engine
{


    public class ComputeEngine
    {
        private readonly Scenario _scenario;
        private readonly List<Account> _accounts;
        private readonly IMarketService _market;
        private readonly SocialSecuritySettings _primary;
        private readonly SocialSecuritySettings? _spouse;

        private readonly IWithdrawalStrategy _strategy = new DefaultWithdrawalStrategy();
        private readonly IIncomeTaxStrategy _taxStrategy;
        private readonly IRothConversionWithdrawalStrategy _conversionStrategy;

        private readonly GrowthEngine _growth;
        private readonly SpendingEngine _spending;

        private TaxYearAccumulator _tax = new TaxYearAccumulator();
        private decimal _nextYearTaxBill = 0m;

        public ComputeEngine(
            Scenario scenario,
            List<Account> accounts,
            IMarketService market,
            SocialSecuritySettings primary,
            SocialSecuritySettings? spouse,
            IIncomeTaxStrategy? taxStrategy = null)

        {
            _scenario = scenario;
            _accounts = accounts;
            _market = market;
            _primary = primary;
            _spouse = spouse;

            _taxStrategy = taxStrategy ?? new ConfigIncomeTaxStrategy();
            _conversionStrategy = new RothConversionWithdrawalStrategy(_strategy);

            _growth = new GrowthEngine(market);
            _spending = new SpendingEngine(market);

        }



        public SimulationOutput Run(int simNumber, string simulation)
        {
            
            
            int years = _scenario.YearsToCalculate;
            decimal baseSpending = _scenario.AnnualSpending;
            decimal spendingNeed = 0;


            var output = new SimulationOutput();

            // Load RMD policy once
            var rmdPolicy = RmdPolicyLoader.LoadForYear(_scenario.BaseYear);

            // Working accounts are the ONLY balances
            var workingAccounts = _accounts
                .Select(a => a.DeepCopy())
                .ToList();

            decimal nextYearTaxBill = _scenario.TaxesFromLastYear ;

            for (int year = 0; year < years; year++)
            {
                _tax = new TaxYearAccumulator();
                var deferredWithdrawnThisYear = new Dictionary<Guid, decimal>();

                int calendarYear = _scenario.BaseYear + year;

                // -------------------------------
                // Capture starting values (per-account)
                // -------------------------------
                var startingValues = workingAccounts
                    .Select(a => a.Value)
                    .ToList();

                // -------------------------------
                // CORRECT: Capture starting balance BEFORE any withdrawals/taxes/income
                // -------------------------------
                decimal startingBalance = workingAccounts.Sum(a => a.Value);

                // -------------------------------
                // 1. Pay last year's tax bill
                // -------------------------------
                decimal taxesPaid = 0m;
                if (nextYearTaxBill > 0)
                {
                    var taxWithdrawals = WithdrawalEngine.Withdraw(
                        nextYearTaxBill,
                        _strategy,
                        _scenario,
                        workingAccounts,
                        _tax);

                    TrackDeferredWithdrawals(taxWithdrawals, deferredWithdrawnThisYear);
                    taxesPaid = nextYearTaxBill;
                    nextYearTaxBill = 0m;
                }

                // -------------------------------
                // 1b. IRMAA Paid (N+2)
                // -------------------------------
                decimal irmaaPaid = 0m;

                // -------------------------------
                // 2. Deposit SS + Income
                // -------------------------------
                decimal ssIncome = 0m;
                decimal otherIncome = 0m;

                DepositSocialSecurityAndIncome_Flat(
                    year,
                    workingAccounts,
                    ref ssIncome,
                    ref otherIncome);

                // -------------------------------
                // 3. Spending need
                // -------------------------------
                int primaryAge = _primary.CurrentAge + year;

                decimal _inflationrate = _scenario.InflationRate;

                switch (_scenario.SpendingStrategy)
                {
                    case "Fixed":
                        _inflationrate = _scenario.InflationRate;
                        break;
                    case "Historical":
                        if (simulation == "fixed")
                        {
                            _inflationrate = _scenario.InflationRate;
                            break;
                        }
                        _inflationrate = _growth.GetInflation(year);
                        break;

                }

                decimal addedSpending = 0;
                foreach (var w in _scenario.WithdrawalStreams)
                {
                    int ownerAge = GetAgeForOwner(w.OwnerId, year);
                    if (ownerAge >= w.StartAge && ownerAge <= w.EndAge)
                        addedSpending = addedSpending + w.AnnualAmount;
                }
                

                // -------------------------------
                // 4. Withdraw for spending
                // -------------------------------
                decimal withdrawals = 0m;

                spendingNeed = baseSpending + addedSpending;



                if (spendingNeed > 0)
                {
                    var w = WithdrawalEngine.Withdraw(
                        spendingNeed,
                        _strategy,
                        _scenario,
                        workingAccounts,
                        _tax);

                    TrackDeferredWithdrawals(w, deferredWithdrawnThisYear);
                    withdrawals = w.Sum(x => x.Amount);
                    //workingAccounts = w.Item1;

                }



                

                // -------------------------------
                // 4b. RMDs
                // -------------------------------
                decimal rmds = ApplyRmdsForYear_Flat(
                    year,
                    workingAccounts,
                    deferredWithdrawnThisYear,
                    rmdPolicy);

                // -------------------------------
                // 4c. Roth Conversions
                // -------------------------------
                decimal conversions = ApplyRothConversionsForYear_Flat(
                    year,
                    workingAccounts);

                // -------------------------------
                // 4d. Inheritance
                // -------------------------------
                ApplyInheritanceForYear(
                    year,
                    workingAccounts);

                // -------------------------------
                // 5. Growth
                // -------------------------------
                _growth.ApplyGrowth(workingAccounts, year);



                decimal endingBalance = workingAccounts.Sum(a => a.Value);


                decimal investmentReturn = endingBalance - startingBalance
                                           + withdrawals
                                           - ssIncome
                                           - otherIncome
                                           + taxesPaid;

                // -------------------------------
                // 6. Rebalance
                // -------------------------------
                if (_scenario.RebalanceAnnually)
                    RebalanceWorkingAccounts(workingAccounts, _scenario);

                // -------------------------------
                // 7. Compute Taxes
                // -------------------------------
                var filingStatus = FilingStatusHelper.GetFilingStatusForYear(year, _primary, _spouse);
                var taxPolicy = TaxPolicyLoader.LoadForYear(calendarYear);

                var taxInput = new TaxComputationInput
                {
                    Year = calendarYear,
                    FilingStatus = filingStatus,
                    OrdinaryIncome = _tax.OrdinaryIncome,
                    CapitalGains = _tax.CapitalGains,
                    SocialSecurityBenefits = ssIncome
                };

                var taxResult = _taxStrategy.Compute(taxInput, taxPolicy);

                decimal perPersonIrmaa = taxResult.IrmaaSurcharge;
                decimal taxesDue = taxResult.FederalTax;

                nextYearTaxBill = taxesDue;

                // -------------------------------
                // 8. IRMAA Paid (N+2)
                // -------------------------------
                if (year >= 2)
                {
                    irmaaPaid = output.Results[year - 2].IrmaaPaid;

                }



                // -------------------------------
                // 9. Add per-account summaries
                // -------------------------------
                for (int i = 0; i < workingAccounts.Count; i++)
                {
                    var acct = workingAccounts[i];
                    var start = startingValues[i];
                    var end = acct.Value;

                    output.Accounts.Add(new AccountYearSummary
                    {
                        Year = year + 1,
                        Age = _primary.CurrentAge + year,
                        Owner = acct.Owner,
                        TaxBucket = acct.TaxBucket,
                        AssetClass = acct.AssetClass,
                        StartingValue = start,
                        EndingValue = end,
                        InvestmentReturn = end - start
                    });
                }

                // -------------------------------
                // 10. Add ResultRow
                // -------------------------------
                output.Results.Add(new ResultRow
                {
                    SimNo = simNumber,
                    Iteration = year + 1,
                    Year = calendarYear,

                    Age = _primary.CurrentAge + year,
                    SpouseAge = _spouse == null ? 0 : _spouse.CurrentAge + year,
                    

                    StartingBalance = startingBalance,
                    EndingBalance = endingBalance,

                    Withdrawal = withdrawals,
                    SocialSecurityIncome = ssIncome,
                    Income = otherIncome,
                    InvestmentReturn = investmentReturn,

                    TaxableIncome = taxResult.TaxableOrdinaryIncome + taxResult.TaxableCapitalGains,
                    OrdinaryIncome = taxResult.TaxableOrdinaryIncome,
                    CapitalGains = taxResult.TaxableCapitalGains,

                    TaxesDue = taxesDue,
                    TaxesPaid = taxesPaid,
                    IrmaaPaid = perPersonIrmaa,
                    Magi = taxResult.Magi,
                    Rmds = rmds,
                    Conversions = conversions,
                    BaseSpending = baseSpending,
                    AddedSpending = addedSpending,

                    AccountWithdrawals = (withdrawals + taxesPaid) - (ssIncome + otherIncome),

                    PercentWithdrawals = startingBalance == 0 ? 0 : ((withdrawals + taxesPaid) - (ssIncome + otherIncome)) / startingBalance


                }); ;

                baseSpending = baseSpending * (1 + _inflationrate);
            }


            return output;
        }




        private void TrackDeferredWithdrawals(
            List<WithdrawalResult> withdrawals,
            Dictionary<Guid, decimal> deferredWithdrawnThisYear)
        {
            foreach (var w in withdrawals)
            {
                if (w.Bucket != TaxBucket.TaxDeferred)
                    continue;

                var ownerId = w.Account.OwnerId;

                if (!deferredWithdrawnThisYear.TryGetValue(ownerId, out var current))
                    current = 0m;
                
                deferredWithdrawnThisYear[ownerId] = current + w.Amount;

            }
        }

        private decimal ApplyRmdsForYear_Flat(
            int yearIndex,
            List<Account> workingAccounts,
            Dictionary<Guid, decimal> deferredWithdrawnThisYear,
            RmdPolicy policy)
        {
            if (policy == null)
                return 0m;

            decimal total = 0m;

            // Primary
            int primaryAge = _primary.CurrentAge + yearIndex;
            total += ApplyRmdForOwner_Flat(
                _primary.UserId,
                primaryAge,
                yearIndex,
                workingAccounts,
                deferredWithdrawnThisYear,
                policy);

            // Spouse
            if (_spouse != null)
            {
                int spouseAge = _spouse.CurrentAge + yearIndex;
                total += ApplyRmdForOwner_Flat(
                    _spouse.UserId,
                    spouseAge,
                    yearIndex,
                    workingAccounts,
                    deferredWithdrawnThisYear,
                    policy);
            }

            return total;
        }



        private decimal ApplyRmdForOwner_Flat(
            Guid ownerId,
            int ownerAgeThisYear,
            int yearIndex,
            List<Account> workingAccounts,
            Dictionary<Guid, decimal> deferredWithdrawnThisYear,
            RmdPolicy policy)
        {
            // Validate policy
            if (policy == null)
                return 0m;

            // Determine death age
            int deathAge =
                ownerId == _primary.UserId ? _primary.DeathAge :
                (_spouse != null && ownerId == _spouse.UserId ? _spouse.DeathAge : int.MaxValue);

            // Not in RMD window
            if (ownerAgeThisYear < policy.RmdStartAge || ownerAgeThisYear > deathAge)
                return 0m;

            // Get divisor
            if (!policy.LifeExpectancyDivisors.TryGetValue(ownerAgeThisYear, out var divisor) ||
                divisor <= 0m)
                return 0m;

            // Need prior-year balance
            int priorYear = yearIndex - 1;
            if (priorYear < 0)
                return 0m;

            // Owner's tax-deferred accounts
            var deferredAccounts = workingAccounts
                .Where(a =>
                    a.OwnerId == ownerId &&
                    a.TaxBucket.Trim() == "Tax Deferred")
                .ToList();

            if (deferredAccounts.Count == 0)
                return 0m;

            // Compute total prior-year balance
            decimal totalPriorYearBalance = deferredAccounts.Sum(a => a.Value);
            if (totalPriorYearBalance <= 0m)
                return 0m;

            // Compute required RMD
            decimal rmdAmount = totalPriorYearBalance / divisor;
            if (rmdAmount <= 0m)
                return 0m;

            // Already withdrawn this year
            deferredWithdrawnThisYear.TryGetValue(ownerId, out decimal alreadyWithdrawn);

            decimal remainingRmd = Math.Max(0m, rmdAmount - alreadyWithdrawn);
            if (remainingRmd <= 0m)
                return 0m;

            // Find taxable cash destination
            var taxableCash = workingAccounts.FirstOrDefault(a =>
                a.OwnerId == ownerId &&
                a.TaxBucket.Trim() == "Taxable" &&
                a.AssetClass.Trim() == "Cash");

            if (taxableCash == null)
                return 0m;

            decimal remaining = remainingRmd;

            // Proportional withdrawals
            foreach (var acct in deferredAccounts)
            {
                if (remaining <= 0m)
                    break;

                decimal priorBalance = acct.Value;
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
                return 0m;

            // Add to ordinary income
            _tax.OrdinaryIncome += executedRmd;

            return executedRmd;
        }



        private void DepositSocialSecurityAndIncome_Flat(
            int year,
            List<Account> workingAccounts,
            ref decimal ssIncome,
            ref decimal otherIncome)
        {
            // -----------------------------
            // PRIMARY SOCIAL SECURITY
            // -----------------------------
            int primaryAge = _primary.CurrentAge + year;

            if (primaryAge >= _primary.ClaimAge && primaryAge <= _primary.DeathAge)
            {
                var acct = workingAccounts.First(a =>
                    a.OwnerId == _primary.UserId &&
                    a.TaxBucket.Trim() == "Tax Free" &&
                    a.AssetClass.Trim() == "Cash");

                decimal benefit = CalculateColaAdjustedBenefit(
                    currentAge: primaryAge,
                    claimAge: _primary.ClaimAge,
                    fraMonthly: _primary.BenefitAtFullRetirementAge,
                    colaRate: _primary.Cola);

                acct.Value += benefit;
                ssIncome += benefit;
            }

            // -----------------------------
            // OTHER INCOME STREAMS
            // -----------------------------
            foreach (var income in _scenario.IncomeStreams)
            {
                int ownerAge = GetAgeForOwner(income.OwnerId, year);

                if (ownerAge >= income.StartAge && ownerAge <= income.EndAge)
                {
                    var acct = workingAccounts.First(a =>
                        a.OwnerId == income.OwnerId &&
                        a.TaxBucket.Trim() == "Tax Free" &&
                        a.AssetClass.Trim() == "Cash");

                    acct.Value += income.AnnualAmount;
                    otherIncome += income.AnnualAmount;

                    if (income.IsTaxable)
                        _tax.OrdinaryIncome += income.AnnualAmount;
                }
            }

            // -----------------------------
            // SPOUSE SOCIAL SECURITY
            // -----------------------------
            if (_spouse != null)
            {
                int spouseAge = _spouse.CurrentAge + year;

                if (spouseAge >= _spouse.ClaimAge && spouseAge <= _spouse.DeathAge)
                {
                    var acct = workingAccounts.First(a =>
                        a.OwnerId == _spouse.UserId &&
                        a.TaxBucket.Trim() == "Tax Free" &&
                        a.AssetClass.Trim() == "Cash");

                    decimal benefit = CalculateColaAdjustedBenefit(
                        currentAge: spouseAge,
                        claimAge: _spouse.ClaimAge,
                        fraMonthly: _spouse.BenefitAtFullRetirementAge,
                        colaRate: _spouse.Cola);

                    acct.Value += benefit;
                    ssIncome += benefit;
                }
            }
        }

        private decimal CalculateColaAdjustedBenefit(
    int currentAge,
    int claimAge,
    decimal fraMonthly,
    decimal colaRate)
        {
            // FRA monthly → annual
            decimal fraAnnual = fraMonthly * 12m;

            // 1. COLA applies starting at age 62
            int yearsSince62 = Math.Max(0, currentAge - 62);
            decimal fraWithCola = fraAnnual * (decimal)Math.Pow((double)(1 + colaRate), yearsSince62);

            // 2. Early / Late claiming adjustment
            int ageDiff = claimAge - 67;

            decimal adjustment = ageDiff switch
            {
                > 0 => 0.08m * ageDiff,               // delayed credits
                < 0 => -0.06m * Math.Abs(ageDiff),    // early filing reduction
                _ => 0m
            };

            decimal adjustedAnnual = fraWithCola * (1 + adjustment);

            return Math.Max(0, adjustedAnnual);
        }




        private int GetAgeForOwner(Guid ownerId, int year)
        {
            if (ownerId == _primary.UserId)
                return _primary.CurrentAge + year;

            if (_spouse != null && ownerId == _spouse.UserId)
                return _spouse.CurrentAge + year;

            return 0;
        }

        private decimal CalculateBenefits(int claimage, decimal amount)
        {
            
            decimal fraAnnual = amount * 12m;

            int ageDiff = claimage - 67;

            decimal adjustment = ageDiff switch
            {
                > 0 => 0.08m * ageDiff,
                < 0 => -0.06m * Math.Abs(ageDiff),
                _ => 0m
            };

            decimal adjustedAnnual = fraAnnual * (1 + adjustment);

            return Math.Max(0, adjustedAnnual);
        }

        private List<AccountProjection> CreateProjectionsSkeleton(List<Account> accounts, int years)
        {
            var projections = new List<AccountProjection>();

            foreach (var acct in accounts)
            {
                var proj = new AccountProjection
                {
                    OwnerId = acct.OwnerId,
                    Name = acct.Owner,
                    TaxBucket = acct.TaxBucket.Trim(),
                    AssetClass = acct.AssetClass.Trim(),
                    StartingValue = acct.Value,
                    RateOfReturn = acct.RateOfReturn,
                    ValuesByYear = new List<decimal>(new decimal[years]),
                    InvestmentReturnByYear = new List<decimal>(new decimal[years])
                };

                projections.Add(proj);
            }

            return projections;
        }

        private void RebalanceWorkingAccounts(List<Account> accounts, Scenario scenario)
        {
            foreach (var bucket in new[] { "Tax Free", "Tax Deferred", "Taxable" })
            {
                var eq = accounts.FirstOrDefault(a =>
                    a.TaxBucket.Trim() == bucket && a.AssetClass.Trim() == "Equities");

                var bo = accounts.FirstOrDefault(a =>
                    a.TaxBucket.Trim() == bucket && a.AssetClass.Trim() == "Bonds");

                if (eq == null || bo == null)
                    continue;

                decimal total = eq.Value + bo.Value;
                if (total <= 0)
                    continue;

                decimal targetEquity = total * scenario.TargetEquityPercentage;
                decimal delta = targetEquity - eq.Value;

                eq.Value += delta;
                bo.Value -= delta;
            }
        }

        private decimal ApplyRothConversionsForYear_Flat(
            int yearIndex,
            List<Account> workingAccounts)
        {
            decimal total = 0m;

            foreach (var conv in _scenario.RothConversions)
            {
                int ownerAge = GetAgeForOwner(conv.OwnerId, yearIndex);
                if (ownerAge < conv.StartAge || ownerAge > conv.EndAge)
                    continue;

                if (conv.AnnualAmount <= 0)
                    continue;

                decimal actual = _conversionStrategy.Convert(
                    conv.OwnerId,
                    conv.AnnualAmount,
                    workingAccounts,
                    _tax,
                    null, // no SimulationResult
                    yearIndex);

                total += actual;
            }

            return total;
        }


        private void ApplyInheritanceForYear(
            int yearIndex,
            List<Account> workingAccounts)
        {
            int primaryAge = _primary.CurrentAge + yearIndex;
            int spouseAge = _spouse != null ? _spouse.CurrentAge + yearIndex : int.MaxValue;

            bool primaryDiedThisYear = primaryAge == _primary.DeathAge;
            bool spouseDiedThisYear = _spouse != null && spouseAge == _spouse.DeathAge;

            // No deaths → no inheritance
            if (!primaryDiedThisYear && !spouseDiedThisYear)
                return;

            // Determine survivor
            Guid? survivorId = null;

            if (primaryDiedThisYear && _spouse != null)
                survivorId = _spouse.UserId;

            if (spouseDiedThisYear)
                survivorId = _primary.UserId;

            // Single-person household → nothing to inherit
            if (survivorId == null)
                return;

            Guid deceasedId = primaryDiedThisYear ? _primary.UserId : _spouse!.UserId;

            // Now safe to call
            TransferAccountsOnDeath(deceasedId, survivorId.Value, workingAccounts);
        }




        private void TransferAccountsOnDeath(
                Guid deceasedId,
                Guid survivorId,
                List<Account> workingAccounts)
                //List<AccountProjection> projections)
        {
            foreach (var acct in workingAccounts.Where(a => a.OwnerId == deceasedId).ToList())
            {
                if (acct.Value <= 0m)
                    continue;

                // Find matching survivor account
                var survivorAcct = workingAccounts.FirstOrDefault(a =>
                    a.OwnerId == survivorId &&
                    a.TaxBucket.Trim() == acct.TaxBucket.Trim() &&
                    a.AssetClass.Trim() == acct.AssetClass.Trim());

                // Create survivor account if needed
                if (survivorAcct == null)
                {
                    survivorAcct = new Account
                    {
                        OwnerId = survivorId,
                        Owner = survivorId == _primary.UserId ? _primary.Owner : _spouse!.Owner,
                        TaxBucket = acct.TaxBucket,
                        AssetClass = acct.AssetClass,
                        Priority = acct.Priority,
                        RateOfReturn = acct.RateOfReturn,
                        Value = 0m,
                        CostBasis = 0m
                    };

                    workingAccounts.Add(survivorAcct);

                  //  projections.Add(new AccountProjection
                  //  {
                  //      OwnerId = survivorId,
                   //     Name = survivorAcct.Owner,
                   //     TaxBucket = survivorAcct.TaxBucket.Trim(),
                   //     AssetClass = survivorAcct.AssetClass.Trim(),
                   //     StartingValue = 0m,
                   //     RateOfReturn = survivorAcct.RateOfReturn,
                   //     ValuesByYear = new List<decimal>(new decimal[_scenario.YearsToCalculate]),
                  //      InvestmentReturnByYear = new List<decimal>(new decimal[_scenario.YearsToCalculate])
                  //  });
                }

                // Transfer logic by tax bucket
                if (acct.TaxBucket.Trim() == "Taxable")
                {
                    // Step-up in basis
                    survivorAcct.Value += acct.Value;
                    survivorAcct.CostBasis += acct.Value;
                }
                else
                {
                    // Tax Deferred or Tax Free
                    survivorAcct.Value += acct.Value;
                }

                // Zero out deceased account
                acct.Value = 0m;
                acct.CostBasis = 0m;
            }
        }





    }
}


