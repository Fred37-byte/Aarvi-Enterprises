using NewCustomerWindow.xaml;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
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

        // ---------- helpers to resolve/create a real CustomerId + popup info ----------
        private static string Normalize(string s) => (s ?? string.Empty).Trim();

        private int FindExistingCustomerId(SqlConnection conn, string name, string phone)
        {
            using (var cmd = new SqlCommand(@"
                SELECT TOP 1 Id
                FROM Customers
                WHERE
                    (Phone = @Phone AND @Phone IS NOT NULL AND LTRIM(RTRIM(@Phone)) <> '')
                 OR (LOWER(LTRIM(RTRIM(FullName))) = LOWER(LTRIM(RTRIM(@Name))) AND @Name IS NOT NULL AND LTRIM(RTRIM(@Name)) <> '')
                ORDER BY CASE WHEN Phone = @Phone THEN 0 ELSE 1 END;", conn))
            {
                cmd.Parameters.AddWithValue("@Phone", (object)(phone ?? (object)DBNull.Value) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Name", (object)(name ?? (object)DBNull.Value) ?? DBNull.Value);

                var result = cmd.ExecuteScalar();
                return (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
            }
        }

        private int CreateCustomer(SqlConnection conn, string name, string phone)
        {
            using (var cmd = new SqlCommand(@"
                INSERT INTO Customers (FullName, Email, Phone, Address, City, State, ZipCode, ServiceType)
                OUTPUT INSERTED.Id
                VALUES (@FullName, @Email, @Phone, @Address, @City, @State, @ZipCode, @ServiceType);", conn))
            {
                cmd.Parameters.AddWithValue("@FullName", name);
                cmd.Parameters.AddWithValue("@Email", $"{name.Replace(" ", "").ToLower()}@mail.com");
                cmd.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);
                cmd.Parameters.AddWithValue("@Address", "Auto Added");
                cmd.Parameters.AddWithValue("@City", "Thane");
                cmd.Parameters.AddWithValue("@State", "Maharashtra");
                cmd.Parameters.AddWithValue("@ZipCode", "400607");
                cmd.Parameters.AddWithValue("@ServiceType", "Aarvi Car Washing Service");

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private string GetCustomerFullNameById(SqlConnection conn, int id)
        {
            using (var cmd = new SqlCommand("SELECT FullName FROM Customers WHERE Id = @Id", conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                var o = cmd.ExecuteScalar();
                return (o == null || o == DBNull.Value) ? "" : o.ToString();
            }
        }

        // returns: (id, createdNew, canonicalFullName)
        private (int id, bool created, string fullName) EnsureCustomer(SqlConnection conn, string typedName, string typedPhone)
        {
            string name = Normalize(typedName);
            string phone = Normalize(typedPhone);

            int id = FindExistingCustomerId(conn, name, phone);
            if (id > 0)
            {
                string fullName = GetCustomerFullNameById(conn, id);
                return (id, false, string.IsNullOrWhiteSpace(fullName) ? name : fullName);
            }

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Customer name is required to create a new customer.");

            id = CreateCustomer(conn, name, phone);
            string createdName = GetCustomerFullNameById(conn, id);
            return (id, true, string.IsNullOrWhiteSpace(createdName) ? name : createdName);
        }
        // ---------- end helpers ----------

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

                    string query = @"
                        SELECT 
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

                    // computed Plan column
                    subscriptionsData.Columns.Add("Plan", typeof(string));
                    foreach (DataRow row in subscriptionsData.Rows)
                    {
                        string amount = row["SubscriptionAmount"]?.ToString() ?? "0";
                        string type = row["SubscriptionType"]?.ToString();

                        if (string.IsNullOrWhiteSpace(type))
                        {
                            if (decimal.TryParse(amount, out decimal v))
                                type = v <= 300 ? "Monthly" : (v <= 700 ? "Quarterly" : "Yearly");
                            else
                                type = "Monthly";
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

                    string querySocieties = "SELECT COUNT(*) FROM CarWashingSocieties";
                    int activeSocieties = Convert.ToInt32(new SqlCommand(querySocieties, conn).ExecuteScalar());

                    string queryCars = "SELECT COUNT(*) FROM CarWashingOrders WHERE Status = 'Active'";
                    int subscribedCars = Convert.ToInt32(new SqlCommand(queryCars, conn).ExecuteScalar());

                    string queryWashers = "SELECT COUNT(DISTINCT Washer) FROM CarWashingOrders WHERE Washer IS NOT NULL AND Washer <> '' AND Status = 'Active'";
                    int carWashers = Convert.ToInt32(new SqlCommand(queryWashers, conn).ExecuteScalar());

                    decimal monthlyRevenue = CalculateMonthlyRevenue(conn);

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

        private decimal CalculateMonthlyRevenue(SqlConnection conn)
        {
            try
            {
                string query = @"
                    SELECT Subscription, SubscriptionType 
                    FROM CarWashingOrders 
                    WHERE Status = 'Active'";

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    decimal monthlyRevenue = 0;

                    while (reader.Read())
                    {
                        string subscriptionStr = reader["Subscription"]?.ToString() ?? "0";
                        string subscriptionType = reader["SubscriptionType"]?.ToString() ?? "Monthly";

                        if (decimal.TryParse(subscriptionStr, out decimal amount))
                        {
                            switch (subscriptionType.ToLower())
                            {
                                case "monthly": monthlyRevenue += amount; break;
                                case "quarterly": monthlyRevenue += amount / 3m; break;
                                case "yearly": monthlyRevenue += amount / 12m; break;
                                default: monthlyRevenue += amount; break;
                            }
                        }
                    }

                    return monthlyRevenue;
                }
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
            var overviewWindow = new CarWashingSocietiesOverview();
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
                case "monthly": return today.AddMonths(1);
                case "quarterly": return today.AddMonths(3);
                case "yearly": return today.AddYears(1);
                default: return today.AddMonths(1);
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

                    // ✅ Resolve a real CustomerId (find or create) + show popup
                    var resolved = EnsureCustomer(conn, txtCustomer.Text.Trim(), txtMobile.Text.Trim());
                    int customerId = resolved.id;

                    if (resolved.created)
                        MessageBox.Show($"Customer \"{resolved.fullName}\" was added and used (ID: {customerId}).",
                                        "Customer Created",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        MessageBox.Show($"Customer \"{resolved.fullName}\" found and used (ID: {customerId}).",
                                        "Customer Selected",
                                        MessageBoxButton.OK, MessageBoxImage.Information);

                    string query = @"
                        INSERT INTO CarWashingOrders 
                            (InvoiceId, CustomerId, Society, SocietyId, CustomerName, Flat, Mobile, CarModel, CarNumber, CarType, Subscription, SubscriptionType, Washer, Status, NextDueDate, OrderDate)
                        VALUES
                            (@InvoiceId, @CustomerId, @Society, @SocietyId, @CustomerName, @Flat, @Mobile, @CarModel, @CarNumber, @CarType, @Subscription, @SubscriptionType, @Washer, @Status, @NextDueDate, @OrderDate)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@InvoiceId", 1); // kept as-is (invoice generated later)
                        cmd.Parameters.AddWithValue("@CustomerId", customerId); // ✅ no more 1
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
                    }

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
