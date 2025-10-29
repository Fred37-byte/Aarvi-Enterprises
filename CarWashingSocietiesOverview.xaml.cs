using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

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

            LoadOverallStats();
            LoadSocieties();
        }

        private void LoadOverallStats()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string querySocieties = "SELECT COUNT(*) FROM CarWashingSocieties";
                    int activeSocieties = Convert.ToInt32(new SqlCommand(querySocieties, conn).ExecuteScalar());

                    string queryCars = "SELECT COUNT(*) FROM CarWashingOrders WHERE Status = 'Active'";
                    int subscribedCars = Convert.ToInt32(new SqlCommand(queryCars, conn).ExecuteScalar());

                    string queryWashers = "SELECT COUNT(DISTINCT Washer) FROM CarWashingOrders WHERE Status = 'Active' AND Washer IS NOT NULL AND Washer != ''";
                    int carWashers = Convert.ToInt32(new SqlCommand(queryWashers, conn).ExecuteScalar());

                    // FIXED: Now calculates normalized monthly revenue
                    decimal monthlyRevenue = CalculateOverallMonthlyRevenue(conn);

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

        // FIXED: Now properly normalizes revenue to monthly equivalent
        private decimal CalculateOverallMonthlyRevenue(SqlConnection conn)
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
                MessageBox.Show($"Error calculating revenue: {ex.Message}");
                return 0;
            }
        }

        // Calculate rating based on cars and revenue
        private string CalculateRating(int activeCars, decimal monthlyRevenue)
        {
            if (activeCars == 0 && monthlyRevenue == 0)
                return "N/A";

            // Base rating 3.0
            double rating = 3.0;

            // Add up to 1.5 stars for cars (every 5 cars = +0.5)
            rating += Math.Min(1.5, (activeCars / 5.0) * 0.5);

            // Add up to 0.5 stars for revenue (every 2000 = +0.5)
            rating += Math.Min(0.5, ((double)monthlyRevenue / 2000.0) * 0.5);

            // Cap at 5.0
            rating = Math.Min(5.0, rating);

            // Convert to stars
            int fullStars = (int)Math.Round(rating);
            return new string('⭐', fullStars);
        }

        private void LoadSocieties()
        {
            societies.Clear();
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"SELECT SocietyId, SocietyName, Address, ContactNumber, ManagerName 
                                     FROM CarWashingSocieties 
                                     ORDER BY SocietyName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var societyId = Convert.ToInt32(reader["SocietyId"]);

                            // Get data for this society AFTER closing the reader
                            var societyName = reader["SocietyName"]?.ToString() ?? string.Empty;
                            var address = reader["Address"]?.ToString() ?? string.Empty;
                            var phone = reader["ContactNumber"]?.ToString() ?? string.Empty;
                            var managerName = reader["ManagerName"]?.ToString() ?? string.Empty;

                            societies.Add(new Society
                            {
                                SocietyId = societyId,
                                SocietyName = societyName,
                                Address = address,
                                Phone = phone,
                                ManagerName = managerName,
                                ActiveCars = 0,
                                MonthlyRevenue = "₹0",
                                Satisfaction = "N/A"
                            });
                        }
                    }

                    // Now load stats for each society
                    foreach (var society in societies)
                    {
                        int activeCars = GetActiveCarsForSociety(society.SocietyId, conn);
                        decimal monthlyRevenue = GetMonthlyRevenueForSociety(society.SocietyId, conn);

                        society.ActiveCars = activeCars;
                        society.MonthlyRevenue = "₹" + monthlyRevenue.ToString("N0");
                        society.Satisfaction = CalculateRating(activeCars, monthlyRevenue);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading societies: {ex.Message}\n\nStack: {ex.StackTrace}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetActiveCarsForSociety(int societyId, SqlConnection conn)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM CarWashingOrders WHERE SocietyId = @societyId AND Status = 'Active'";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@societyId", societyId);

                object result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting active cars for Society {societyId}: {ex.Message}");
                return 0;
            }
        }

        // FIXED: Now properly normalizes revenue to monthly equivalent for each society
        private decimal GetMonthlyRevenueForSociety(int societyId, SqlConnection conn)
        {
            try
            {
                string query = @"
                    SELECT Subscription, SubscriptionType 
                    FROM CarWashingOrders 
                    WHERE SocietyId = @societyId AND Status = 'Active'";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@societyId", societyId);
                SqlDataReader reader = cmd.ExecuteReader();

                decimal societyRevenue = 0;

                while (reader.Read())
                {
                    string subscriptionStr = reader["Subscription"]?.ToString() ?? "0";
                    string subscriptionType = reader["SubscriptionType"]?.ToString() ?? "Monthly";

                    if (decimal.TryParse(subscriptionStr, out decimal amount))
                    {
                        // Normalize to monthly revenue based on subscription type
                        switch (subscriptionType.ToLower())
                        {
                            case "monthly":
                                societyRevenue += amount;
                                break;
                            case "quarterly":
                                societyRevenue += amount / 3;
                                break;
                            case "yearly":
                                societyRevenue += amount / 12;
                                break;
                            default:
                                societyRevenue += amount;
                                break;
                        }
                    }
                }

                reader.Close();
                return societyRevenue;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating revenue for Society {societyId}: {ex.Message}");
                return 0;
            }
        }

        private void BtnAddSociety_Click(object sender, RoutedEventArgs e)
        {
            AddSocietyCarWindow addSocietyWindow = new AddSocietyCarWindow();
            if (addSocietyWindow.ShowDialog() == true)
            {
                LoadOverallStats();
                LoadSocieties();
            }
        }

        private void BtnEditSociety_Click(object sender, RoutedEventArgs e)
        {
            var society = (sender as FrameworkElement)?.DataContext as Society;
            if (society != null)
            {
                EditSociety editWindow = new EditSociety(society);
                if (editWindow.ShowDialog() == true)
                {
                    RefreshData();
                }
            }
        }

        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            var society = (sender as FrameworkElement)?.DataContext as Society;
            if (society != null)
            {
                try
                {
                    ViewdetailSocieties detailsWindow = new ViewdetailSocieties(society.SocietyId);
                    detailsWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening society details: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Unable to load society details. Please try again.", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void RefreshData()
        {
            LoadOverallStats();
            LoadSocieties();
        }
    }

    public class Society
    {
        public int SocietyId { get; set; }
        public string SocietyName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string ManagerName { get; set; }
        public int ActiveCars { get; set; }
        public string MonthlyRevenue { get; set; }
        public string Satisfaction { get; set; }
    }
}