using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;

namespace RetireMe.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }

    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                File.WriteAllText("crash.log", e.ExceptionObject.ToString());
            };
        }
    }


}



