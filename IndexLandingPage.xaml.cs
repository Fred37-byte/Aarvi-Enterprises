using EmployeeManagerWPF;
using System;
using System.Windows;

namespace NewCustomerWindow.xaml
{
    public partial class IndexLandingPage : Window
    {
        public IndexLandingPage()
        {
            InitializeComponent();
        }
        private void OpenWaterServiceWindow(object sender, RoutedEventArgs e)
        {
            WaterServiceWindow wsWindow = new WaterServiceWindow();
            wsWindow.ShowDialog(); // or .Show() if you don't want it modal
        }


        private void BtnEmployees_Click(object sender, RoutedEventArgs e)
        {
            var employeeListWindow = new EmployeeList();
            employeeListWindow.ShowDialog();
        }

        private void BtnNewCustomers_Click(object sender, RoutedEventArgs e)
        {
            var newcustomerWindow = new newcustomer();
            newcustomerWindow.ShowDialog();
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow helpWindow = new HelpWindow();
            helpWindow.Owner = this;
            helpWindow.ShowDialog(); // modal
        }





        private void BtnInvoices_Click(object sender, RoutedEventArgs e)
        {
            var invoiceListWindow = new InvoiceListWindow();
            invoiceListWindow.ShowDialog();
        }
    }
}
