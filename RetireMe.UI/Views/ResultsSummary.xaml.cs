using RetireMe.UI.ViewModels;
using System.Windows;

namespace RetireMe.UI.Views
{
    public partial class ResultsSummary : Window
    {
        public ResultsSummary(ResultsSummaryViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}

