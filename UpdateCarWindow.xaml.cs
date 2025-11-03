using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace NewCustomerWindow.xaml
{
    public partial class UpdateCarWindow : Window
    {
        private string connectionString;
        private int carInvoiceId;
        private int societyId;
        private string originalStatus;

        public bool DataUpdated { get; private set; } = false;

        public UpdateCarWindow(int carInvoiceId, int societyId)
        {
            InitializeComponent();
            this.carInvoiceId = carInvoiceId;
            this.societyId = societyId;
            connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            LoadCarData();
        }

        private void LoadCarData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT CustomerName, CarModel, CarNumber, 
                                   Subscription, SubscriptionType, Washer, Status, 
                                   NextDueDate, OrderDate 
                                   FROM CarWashingOrders 
                                   WHERE CarInvoiceId = @carInvoiceId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@carInvoiceId", carInvoiceId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Load customer info
                                CustomerNameTextBox.Text = reader["CustomerName"]?.ToString() ?? "";

                                // Load car info
                                CarModelTextBox.Text = reader["CarModel"]?.ToString() ?? "";
                                CarNumberTextBox.Text = reader["CarNumber"]?.ToString() ?? "";

                                // Load subscription info
                                SubscriptionTextBox.Text = reader["Subscription"]?.ToString() ?? "0";
                                string subType = reader["SubscriptionType"]?.ToString() ?? "Monthly";
                                SelectComboBoxItem(SubscriptionTypeComboBox, subType);

                                // Load assignment & status
                                WasherTextBox.Text = reader["Washer"]?.ToString() ?? "";
                                string status = reader["Status"]?.ToString() ?? "Active";
                                originalStatus = status;
                                SelectComboBoxItem(StatusComboBox, status);

                                // Load dates
                                if (reader["OrderDate"] != DBNull.Value)
                                {
                                    StartDatePicker.SelectedDate = Convert.ToDateTime(reader["OrderDate"]);
                                }

                                if (reader["NextDueDate"] != DBNull.Value)
                                {
                                    NextDueDatePicker.SelectedDate = Convert.ToDateTime(reader["NextDueDate"]);
                                }

                                // Update header info
                                CarInfoText.Text = $"{CarModelTextBox.Text} • {CarNumberTextBox.Text}";

                                // Calculate MRR
                                UpdateMRRPreview();
                            }
                            else
                            {
                                MessageBox.Show("Car record not found!", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                this.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading car data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void SelectComboBoxItem(ComboBox comboBox, string value)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void UpdateMRRPreview()
        {
            // Add null checks - window might not be fully loaded yet
            if (MRRPreviewText == null || SubscriptionTextBox == null || SubscriptionTypeComboBox == null)
                return;

            if (decimal.TryParse(SubscriptionTextBox.Text, out decimal amount))
            {
                string subType = (SubscriptionTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Monthly";
                decimal mrr = CalculateMRR(amount, subType);
                MRRPreviewText.Text = $"₹{mrr:N0}/month";
            }
            else
            {
                MRRPreviewText.Text = "₹0/month";
            }
        }

        private decimal CalculateMRR(decimal amount, string subscriptionType)
        {
            switch (subscriptionType.ToLower())
            {
                case "monthly":
                    return amount;
                case "quarterly":
                    return amount / 3;
                case "yearly":
                    return amount / 12;
                default:
                    return amount;
            }
        }

        private void SubscriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateMRRPreview();
        }

        private void SubscriptionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMRRPreview();
            AutoCalculateNextDueDate();
        }

        private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            AutoCalculateNextDueDate();
        }

        private void AutoCalculateNextDueDate()
        {
            // Add null checks - controls might not be loaded yet
            if (StartDatePicker == null || NextDueDatePicker == null || SubscriptionTypeComboBox == null)
                return;

            if (StartDatePicker.SelectedDate.HasValue)
            {
                DateTime startDate = StartDatePicker.SelectedDate.Value;
                string subType = (SubscriptionTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Monthly";

                DateTime nextDue;
                switch (subType.ToLower())
                {
                    case "monthly":
                        nextDue = startDate.AddMonths(1);
                        break;
                    case "quarterly":
                        nextDue = startDate.AddMonths(3);
                        break;
                    case "yearly":
                        nextDue = startDate.AddYears(1);
                        break;
                    default:
                        nextDue = startDate.AddMonths(1);
                        break;
                }

                NextDueDatePicker.SelectedDate = nextDue;
            }
        }

        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Add null check
            if (StatusWarning == null)
                return;

            string selectedStatus = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Active";

            if (originalStatus == "Active" && selectedStatus == "Inactive")
            {
                StatusWarning.Visibility = Visibility.Visible;
            }
            else
            {
                StatusWarning.Visibility = Visibility.Collapsed;
            }
        }

        private bool ValidateFields()
        {
            bool isValid = true;

            // Validate Customer Name
            if (string.IsNullOrWhiteSpace(CustomerNameTextBox.Text))
            {
                CustomerNameError.Visibility = Visibility.Visible;
                CustomerNameTextBox.BorderBrush = System.Windows.Media.Brushes.Red;
                isValid = false;
            }
            else
            {
                CustomerNameError.Visibility = Visibility.Collapsed;
                CustomerNameTextBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(209, 213, 219));
            }

            // Validate Car Model
            if (string.IsNullOrWhiteSpace(CarModelTextBox.Text))
            {
                CarModelError.Visibility = Visibility.Visible;
                CarModelTextBox.BorderBrush = System.Windows.Media.Brushes.Red;
                isValid = false;
            }
            else
            {
                CarModelError.Visibility = Visibility.Collapsed;
                CarModelTextBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(209, 213, 219));
            }

            // Validate Car Number
            if (string.IsNullOrWhiteSpace(CarNumberTextBox.Text))
            {
                CarNumberError.Visibility = Visibility.Visible;
                CarNumberTextBox.BorderBrush = System.Windows.Media.Brushes.Red;
                isValid = false;
            }
            else
            {
                CarNumberError.Visibility = Visibility.Collapsed;
                CarNumberTextBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(209, 213, 219));
            }

            // Validate Subscription Amount
            if (!decimal.TryParse(SubscriptionTextBox.Text, out decimal amount) || amount <= 0)
            {
                SubscriptionError.Visibility = Visibility.Visible;
                SubscriptionTextBox.BorderBrush = System.Windows.Media.Brushes.Red;
                isValid = false;
            }
            else
            {
                SubscriptionError.Visibility = Visibility.Collapsed;
                SubscriptionTextBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(209, 213, 219));
            }

            // Validate Next Due Date
            if (!NextDueDatePicker.SelectedDate.HasValue)
            {
                DueDateError.Visibility = Visibility.Visible;
                isValid = false;
            }
            else
            {
                DueDateError.Visibility = Visibility.Collapsed;
            }

            return isValid;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
            {
                MessageBox.Show("Please fill in all required fields correctly.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Confirmation dialog
            var result = MessageBox.Show(
                $"Are you sure you want to update this subscription?\n\n" +
                $"Customer: {CustomerNameTextBox.Text}\n" +
                $"Car: {CarModelTextBox.Text} • {CarNumberTextBox.Text}\n" +
                $"Amount: ₹{SubscriptionTextBox.Text}",
                "Confirm Update",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                SaveChanges();
            }
        }

        private void SaveChanges()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"UPDATE CarWashingOrders 
                                   SET CustomerName = @customerName,
                                       CarModel = @carModel,
                                       CarNumber = @carNumber,
                                       Subscription = @subscription,
                                       SubscriptionType = @subscriptionType,
                                       Washer = @washer,
                                       Status = @status,
                                       NextDueDate = @nextDueDate,
                                       OrderDate = @orderDate
                                   WHERE CarInvoiceId = @carInvoiceId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@customerName", CustomerNameTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@carModel", CarModelTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@carNumber", CarNumberTextBox.Text.Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@subscription", SubscriptionTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@subscriptionType",
                            (SubscriptionTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Monthly");
                        cmd.Parameters.AddWithValue("@washer", WasherTextBox.Text.Trim());
                        cmd.Parameters.AddWithValue("@status",
                            (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Active");
                        cmd.Parameters.AddWithValue("@nextDueDate",
                            NextDueDatePicker.SelectedDate ?? DateTime.Now.AddMonths(1));
                        cmd.Parameters.AddWithValue("@orderDate",
                            StartDatePicker.SelectedDate ?? DateTime.Now);
                        cmd.Parameters.AddWithValue("@carInvoiceId", carInvoiceId);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Car subscription updated successfully! ✅", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            DataUpdated = true;
                            this.DialogResult = true;
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Failed to update subscription.", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating subscription: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to cancel? Any unsaved changes will be lost.",
                "Confirm Cancel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                this.DialogResult = false;
                this.Close();
            }
        }
    }
}