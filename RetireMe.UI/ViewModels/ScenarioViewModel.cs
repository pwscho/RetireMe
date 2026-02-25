using RetireMe.Core;
using System.Diagnostics;

namespace RetireMe.UI.ViewModels
{
    public class ScenarioViewModel : ViewModelBase
    {
        private readonly ScenarioState _state;

        public ScenarioViewModel(ScenarioState state)
        {
            _state = state;

            IncomeVM = new IncomeViewModel(_state.Scenario);
            WithdrawalsVM = new WithdrawalsViewModel(_state.Scenario);
        }

        public Scenario Scenario => _state.Scenario;

        public string Name
        {
            get => _state.Name;
            set
            {
                if (_state.Name != value)
                {
                    _state.Name = value;
                    OnPropertyChanged();
                }
               
            }
        }

        public int BaseYear
        {
            get => Scenario.BaseYear;
            set
            {
                if (Scenario.BaseYear != value)
                {
                    Scenario.BaseYear = value;
                    OnPropertyChanged();
                }
            }
        }

        public int YearsToCalculate
        {
            get => Scenario.YearsToCalculate;
            set
            {
                if (Scenario.YearsToCalculate != value)
                {
                    Scenario.YearsToCalculate = value;
                    OnPropertyChanged();
                }
            }
        }

        public void NotifyYearsChanged()
        {
            OnPropertyChanged(nameof(YearsToCalculate));
        }

        public int RetirementAge
        {
            get => Scenario.RetirementAge;
            set
            {
                Scenario.RetirementAge = value;
                OnPropertyChanged();
            }
        }

        public decimal AnnualSpending
        {
            get => Scenario.AnnualSpending;
            set
            {
                Scenario.AnnualSpending = value;
                OnPropertyChanged();
            }
        }

        public string SpendingStrategy
        {
            get => Scenario.SpendingStrategy;
            set
            {
                Scenario.SpendingStrategy = value;
                OnPropertyChanged();
            }
        }

        public decimal InflationRate
        {
            get => Scenario.InflationRate;
            set
            {
                Scenario.InflationRate = value;
                OnPropertyChanged();
            }
        }

        public decimal TaxesFromLastYear
        {
            get => Scenario.TaxesFromLastYear;
            set
            {
                Scenario.TaxesFromLastYear = value;
                OnPropertyChanged();
            }
        }


        public decimal WithdrawalAdjustmentRate
        {
            get => Scenario.WithdrawalAdjustmentRate;
            set
            {
                Scenario.WithdrawalAdjustmentRate = value;
                OnPropertyChanged();
            }
        }

        public bool RebalanceAnnually
        {
            get => Scenario.RebalanceAnnually;
            set
            {
                Scenario.RebalanceAnnually = value;
                OnPropertyChanged();
            }
        }

        public decimal TargetEquityPercentage
        {
            get => Scenario.TargetEquityPercentage;
            set
            {
                var eq = Math.Clamp(value, 0m, 1m);

                Scenario.TargetEquityPercentage = eq;
                Scenario.TargetBondPercentage = 1.0m - eq;

                OnPropertyChanged(nameof(TargetEquityPercentage));
                OnPropertyChanged(nameof(TargetBondPercentage));
            }
        }

        public decimal TargetBondPercentage
        {
            get => Scenario.TargetBondPercentage;
            set
            {
                var bo = Math.Clamp(value, 0m, 1m);

                Scenario.TargetBondPercentage = bo;
                Scenario.TargetEquityPercentage = 1.0m - bo;

                OnPropertyChanged(nameof(TargetBondPercentage));
                OnPropertyChanged(nameof(TargetEquityPercentage));
            }
        }

        public IncomeViewModel IncomeVM { get; }
        public WithdrawalsViewModel WithdrawalsVM { get; }

        public void RefreshOwnerOptions(
            string primaryName,
            Guid primaryId,
            string? spouseName,
            Guid? spouseId)
        {
            IncomeVM.RefreshOwnerOptions(primaryName, primaryId, spouseName, spouseId);
            WithdrawalsVM.RefreshOwnerOptions(primaryName, primaryId, spouseName, spouseId);
        }
    }
}
