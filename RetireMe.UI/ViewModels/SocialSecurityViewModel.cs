using System.Linq;
using RetireMe.Core;

namespace RetireMe.UI.ViewModels
{
    public class SocialSecurityViewModel : ViewModelBase
    {
        private readonly AccountsViewModel _accountsVM;

        public SocialSecuritySettings Settings { get; }

        public Guid UserId => Settings.UserId;

        public SocialSecurityViewModel(SocialSecuritySettings settings, AccountsViewModel accountsVM)
        {
            Settings = settings;
            _accountsVM = accountsVM;
        }

        public string Owner
        {
            get => Settings.Owner;
            set
            {
                if (Settings.Owner != value)
                {
                    Settings.Owner = value;
                    OnPropertyChanged(nameof(Owner));
                    UpdateAccountOwnerNames();
                }
            }
        }

        public int CurrentAge
        {
            get => Settings.CurrentAge;
            set
            {
                if (Settings.CurrentAge != value)
                {
                    Settings.CurrentAge = value;
                    OnPropertyChanged(nameof(CurrentAge));
                }
            }
        }

        public int ClaimAge
        {
            get => Settings.ClaimAge;
            set
            {
                if (Settings.ClaimAge != value)
                {
                    Settings.ClaimAge = value;
                    OnPropertyChanged(nameof(ClaimAge));
                }
            }
        }

        public int DeathAge
        {
            get => Settings.DeathAge;
            set
            {
                if (Settings.DeathAge != value)
                {
                    Settings.DeathAge = value;
                    OnPropertyChanged(nameof(DeathAge));
                }
            }
        }

        public decimal BenefitAtFullRetirementAge
        {
            get => Settings.BenefitAtFullRetirementAge;
            set
            {
                if (Settings.BenefitAtFullRetirementAge != value)
                {
                    Settings.BenefitAtFullRetirementAge = value;
                    OnPropertyChanged(nameof(BenefitAtFullRetirementAge));
                }
            }
        }

        public decimal ColaRate
        {
            get => Settings.Cola;
            set
            {
                Settings.Cola = value;
                OnPropertyChanged();
            }
        }

        public bool IsSurvivorBenefit
        {
            get => Settings.IsSurvivorBenefit;
            set
            {
                if (Settings.IsSurvivorBenefit != value)
                {
                    Settings.IsSurvivorBenefit = value;
                    OnPropertyChanged(nameof(IsSurvivorBenefit));
                }
            }
        }

        public decimal SurvivorBenefitAmount
        {
            get => Settings.SurvivorBenefitAmount;
            set
            {
                if (Settings.SurvivorBenefitAmount != value)
                {
                    Settings.SurvivorBenefitAmount = value;
                    OnPropertyChanged(nameof(SurvivorBenefitAmount));
                }
            }
        }

        private void UpdateAccountOwnerNames()
        {
            if (_accountsVM == null)
                return;

            foreach (var acct in _accountsVM.Accounts.Where(a => a.OwnerId == UserId))
                acct.Owner = Settings.Owner;

            _accountsVM.Refresh();
        }
    }
}
