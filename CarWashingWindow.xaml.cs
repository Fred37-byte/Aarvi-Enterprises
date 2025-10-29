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
        private DataTable subscriptionsData;
        private DataView filteredResults;

        public CarWashingWindow()
        {
            InitializeComponent();
            LoadStats();
            LoadOrders();
            LoadSocieties();
        }

        private void LoadSocieties()
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT SocietyId, SocietyName FROM CarWashingSocieties ORDER BY SocietyName";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    cmbSociety.ItemsSource = dt.DefaultView;
                    cmbSociety.DisplayMemberPath = "SocietyName";
                    cmbSociety.SelectedValuePath = "SocietyId";
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
                        Subscription as SubscriptionAmount,
                        SubscriptionType,
                        Washer,
                        Status,
                        FORMAT(OrderDate, 'dd-MM-yyyy') as StartDate,
                        FORMAT(NextDueDate, 'dd-MM-yyyy') as NextDue
                        FROM CarWashingOrders 
                        ORDER BY CarInvoiceId ASC";

                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    subscriptionsData = new DataTable();
                    da.Fill(subscriptionsData);

                    // Add computed column for Plan display using SubscriptionType
                    subscriptionsData.Columns.Add("Plan", typeof(string));

                    foreach (DataRow row in subscriptionsData.Rows)
                    {
                        string amount = row["SubscriptionAmount"]?.ToString() ?? "0";
                        string type = row["SubscriptionType"]?.ToString();

                        // If SubscriptionType is NULL or empty, determine from amount (for old records)
                        if (string.IsNullOrWhiteSpace(type))
                        {
                            if (decimal.TryParse(amount, out decimal amountValue))
                            {
                                if (amountValue <= 300)
                                    type = "Monthly";
                                else if (amountValue <= 700)
                                    type = "Quarterly";
                                else
                                    type = "Yearly";
                            }
                            else
                            {
                                type = "Monthly";
                            }
                        }

                        row["Plan"] = $"₹{amount} ({type})";
                    }

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
                    string queryCars = "SELECT COUNT(*) FROM CarWashingOrders WHERE Status = 'Active'";
                    SqlCommand cmdCars = new SqlCommand(queryCars, conn);
                    int subscribedCars = Convert.ToInt32(cmdCars.ExecuteScalar());

                    // 3. Car Washers (count unique washers)
                    string queryWashers = "SELECT COUNT(DISTINCT Washer) FROM CarWashingOrders WHERE Washer IS NOT NULL AND Washer != '' AND Status = 'Active'";
                    SqlCommand cmdWashers = new SqlCommand(queryWashers, conn);
                    int carWashers = Convert.ToInt32(cmdWashers.ExecuteScalar());

                    // 4. Monthly Revenue (normalized MRR) - FIXED CALCULATION
                    decimal monthlyRevenue = CalculateMonthlyRevenue(conn);

                    // Update UI
                    txtActiveSocieties.Text = activeSocieties.ToString();
                    txtSubscribedCars.Text = subscribedCars.ToString();
                    txtCarWashers.Text = carWashers.ToString();
                    txtRevenue.Text = "₹" + monthlyRevenue.ToString("N0");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading stats: " + ex.Message);
                }
            }
        }

        // FIXED: This method now matches the calculation in CarWashingSocietiesOverview
        private decimal CalculateMonthlyRevenue(SqlConnection conn)
        {
            try
            {
                // Read ACTUAL subscription amounts and types from database
                string query = @"
                    SELECT Subscription, SubscriptionType 
                    FROM CarWashingOrders 
                    WHERE Status = 'Active'";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                decimal monthlyRevenue = 0;

                while (reader.Read())
                {
                    // Get the actual subscription amount from database
                    string subscriptionStr = reader["Subscription"]?.ToString() ?? "0";
                    string subscriptionType = reader["SubscriptionType"]?.ToString() ?? "Monthly";

                    // Parse the actual amount
                    if (decimal.TryParse(subscriptionStr, out decimal amount))
                    {
                        // Normalize to monthly revenue based on subscription type
                        switch (subscriptionType.ToLower())
                        {
                            case "monthly":
                                monthlyRevenue += amount; // Already monthly
                                break;
                            case "quarterly":
                                monthlyRevenue += amount / 3; // Divide by 3 months
                                break;
                            case "yearly":
                                monthlyRevenue += amount / 12; // Divide by 12 months
                                break;
                            default:
                                monthlyRevenue += amount; // Default to monthly
                                break;
                        }
                    }
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

                // Remove ₹ symbol if user typed it
                string cleanQuery = escapedQuery.Replace("₹", "");

                filteredResults.RowFilter = string.Format(
                    "Convert(Customer, 'System.String') LIKE '%{0}%' OR " +
                    "Convert(Society, 'System.String') LIKE '%{0}%' OR " +
                    "Convert(CarDetails, 'System.String') LIKE '%{0}%' OR " +
                    "Convert(Plan, 'System.String') LIKE '%{0}%' OR " +
                    "Convert(SubscriptionAmount, 'System.String') LIKE '%{1}%' OR " +
                    "Convert(Washer, 'System.String') LIKE '%{0}%' OR " +
                    "Convert(Status, 'System.String') LIKE '%{0}%' OR " +
                    "Convert(StartDate, 'System.String') LIKE '%{0}%' OR " +
                    "Convert(NextDue, 'System.String') LIKE '%{0}%'",
                    escapedQuery, cleanQuery
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

                    var selectedSociety = cmbSociety.SelectedItem as DataRowView;
                    if (selectedSociety == null)
                    {
                        MessageBox.Show("Please select a society");
                        return;
                    }

                    string societyName = selectedSociety["SocietyName"].ToString();
                    int societyId = Convert.ToInt32(selectedSociety["SocietyId"]);

                    string query = @"INSERT INTO CarWashingOrders 
                        (InvoiceId, CustomerId, Society, SocietyId, CustomerName, Flat, Mobile, CarModel, CarNumber, CarType, Subscription, SubscriptionType, Washer, Status, NextDueDate, OrderDate)
                        VALUES (@InvoiceId, @CustomerId, @Society, @SocietyId, @CustomerName, @Flat, @Mobile, @CarModel, @CarNumber, @CarType, @Subscription, @SubscriptionType, @Washer, @Status, @NextDueDate, @OrderDate)";

                    SqlCommand cmd = new SqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@InvoiceId", 1);
                    cmd.Parameters.AddWithValue("@CustomerId", 1);
                    cmd.Parameters.AddWithValue("@Society", societyName);
                    cmd.Parameters.AddWithValue("@SocietyId", societyId);
                    cmd.Parameters.AddWithValue("@CustomerName", txtCustomer.Text.Trim());
                    cmd.Parameters.AddWithValue("@Flat", txtFlat.Text.Trim());
                    cmd.Parameters.AddWithValue("@Mobile", txtMobile.Text.Trim());
                    cmd.Parameters.AddWithValue("@CarModel", txtCarModel.Text.Trim());
                    cmd.Parameters.AddWithValue("@CarNumber", txtCarNumber.Text.Trim());
                    cmd.Parameters.AddWithValue("@CarType", txtCarType?.Text?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@Subscription", txtSubscription?.Text?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@SubscriptionType", subscriptionType);
                    cmd.Parameters.AddWithValue("@Washer", txtWasher?.Text?.Trim() ?? "");
                    cmd.Parameters.AddWithValue("@Status", (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Pending");
                    cmd.Parameters.AddWithValue("@NextDueDate", nextDueDate);
                    cmd.Parameters.AddWithValue("@OrderDate", DateTime.Today);

                    cmd.ExecuteNonQuery();

                    MessageBox.Show($"Car subscription added successfully!\nNext due date: {nextDueDate:dd-MM-yyyy}");

                    ClearForm();
                    LoadOrders();
                    LoadStats();
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

        public void RefreshData()
        {
            LoadStats();
            LoadOrders();
            LoadSocieties();
        }
    }
}