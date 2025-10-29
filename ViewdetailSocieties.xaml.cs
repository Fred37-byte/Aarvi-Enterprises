using System;
using System.ComponentModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Media;

namespace NewCustomerWindow.xaml
{
    public partial class ViewdetailSocieties : Window, INotifyPropertyChanged
    {
        // Database connection string - using ConfigurationManager
        private string connectionString;
        private int _currentSocietyId; // ✅ ADDED: Store the current society ID

        // Properties for binding data
        private string _locationName;
        private string _serviceType;
        private string _serviceDescription;
        private string _address;
        private string _phone;
        private string _manager;
        private string _hours;
        private int _activeCars;
        private decimal _todayRevenue;
        private double _avgRating;
        private DateTime _serviceSince;
        private bool _isServiceActive;

        public string LocationName { get => _locationName; set { _locationName = value; OnPropertyChanged(nameof(LocationName)); } }
        public string ServiceType { get => _serviceType; set { _serviceType = value; OnPropertyChanged(nameof(ServiceType)); } }
        public string ServiceDescription { get => _serviceDescription; set { _serviceDescription = value; OnPropertyChanged(nameof(ServiceDescription)); } }
        public string Address { get => _address; set { _address = value; OnPropertyChanged(nameof(Address)); } }
        public string Phone { get => _phone; set { _phone = value; OnPropertyChanged(nameof(Phone)); } }
        public string Manager { get => _manager; set { _manager = value; OnPropertyChanged(nameof(Manager)); } }
        public string Hours { get => _hours; set { _hours = value; OnPropertyChanged(nameof(Hours)); } }
        public int ActiveCars { get => _activeCars; set { _activeCars = value; OnPropertyChanged(nameof(ActiveCars)); } }
        public decimal TodayRevenue { get => _todayRevenue; set { _todayRevenue = value; OnPropertyChanged(nameof(TodayRevenue)); } }
        public double AvgRating { get => _avgRating; set { _avgRating = value; OnPropertyChanged(nameof(AvgRating)); } }
        public DateTime ServiceSince { get => _serviceSince; set { _serviceSince = value; OnPropertyChanged(nameof(ServiceSince)); } }
        public bool IsServiceActive { get => _isServiceActive; set { _isServiceActive = value; OnPropertyChanged(nameof(IsServiceActive)); UpdateStatusDisplay(); } }

        public ViewdetailSocieties()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;
        }

        public ViewdetailSocieties(int societyId) : this()
        {
            _currentSocietyId = societyId; // ✅ ADDED: Store society ID
            LoadServiceData(societyId);
        }

        private void UpdateUI()
        {
            try
            {
                LocationNameText.Text = string.IsNullOrEmpty(LocationName) ? "Unknown Location" : LocationName;
                ServiceTypeText.Text = string.IsNullOrEmpty(ServiceType) ? "Unknown Service" : ServiceType;
                ServiceDescriptionText.Text = string.IsNullOrEmpty(ServiceDescription) ? "No description available" : ServiceDescription;
                AddressText.Text = string.IsNullOrEmpty(Address) ? "No address provided" : Address;
                PhoneText.Text = string.IsNullOrEmpty(Phone) ? "No phone provided" : Phone;
                ManagerText.Text = string.IsNullOrEmpty(Manager) ? "No manager assigned" : Manager;
                HoursText.Text = string.IsNullOrEmpty(Hours) ? "Hours not specified" : Hours;

                StatsActiveCarsText.Text = ActiveCars.ToString();
                ActiveCarsText.Text = ActiveCars.ToString();
                RevenueText.Text = $"₹{TodayRevenue:N0}";

                AvgRatingText.Text = AvgRating > 0 ? AvgRating.ToString("F1") : "N/A";
                RatingText.Text = AvgRating > 0 ? AvgRating.ToString("F1") : "N/A";

                ServiceSinceText.Text = ServiceSince != DateTime.MinValue ? ServiceSince.ToString("MMM dd, yyyy") : "N/A";

                UpdateStatusDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating UI: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatusDisplay()
        {
            if (IsServiceActive)
            {
                StatusBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1FAE5"));
                StatusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF10B981"));
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF065F46"));
                StatusText.Text = "Service Active";
                StatusDescriptionText.Text = "Ready to serve customers";
            }
            else
            {
                StatusBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFEF2F2"));
                StatusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEF4444"));
                StatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF991B1B"));
                StatusText.Text = "Service Inactive";
                StatusDescriptionText.Text = "Currently not accepting customers";
            }
        }

        private double CalculateRating(int activeCars, decimal monthlyRevenue)
        {
            if (activeCars == 0 && monthlyRevenue == 0)
                return 0;

            double rating = 3.0;
            rating += Math.Min(1.5, (activeCars / 5.0) * 0.5);
            rating += Math.Min(0.5, ((double)monthlyRevenue / 2000.0) * 0.5);

            return Math.Min(5.0, rating);
        }

        private decimal GetMonthlyEquivalentRate(string subscriptionType)
        {
            switch (subscriptionType.ToLower())
            {
                case "monthly":
                    return 500;
                case "quarterly":
                    return 450;
                case "yearly":
                    return 416.67m;
                case "premium":
                    return 800;
                case "basic":
                    return 300;
                default:
                    if (decimal.TryParse(subscriptionType, out decimal amount))
                        return amount;
                    return 500;
            }
        }

        public void LoadServiceData(int societyId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Get society details
                    string societyQuery = @"SELECT SocietyName, Address, ContactNumber, ManagerName, CreatedDate 
                                           FROM CarWashingSocieties WHERE SocietyId = @Id";

                    SqlCommand cmd = new SqlCommand(societyQuery, conn);
                    cmd.Parameters.AddWithValue("@Id", societyId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            LocationName = reader["SocietyName"]?.ToString() ?? "Unknown Society";
                            Address = reader["Address"]?.ToString() ?? "Address not provided";
                            Phone = reader["ContactNumber"]?.ToString() ?? "Contact not available";
                            Manager = reader["ManagerName"]?.ToString() ?? "No manager assigned";
                            ServiceSince = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : DateTime.MinValue;

                            ServiceType = "Car Washing Service";
                            ServiceDescription = "Professional car washing and detailing services for society members";
                            Hours = "9:00 AM - 6:00 PM";
                            IsServiceActive = true;
                        }
                        else
                        {
                            MessageBox.Show("No society found with this ID.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                            this.Close();
                            return;
                        }
                    }

                    // Get active cars count using SocietyId
                    string activeCarQuery = "SELECT COUNT(*) FROM CarWashingOrders WHERE SocietyId = @societyId AND Status = 'Active'";
                    SqlCommand activeCmd = new SqlCommand(activeCarQuery, conn);
                    activeCmd.Parameters.AddWithValue("@societyId", societyId);
                    ActiveCars = Convert.ToInt32(activeCmd.ExecuteScalar() ?? 0);

                    // Get monthly revenue using SocietyId - calculate from subscription types
                    string revenueQuery = @"
                        SELECT Subscription, COUNT(*) as Count 
                        FROM CarWashingOrders 
                        WHERE SocietyId = @societyId AND Status = 'Active'
                        GROUP BY Subscription";

                    SqlCommand revenueCmd = new SqlCommand(revenueQuery, conn);
                    revenueCmd.Parameters.AddWithValue("@societyId", societyId);

                    decimal totalRevenue = 0;
                    using (SqlDataReader revenueReader = revenueCmd.ExecuteReader())
                    {
                        while (revenueReader.Read())
                        {
                            string subscriptionType = revenueReader["Subscription"]?.ToString()?.ToLower() ?? "";
                            int count = Convert.ToInt32(revenueReader["Count"]);
                            decimal monthlyRate = GetMonthlyEquivalentRate(subscriptionType);
                            totalRevenue += monthlyRate * count;
                        }
                    }

                    TodayRevenue = totalRevenue;

                    // Calculate rating
                    AvgRating = CalculateRating(ActiveCars, TodayRevenue);
                }

                UpdateUI();
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"Database error: {sqlEx.Message}\n\nDetails: {sqlEx.ToString()}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\nStack: {ex.StackTrace}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ✅ ADDED: Click handler to open car list window
        private void ActiveCars_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ActiveCars > 0)
            {
                CarListWindow carListWindow = new CarListWindow(_currentSocietyId, LocationName);
                carListWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("No active cars available for this society.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            if (this.IsLoaded)
                Dispatcher.BeginInvoke(new Action(() => UpdateUI()));
        }
    }
}