using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetireMe.Core.Strategies
{
    public interface IWithdrawalStrategy
    {
        IEnumerable<Account> GetWithdrawalOrder(Scenario scenario, List<Account> accounts);
    }
}

