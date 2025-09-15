using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace NewCustomerWindow.xaml
{
    public partial class CarWashingSocietiesOverview : Window
    {
        private ObservableCollection<Society> societies;
        private string connectionString;

        public CarWashingSocietiesOverview()
        {
            InitializeComponent();
            societies = new ObservableCollection<Society>();
            SocietyList.ItemsSource = societies;
            connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            LoadOverallStats(); // Load dashboard stats
            LoadSocieties();    // Load society cards
        }

        // ✅ NEW METHOD: Load overall dashboard statistics
        private void LoadOverallStats()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // 1. Count Active Societies
                    string querySocieties = "SELECT COUNT(*) FROM CarWashingSocieties";
                    SqlCommand cmdSocieties = new SqlCommand(querySocieties, conn);
                    int activeSocieties = Convert.ToInt32(cmdSocieties.ExecuteScalar());

                    // 2. Count Active Subscribed Cars
                    string queryCars = "SELECT COUNT(*) FROM CarWashingOrders WHERE Status = 'Active'";
                    SqlCommand cmdCars = new SqlCommand(queryCars, conn);
                    int subscribedCars = Convert.ToInt32(cmdCars.ExecuteScalar());

                    // 3. Count Car Washers (unique active washers)
                    string queryWashers = "SELECT COUNT(DISTINCT Washer) FROM CarWashingOrders WHERE Status = 'Active' AND Washer IS NOT NULL AND Washer != ''";
                    SqlCommand cmdWashers = new SqlCommand(queryWashers, conn);
                    int carWashers = Convert.ToInt32(cmdWashers.ExecuteScalar());

                    // 4. Calculate Monthly Revenue from Active subscriptions
                    decimal monthlyRevenue = CalculateOverallMonthlyRevenue(conn);

                    // Update UI controls
                    txtActiveSocieties.Text = activeSocieties.ToString();
                    txtSubscribedCars.Text = subscribedCars.ToString();
                    txtCarWashers.Text = carWashers.ToString();
                    txtRevenue.Text = "₹" + monthlyRevenue.ToString("N0");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading overall stats: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ NEW METHOD: Calculate overall monthly revenue
        private decimal CalculateOverallMonthlyRevenue(SqlConnection conn)
        {
            try
            {
                string query = @"
                    SELECT Subscription, COUNT(*) as Count 
                    FROM CarWashingOrders 
                    WHERE Status = 'Active'
                    GROUP BY Subscription";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                decimal totalRevenue = 0;

                while (reader.Read())
                {
                    string subscriptionType = reader["Subscription"]?.ToString()?.ToLower() ?? "";
                    int count = Convert.ToInt32(reader["Count"]);
                    decimal monthlyRate = GetMonthlyEquivalentRate(subscriptionType);
                    totalRevenue += monthlyRate * count;
                }

                reader.Close();
                return totalRevenue;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating revenue: {ex.Message}");
                return 0;
            }
        }

        // ✅ NEW METHOD: Get monthly equivalent rate for subscription types
        private decimal GetMonthlyEquivalentRate(string subscriptionType)
        {
            switch (subscriptionType.ToLower())
            {
                case "monthly":
                    return 500; // ₹500 per month
                case "quarterly":
                    return 1350 / 3; // ₹450 per month
                case "yearly":
                    return 5000 / 12; // ₹417 per month
                case "premium":
                    return 800; // ₹800 per month
                case "basic":
                    return 300; // ₹300 per month
                default:
                    if (decimal.TryParse(subscriptionType, out decimal amount))
                        return amount;
                    return 500; // Default rate
            }
        }

        // ✅ ENHANCED METHOD: Load societies with real calculated data
        private void LoadSocieties()
        {
            societies.Clear();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Get basic society info
                    string query = @"SELECT SocietyId, SocietyName, Address, ContactNumber, ManagerName 
                                     FROM CarWashingSocieties 
                                     ORDER BY SocietyName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string societyName = reader["SocietyName"].ToString();

                            societies.Add(new Society
                            {
                                SocietyName = societyName,
                                Address = reader["Address"].ToString(),
                                Phone = reader["ContactNumber"].ToString(),
                                ManagerName = reader["ManagerName"].ToString(),
                                ActiveCars = GetActiveCarsForSociety(societyName),
                                MonthlyRevenue = "₹" + GetMonthlyRevenueForSociety(societyName).ToString("N0"),
                                Satisfaction = CalculateSatisfactionForSociety(societyName)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while loading societies: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ✅ NEW METHOD: Get active cars count for specific society
        private int GetActiveCarsForSociety(string societyName)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM CarWashingOrders WHERE Society = @society AND Status = 'Active'";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@society", societyName);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting active cars for {societyName}: {ex.Message}");
                return 0;
            }
        }

        // ✅ NEW METHOD: Get monthly revenue for specific society
        private decimal GetMonthlyRevenueForSociety(string societyName)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT Subscription, COUNT(*) as Count 
                        FROM CarWashingOrders 
                        WHERE Society = @society AND Status = 'Active'
                        GROUP BY Subscription";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@society", societyName);
                    SqlDataReader reader = cmd.ExecuteReader();

                    decimal societyRevenue = 0;

                    while (reader.Read())
                    {
                        string subscriptionType = reader["Subscription"]?.ToString()?.ToLower() ?? "";
                        int count = Convert.ToInt32(reader["Count"]);
                        decimal monthlyRate = GetMonthlyEquivalentRate(subscriptionType);
                        societyRevenue += monthlyRate * count;
                    }

                    reader.Close();
                    return societyRevenue;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating revenue for {societyName}: {ex.Message}");
                return 0;
            }
        }

        // ✅ NEW METHOD: Calculate satisfaction rating for society (Fixed DBNull handling)
        private string CalculateSatisfactionForSociety(string societyName)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Get total cars and active cars ratio for satisfaction
                    string query = @"
                        SELECT 
                            COUNT(*) as TotalCars,
                            SUM(CASE WHEN Status = 'Active' THEN 1 ELSE 0 END) as ActiveCars
                        FROM CarWashingOrders 
                        WHERE Society = @society";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@society", societyName);
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        // ✅ FIX: Handle DBNull values properly
                        int totalCars = reader["TotalCars"] != DBNull.Value ? Convert.ToInt32(reader["TotalCars"]) : 0;
                        int activeCars = reader["ActiveCars"] != DBNull.Value ? Convert.ToInt32(reader["ActiveCars"]) : 0;

                        reader.Close();

                        if (totalCars == 0) return "N/A";

                        // Calculate satisfaction based on active/total ratio
                        double satisfactionRatio = (double)activeCars / totalCars;

                        if (satisfactionRatio >= 0.9) return "⭐⭐⭐⭐⭐";
                        if (satisfactionRatio >= 0.8) return "⭐⭐⭐⭐";
                        if (satisfactionRatio >= 0.7) return "⭐⭐⭐";
                        if (satisfactionRatio >= 0.6) return "⭐⭐";
                        return "⭐";
                    }

                    reader.Close();
                    return "N/A";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating satisfaction for {societyName}: {ex.Message}");
                return "N/A";
            }
        }

        // ✅ ENHANCED METHOD: Add society and refresh data
        private void BtnAddSociety_Click(object sender, RoutedEventArgs e)
        {
            AddSocietyCarWindow addSocietyWindow = new AddSocietyCarWindow();
            if (addSocietyWindow.ShowDialog() == true)
            {
                // Refresh both stats and society cards after adding
                LoadOverallStats();
                LoadSocieties();
            }
        }

        // ✅ NEW METHOD: Public method to refresh data from external calls
        public void RefreshData()
        {
            LoadOverallStats();
            LoadSocieties();
        }
    }

    // ✅ ENHANCED Society model
    public class Society
    {
        public string SocietyName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string ManagerName { get; set; }
        public int ActiveCars { get; set; }
        public string MonthlyRevenue { get; set; }
        public string Satisfaction { get; set; }
    }
} 