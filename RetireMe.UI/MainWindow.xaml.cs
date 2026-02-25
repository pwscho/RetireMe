using RetireMe.UI.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RetireMe.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.SaveCommand.Execute(null);

            base.OnClosing(e);
        }


    }



}

