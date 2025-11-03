using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NewCustomerWindow.xaml
{
    public partial class CarListWindow : Window
    {
        private string connectionString;
        private ObservableCollection<CarOrder> carOrders;
        private ObservableCollection<CarOrder> filteredCarOrders;
        private int societyId;
        private string currentFilter = "All";

        public CarListWindow(int societyId, string societyName)
        {
            InitializeComponent();

            this.societyId = societyId;
            connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            carOrders = new ObservableCollection<CarOrder>();
            filteredCarOrders = new ObservableCollection<CarOrder>();
            CarsDataGrid.ItemsSource = filteredCarOrders;

            SocietyNameText.Text = $"Society: {societyName}";

            LoadCarOrders();
        }

        private void LoadCarOrders()
        {
            carOrders.Clear();
            filteredCarOrders.Clear();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"SELECT CarInvoiceId, CustomerName, CarModel, CarNumber, 
                                           Subscription, SubscriptionType, Washer, Status, 
                                           NextDueDate, OrderDate
                                    FROM CarWashingOrders 
                                    WHERE SocietyId = @societyId AND Status = 'Active'
                                    ORDER BY NextDueDate ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@societyId", societyId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime? orderDate = reader["OrderDate"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["OrderDate"])
                                    : (DateTime?)null;

                                DateTime? nextDue = reader["NextDueDate"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["NextDueDate"])
                                    : (DateTime?)null;

                                var carOrder = new CarOrder
                                {
                                    CarInvoiceId = Convert.ToInt32(reader["CarInvoiceId"]),
                                    CustomerName = reader["CustomerName"]?.ToString() ?? "N/A",
                                    CarModel = reader["CarModel"]?.ToString() ?? "N/A",
                                    RegistrationNumber = reader["CarNumber"]?.ToString() ?? "N/A",
                                    Subscription = reader["Subscription"]?.ToString() ?? "0",
                                    SubscriptionType = reader["SubscriptionType"]?.ToString() ?? "Monthly",
                                    Washer = reader["Washer"]?.ToString() ?? "Unassigned",
                                    StatusValue = reader["Status"]?.ToString() ?? "N/A",
                                    StartDate = orderDate,
                                    NextDueDate = nextDue
                                };

                                carOrders.Add(carOrder);
                                filteredCarOrders.Add(carOrder);
                            }
                        }
                    }
                }

                UpdateStatistics();
                UpdateFilterCounts();
                UpdateDashboardCards();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading car orders: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Calculate normalized monthly revenue
        private decimal CalculateNormalizedMRR(CarOrder car)
        {
            if (!decimal.TryParse(car.Subscription, out decimal amount))
                return 0;

            string subscriptionType = car.SubscriptionType?.ToLower() ?? "monthly";

            switch (subscriptionType)
            {
                case "monthly":
                    return amount; // Already monthly
                case "quarterly":
                    return amount / 3; // Divide by 3 months
                case "yearly":
                    return amount / 12; // Divide by 12 months
                default:
                    return amount; // Default to monthly
            }
        }

        // Calculate total raw revenue (sum of all subscriptions)
        private decimal CalculateTotalRevenue(CarOrder car)
        {
            if (!decimal.TryParse(car.Subscription, out decimal amount))
                return 0;
            return amount;
        }

        // Updated statistics with Total Revenue and Total MRR
        private void UpdateStatistics()
        {
            int totalCars = carOrders.Count;

            // Calculate total raw revenue (just sum all amounts)
            decimal totalRevenue = carOrders.Sum(c => CalculateTotalRevenue(c));

            // Calculate normalized monthly revenue
            decimal totalMRR = carOrders.Sum(c => CalculateNormalizedMRR(c));

            int activeWashers = carOrders.Select(c => c.Washer).Distinct().Count(w => w != "Unassigned");

            ActiveCarsText.Text = totalCars.ToString();
            TotalMRRText.Text = $"₹{totalRevenue:N0}";  // Shows Total Revenue
            AvgRevenueText.Text = $"₹{totalMRR:N0}";    // Shows Total MRR
            ActiveWashersText.Text = activeWashers.ToString();
        }

        private void UpdateFilterCounts()
        {
            int overdue = carOrders.Count(c => c.PaymentStatus.Contains("OVERDUE"));
            int dueSoon = carOrders.Count(c => c.PaymentStatus.Contains("DUE SOON"));
            int active = carOrders.Count(c => c.PaymentStatus.Contains("ACTIVE") && !c.PaymentStatus.Contains("OVERDUE") && !c.PaymentStatus.Contains("DUE SOON"));

            FilterAllBtn.Content = $"All ({carOrders.Count})";

            // Update filter buttons with StackPanel content
            if (FilterOverdueBtn.Content is StackPanel overdueStack && overdueStack.Children.Count > 1)
            {
                if (overdueStack.Children[1] is TextBlock overdueText)
                {
                    overdueText.Text = $"Overdue ({overdue})";
                }
            }

            if (FilterDueSoonBtn.Content is StackPanel dueSoonStack && dueSoonStack.Children.Count > 1)
            {
                if (dueSoonStack.Children[1] is TextBlock dueSoonText)
                {
                    dueSoonText.Text = $"Due Soon ({dueSoon})";
                }
            }

            if (FilterActiveBtn.Content is StackPanel activeStack && activeStack.Children.Count > 1)
            {
                if (activeStack.Children[1] is TextBlock activeText)
                {
                    activeText.Text = $"Active ({active})";
                }
            }
        }

        // Dashboard cards use normalized MRR
        private void UpdateDashboardCards()
        {
            var overdueCars = carOrders.Where(c => c.PaymentStatus.Contains("OVERDUE")).ToList();
            var dueSoonCars = carOrders.Where(c => c.PaymentStatus.Contains("DUE SOON")).ToList();
            var activeCars = carOrders.Where(c => c.PaymentStatus.Contains("ACTIVE") && !c.PaymentStatus.Contains("OVERDUE") && !c.PaymentStatus.Contains("DUE SOON")).ToList();

            // Calculate normalized monthly revenue for each category
            decimal overdueMRR = overdueCars.Sum(c => CalculateNormalizedMRR(c));
            decimal dueSoonMRR = dueSoonCars.Sum(c => CalculateNormalizedMRR(c));
            decimal activeMRR = activeCars.Sum(c => CalculateNormalizedMRR(c));

            OverdueCountText.Text = overdueCars.Count.ToString();
            OverdueMRRText.Text = $"₹{overdueMRR:N0}/mo";

            DueSoonCountText.Text = dueSoonCars.Count.ToString();
            DueSoonMRRText.Text = $"₹{dueSoonMRR:N0}/mo";

            ActiveStatusCountText.Text = activeCars.Count.ToString();
            ActiveMRRText.Text = $"₹{activeMRR:N0}/mo";
        }

        private void ApplyFilter()
        {
            filteredCarOrders.Clear();

            var filtered = carOrders.AsEnumerable();

            if (currentFilter == "Overdue")
                filtered = filtered.Where(c => c.PaymentStatus.Contains("OVERDUE"));
            else if (currentFilter == "DueSoon")
                filtered = filtered.Where(c => c.PaymentStatus.Contains("DUE SOON"));
            else if (currentFilter == "Active")
                filtered = filtered.Where(c => c.PaymentStatus.Contains("ACTIVE") && !c.PaymentStatus.Contains("OVERDUE") && !c.PaymentStatus.Contains("DUE SOON"));

            string searchText = SearchTextBox.Text.ToLower();
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filtered = filtered.Where(c =>
                    c.CustomerName.ToLower().Contains(searchText) ||
                    c.CarDetails.ToLower().Contains(searchText) ||
                    c.Subscription.ToLower().Contains(searchText) ||
                    c.PaymentStatus.ToLower().Contains(searchText));
            }

            foreach (var item in filtered)
            {
                filteredCarOrders.Add(item);
            }
        }

        // Context Menu Handlers
        private void UpdateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CarsDataGrid.SelectedItem is CarOrder selectedCar)
            {
                UpdateCarWindow updateWindow = new UpdateCarWindow(selectedCar.CarInvoiceId, societyId);
                if (updateWindow.ShowDialog() == true)
                {
                    LoadCarOrders(); // Refresh the data grid
                }
            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CarsDataGrid.SelectedItem is CarOrder selectedCar)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete this car subscription?\n\nCustomer: {selectedCar.CustomerName}\nCar: {selectedCar.CarDetails}",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    DeleteCarOrder(selectedCar.CarInvoiceId);
                }
            }
        }

        private void DeleteCarOrder(int carInvoiceId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Option 1: Hard delete
                    string query = "DELETE FROM CarWashingOrders WHERE CarInvoiceId = @carInvoiceId";

                    // Option 2: Soft delete (recommended) - just mark as inactive
                    // string query = "UPDATE CarWashingOrders SET Status = 'Inactive' WHERE CarInvoiceId = @carInvoiceId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@carInvoiceId", carInvoiceId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Car subscription deleted successfully!", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadCarOrders(); // Refresh the list
                        }
                        else
                        {
                            MessageBox.Show("Failed to delete car subscription.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting car subscription: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterAllBtn_Click(object sender, RoutedEventArgs e)
        {
            currentFilter = "All";
            UpdateFilterButtonStyles();
            ApplyFilter();
        }

        private void FilterOverdueBtn_Click(object sender, RoutedEventArgs e)
        {
            currentFilter = "Overdue";
            UpdateFilterButtonStyles();
            ApplyFilter();
        }

        private void FilterDueSoonBtn_Click(object sender, RoutedEventArgs e)
        {
            currentFilter = "DueSoon";
            UpdateFilterButtonStyles();
            ApplyFilter();
        }

        private void FilterActiveBtn_Click(object sender, RoutedEventArgs e)
        {
            currentFilter = "Active";
            UpdateFilterButtonStyles();
            ApplyFilter();
        }

        private void UpdateFilterButtonStyles()
        {
            FilterAllBtn.Background = new SolidColorBrush(Color.FromRgb(211, 211, 211));
            FilterOverdueBtn.Background = new SolidColorBrush(Color.FromRgb(211, 211, 211));
            FilterDueSoonBtn.Background = new SolidColorBrush(Color.FromRgb(211, 211, 211));
            FilterActiveBtn.Background = new SolidColorBrush(Color.FromRgb(211, 211, 211));

            FilterAllBtn.Foreground = Brushes.Black;
            FilterOverdueBtn.Foreground = Brushes.Black;
            FilterDueSoonBtn.Foreground = Brushes.Black;
            FilterActiveBtn.Foreground = Brushes.Black;

            Button activeBtn;
            if (currentFilter == "Overdue")
                activeBtn = FilterOverdueBtn;
            else if (currentFilter == "DueSoon")
                activeBtn = FilterDueSoonBtn;
            else if (currentFilter == "Active")
                activeBtn = FilterActiveBtn;
            else
                activeBtn = FilterAllBtn;

            activeBtn.Background = new SolidColorBrush(Color.FromRgb(79, 70, 229));
            activeBtn.Foreground = Brushes.White;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearSearchButton.Visibility = string.IsNullOrWhiteSpace(SearchTextBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;

            ApplyFilter();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
        }

        private void OverdueCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentFilter = "Overdue";
            UpdateFilterButtonStyles();
            ApplyFilter();
        }

        private void DueSoonCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentFilter = "DueSoon";
            UpdateFilterButtonStyles();
            ApplyFilter();
        }

        private void ActiveCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            currentFilter = "Active";
            UpdateFilterButtonStyles();
            ApplyFilter();
        }
        // Add this new method for Generate Invoice
        private void GenerateInvoiceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CarsDataGrid.SelectedItem is CarOrder selectedCar)
            {
                // Open the invoice generation window
                CarInvoiceWindow invoiceWindow = new CarInvoiceWindow(selectedCar.CarInvoiceId);
                invoiceWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Please select a car to generate invoice.", "No Selection",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class CarOrder
    {
        public int CarInvoiceId { get; set; }
        public string CustomerName { get; set; }
        public string CarModel { get; set; }
        public string RegistrationNumber { get; set; }
        public string Subscription { get; set; }
        public string SubscriptionType { get; set; }
        public string Washer { get; set; }
        public string StatusValue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? NextDueDate { get; set; }

        public string CarDetails => $"{CarModel} • {RegistrationNumber}";

        public string SubscriptionDisplay => $"₹{Subscription}/{(SubscriptionType == "Monthly" ? "mo" : (SubscriptionType == "Quarterly" ? "qtr" : "yr"))}";

        public string StartDateFormatted => StartDate?.ToString("dd MMM yyyy") ?? "N/A";

        public string DueDateFormatted => NextDueDate?.ToString("dd MMM yyyy") ?? "N/A";

        public string PaymentStatus
        {
            get
            {
                if (!NextDueDate.HasValue)
                    return "⚪ ACTIVE";

                int daysUntilDue = (NextDueDate.Value - DateTime.Now).Days;

                if (daysUntilDue < 0)
                    return "🔴 OVERDUE";
                else if (daysUntilDue <= 7)
                    return "🟡 DUE SOON";
                else
                    return "🟢 ACTIVE";
            }
        }

        public string PaymentStatusText
        {
            get
            {
                if (!NextDueDate.HasValue)
                    return "ACTIVE";

                int daysUntilDue = (NextDueDate.Value - DateTime.Now).Days;

                if (daysUntilDue < 0)
                    return "OVERDUE";
                else if (daysUntilDue <= 7)
                    return "DUE SOON";
                else
                    return "ACTIVE";
            }
        }


        public string Status
        {
            get
            {
                if (!NextDueDate.HasValue)
                    return StatusValue;

                int daysUntilDue = (NextDueDate.Value - DateTime.Now).Days;

                if (daysUntilDue < 0)
                    return $"{Math.Abs(daysUntilDue)}d late";
                else if (daysUntilDue == 0)
                    return "Due today";
                else if (daysUntilDue <= 7)
                    return $"{daysUntilDue}d left";
                else
                    return "Active";
            }
        }
    }
}