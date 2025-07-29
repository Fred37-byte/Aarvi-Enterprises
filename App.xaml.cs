using System;
using System.Windows;
using System.Windows.Threading;
using EmployeeManagerWPF;
using NewCustomerWindow; // ✅ Correct namespace

namespace NewCustomerWindow  // ✅ Matches your project root
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var splash = new SplashWindow();
            splash.Show();

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                splash.Close();

                var main = new InvoiceListWindow();  // ✅ This should now work
                main.Show();
            };
            timer.Start();
        }
    }
}
