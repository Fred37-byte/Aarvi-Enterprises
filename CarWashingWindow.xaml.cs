using NewCustomerWindow.xaml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NewCustomerWindow
{
    public partial class CarWashingWindow : Window
    {
        private DataTable subscriptionsData;      // all records
        private DataView filteredResults;         // filtered records

        public CarWashingWindow()
        {
            InitializeComponent();
            LoadStats();
            LoadOrders();
            LoadSocieties(); // 👈 Load societies when window opens
        }

        private void LoadSocieties()
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT SocietyName FROM CarWashingSocieties";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    cmbSociety.ItemsSource = dt.DefaultView;
                    cmbSociety.DisplayMemberPath = "SocietyName";
                    cmbSociety.SelectedValuePath = "SocietyName";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading societies: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadOrders()
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();

                    string query = @"SELECT 
                        CarInvoiceId as CustomerID,
                        CustomerName as Customer, 
                        Society,
                        CONCAT(CarModel, ' (', CarNumber, ')') as CarDetails,
                        Subscription as [Plan],
                        Washer,
                        Status,
                        FORMAT(NextDueDate, 'dd-MM-yyyy') as NextDue
                        FROM CarWashingOrders 
                        ORDER BY CarInvoiceId ASC";

                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    subscriptionsData = new DataTable();
                    da.Fill(subscriptionsData);

                    filteredResults = subscriptionsData.DefaultView;
                    dgSubscriptions.ItemsSource = filteredResults;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading data: " + ex.Message);
                }
            }
        }

        private void LoadStats()
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();

                    // 1. Count Active Societies
                    string querySocieties = "SELECT COUNT(*) FROM CarWashingSocieties";
                    SqlCommand cmdSocieties = new SqlCommand(querySocieties, conn);
                    int activeSocieties = Convert.ToInt32(cmdSocieties.ExecuteScalar());

                    // 2. Count Subscribed Cars
                    string queryCars = "SELECT COUNT(*) FROM CarWashingOrders";
                    SqlCommand cmdCars = new SqlCommand(queryCars, conn);
                    int subscribedCars = Convert.ToInt32(cmdCars.ExecuteScalar());

                    // 3. Car Washers (count unique washers)
                    string queryWashers = "SELECT COUNT(DISTINCT Washer) FROM CarWashingOrders WHERE Washer IS NOT NULL AND Washer != ''";
                    SqlCommand cmdWashers = new SqlCommand(queryWashers, conn);
                    int carWashers = Convert.ToInt32(cmdWashers.ExecuteScalar());

                    // 4. Monthly Revenue - Fixed calculation with proper pricing
                    decimal monthlyRevenue = CalculateMonthlyRevenue(conn);

                    // Update UI
                    txtActiveSocieties.Text = activeSocieties.ToString();
                    txtSubscribedCars.Text = subscribedCars.ToString();
                    txtRevenue.Text = "₹" + monthlyRevenue.ToString("N0");

                    // Update car washers if you have a control for it
                    // txtCarWashers.Text = carWashers.ToString();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading stats: " + ex.Message);
                }
            }
        }

        // ✅ NEW METHOD: Calculate monthly revenue with proper pricing logic
        // ✅ FIXED METHOD: Calculate MONTHLY recurring revenue from Active subscriptions only
        private decimal CalculateMonthlyRevenue(SqlConnection conn)
        {
            try
            {
                // Get only ACTIVE subscriptions grouped by plan type
                string query = @"
            SELECT Subscription, COUNT(*) as Count 
            FROM CarWashingOrders 
            WHERE Status = 'Active'
            GROUP BY Subscription";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                decimal monthlyRevenue = 0;

                while (reader.Read())
                {
                    string subscriptionType = reader["Subscription"]?.ToString()?.ToLower() ?? "";
                    int count = Convert.ToInt32(reader["Count"]);

                    // Convert each plan to its MONTHLY equivalent
                    decimal monthlyRatePerSubscription = GetMonthlyEquivalentRate(subscriptionType);
                    monthlyRevenue += monthlyRatePerSubscription * count;
                }

                reader.Close();
                return monthlyRevenue;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating monthly revenue: {ex.Message}");
                return 0;
            }
        }

        // ✅ FIXED METHOD: Convert subscription plans to monthly equivalent rates
        private decimal GetMonthlyEquivalentRate(string subscriptionType)
        {
            switch (subscriptionType.ToLower())
            {
                case "monthly":
                    return 500; // ₹500 per month

                case "quarterly":
                    return 1350 / 3; // ₹1350 ÷ 3 months = ₹450 per month

                case "yearly":
                    return 5000 / 12; // ₹5000 ÷ 12 months = ₹416.67 per month

                case "premium":
                    return 800; // ₹800 per month (assuming monthly billing)

                case "basic":
                    return 300; // ₹300 per month (assuming monthly billing)

                default:
                    // Try to parse if it's a direct number (assume it's monthly)
                    if (decimal.TryParse(subscriptionType, out decimal amount))
                        return amount;
                    return 500; // Default monthly rate
            }
        }

        private void ApplySearch(string query)
        {
            if (subscriptionsData == null) return;

            if (string.IsNullOrWhiteSpace(query))
            {
                filteredResults.RowFilter = "";
            }
            else
            {
                string escapedQuery = query.Replace("'", "''");
                filteredResults.RowFilter = string.Format(
                    "Convert(Customer, 'System.String') LIKE '%{0}%' OR " +
                    "Convert(Society, 'System.String') LIKE '%{0}%' OR " +
                    "Convert(CarDetails, 'System.String') LIKE '%{0}%' OR " +
                    "Convert([Plan], 'System.String') LIKE '%{0}%' OR " +
                    "Convert(Washer, 'System.String') LIKE '%{0}%' OR " +
                    "Convert(Status, 'System.String') LIKE '%{0}%' OR " +
                    "Convert(NextDue, 'System.String') LIKE '%{0}%'",
                    escapedQuery
                );
            }
        }

        private void BtnNewSociety_Click(object sender, RoutedEventArgs e)
        {
            CarWashingSocietiesOverview overviewWindow = new CarWashingSocietiesOverview();
            overviewWindow.Show();
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

        private DateTime CalculateNextDueDate(string subscriptionType)
        {
            DateTime today = DateTime.Today;

            switch (subscriptionType?.ToLower())
            {
                case "monthly":
                    return today.AddMonths(1);
                case "quarterly":
                    return today.AddMonths(3);
                case "yearly":
                    return today.AddYears(1);
                default:
                    return today.AddMonths(1);
            }
        }

        private void btnSubscribe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbSociety.SelectedValue == null ||
                    string.IsNullOrWhiteSpace(txtCustomer?.Text) ||
                    string.IsNullOrWhiteSpace(txtFlat?.Text) ||
                    string.IsNullOrWhiteSpace(txtMobile?.Text) ||
                    string.IsNullOrWhiteSpace(txtCarModel?.Text) ||
                    string.IsNullOrWhiteSpace(txtCarNumber?.Text))
                {
                    MessageBox.Show("Please fill all required fields marked with *");
                    return;
                }

                string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string subscriptionType = (cmbSubscription.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Monthly";
                    DateTime nextDueDate = CalculateNextDueDate(subscriptionType);

                    string query = @"INSERT INTO CarWashingOrders 
                        (InvoiceId, CustomerId, Society, CustomerName, Flat, Mobile, CarModel, CarNumber, CarType, Subscription, Washer, Status, NextDueDate)
                        VALUES (@InvoiceId, @CustomerId, @Society, @CustomerName, @Flat, @Mobile, @CarModel, @CarNumber, @CarType, @Subscription, @Washer, @Status, @NextDueDate)";

                    SqlCommand cmd = new SqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@InvoiceId", 1);
                    cmd.Parameters.AddWithValue("@CustomerId", 1);

                    cmd.Parameters.AddWithValue("@Society", cmbSociety.SelectedValue.ToString());
                    cmd.Parameters.AddWithValue("@CustomerName", txtCustomer.Text.Trim());
                    cmd.Parameters.AddWithValue("@Flat", txtFlat.Text.Trim());
                    cmd.Parameters.AddWithValue("@Mobile", txtMobile.Text.Trim());
                    cmd.Parameters.AddWithValue("@CarModel", txtCarModel.Text.Trim());
                    cmd.Parameters.AddWithValue("@CarNumber", txtCarNumber.Text.Trim());
                    cmd.Parameters.AddWithValue("@CarType", txtCarType?.Text?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@Subscription", txtSubscription?.Text?.Trim() ?? subscriptionType);
                    cmd.Parameters.AddWithValue("@Washer", txtWasher?.Text?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@Status", (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pending");
                    cmd.Parameters.AddWithValue("@NextDueDate", nextDueDate);

                    cmd.ExecuteNonQuery();

                    MessageBox.Show($"Car subscription added successfully! ✅\nNext due date: {nextDueDate:dd-MM-yyyy}");

                    ClearForm();

                    // ✅ FIX: Refresh both orders AND stats after adding
                    LoadOrders();
                    LoadStats(); // This was missing!
                    LoadSocieties(); // Also refresh societies in case new one was added
                }
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"Database Error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void ClearForm()
        {
            cmbSociety.SelectedIndex = 0;
            txtCustomer.Text = "";
            txtFlat.Text = "";
            txtMobile.Text = "";
            txtCarModel.Text = "";
            txtCarNumber.Text = "";
            txtCarType.Text = "";
            txtSubscription.Text = "";
            txtWasher.Text = "";
            cmbSubscription.SelectedIndex = 0;
            cmbStatus.SelectedIndex = 0;
        }

        // ✅ NEW METHOD: Call this method when a new society is added from another window
        public void RefreshData()
        {
            LoadStats();
            LoadOrders();
            LoadSocieties();
        }
    }
}