using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NewCustomerWindow
{
    public partial class CarWashingWindow : Window
    {
        private List<dynamic> subscriptions;      // all records
        private List<dynamic> filteredResults;    // filtered records

        public CarWashingWindow()
        {
            InitializeComponent();
            LoadStats();
            LoadSubscriptions();
        }

        private void LoadStats()
        {
            txtActiveSocieties.Text = "12";
            txtSubscribedCars.Text = "156";
            txtCarWashers.Text = "8";
            txtRevenue.Text = "₹78,000";
        }

        private void LoadSubscriptions()
        {
            subscriptions = new List<dynamic>
            {
                new {CustomerID=1, Customer = "Priya Sharma", Society="Green Valley", CarDetails="Maruti Swift MH14 CD 5678", Plan="₹800/month", Washer="Suresh Patil", Status="Active", NextDue="10 Dec 2024" },
                new {CustomerID=2, Customer = "Amit Patel", Society="Sunrise Apartments", CarDetails="Toyota Innova GJ 05 EF 9012", Plan="₹800/month", Washer="Amit Shah", Status="Payment Due", NextDue="5 Dec 2024" },
                new {CustomerID=3, Customer = "Sneha Reddy", Society="Palm Residency", CarDetails="Hyundai Creta TS 08 GH 3456", Plan="₹500/month", Washer="Ravi Kumar", Status="Active", NextDue="20 Dec 2024" },
                new {CustomerID=4, Customer = "Vikram Singh", Society="City Heights", CarDetails="Mahindra XUV UP 16 U 7890", Plan="₹800/month", Washer="Mohan Joshi", Status="Suspended", NextDue="-" },
            };

            filteredResults = subscriptions;
            dgSubscriptions.ItemsSource = filteredResults;
        }

        private void ApplySearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                filteredResults = subscriptions;
            }
            else
            {
                query = query.ToLower();
                filteredResults = subscriptions.Where(s =>
                    s.Customer.ToLower().Contains(query) ||
                    s.Society.ToLower().Contains(query) ||
                    s.CarDetails.ToLower().Contains(query) ||
                    s.Status.ToLower().Contains(query)
                ).ToList();
            }

            dgSubscriptions.ItemsSource = null;
            dgSubscriptions.ItemsSource = filteredResults;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;

            ApplySearch(SearchBox.Text);
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
                SearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
                SearchPlaceholder.Visibility = Visibility.Visible;
        }
    }
}
