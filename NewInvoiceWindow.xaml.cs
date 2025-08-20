using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EmployeeManagerWPF
{
    public partial class NewInvoiceWindow : Window
    {
        // Keep references so we can read values later
        private ComboBox brandComboRef;
        private ComboBox qtyComboRef;
        private TextBox unitsBoxRef;

        public NewInvoiceWindow()
        {
            InitializeComponent();
        }

        private void LoadCustomersForService(string serviceType)
        {
            CustomerNameComboBox.Items.Clear();

            if (string.IsNullOrEmpty(serviceType))
                return;

            string connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;
            string query = serviceType == "All Services"
                ? "SELECT FullName FROM Customers"
                : "SELECT FullName FROM Customers WHERE ServiceType = @ServiceType";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                if (serviceType != "All Services")
                {
                    cmd.Parameters.AddWithValue("@ServiceType", serviceType);
                }

                try
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        CustomerNameComboBox.Items.Add(reader["FullName"].ToString());
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading customers: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ServiceTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServiceDetailsPanel == null) return;

            var selectedService = (ServiceTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            LoadCustomersForService(selectedService);

            ServiceDetailsPanel.Children.Clear();
            brandComboRef = null;
            qtyComboRef = null;
            unitsBoxRef = null;

            if (selectedService == "Aarvi Water Supplier")
            {
                // Brand
                ServiceDetailsPanel.Children.Add(new TextBlock { Text = "Brand:", FontWeight = FontWeights.Bold });
                brandComboRef = new ComboBox { Height = 30, Margin = new Thickness(0, 0, 0, 10) };
                brandComboRef.Items.Add("Bisleri");
                brandComboRef.Items.Add("Aquafina");
                brandComboRef.Items.Add("Kinley");
                ServiceDetailsPanel.Children.Add(brandComboRef);

                // Quantity
                ServiceDetailsPanel.Children.Add(new TextBlock { Text = "Quantity:", FontWeight = FontWeights.Bold });
                qtyComboRef = new ComboBox { Height = 30, Margin = new Thickness(0, 0, 0, 10) };
                qtyComboRef.Items.Add("5 Liters");
                qtyComboRef.Items.Add("10 Liters");
                qtyComboRef.Items.Add("20 Liters");
                ServiceDetailsPanel.Children.Add(qtyComboRef);

                // Units
                ServiceDetailsPanel.Children.Add(new TextBlock { Text = "Number of Units:", FontWeight = FontWeights.Bold });
                unitsBoxRef = new TextBox { Height = 30, Margin = new Thickness(0, 0, 0, 10) };
                ServiceDetailsPanel.Children.Add(unitsBoxRef);
            }
            else
            {
                ServiceDetailsPanel.Children.Add(new TextBlock
                {
                    Text = "No extra details for this service.",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic
                });
            }
        }

        private void SubmitInvoice_Click(object sender, RoutedEventArgs e)
        {
            string customerName = CustomerNameComboBox.Text.Trim();
            string invoiceType = (InvoiceTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string description = DescriptionBox.Text.Trim();
            string status = (StatusBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string amountText = AmountBox.Text.Trim();
            DateTime? invoiceDate = InvoiceDatePicker.SelectedDate;

            string serviceType = (ServiceTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (string.IsNullOrEmpty(customerName) || invoiceDate == null || string.IsNullOrEmpty(amountText))
            {
                MessageBox.Show("Please fill all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(amountText, out decimal amount))
            {
                MessageBox.Show("Invalid amount entered.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate for Aarvi Water Supplier
            string brand = brandComboRef?.SelectedItem?.ToString();
            string quantity = qtyComboRef?.SelectedItem?.ToString();
            string units = unitsBoxRef?.Text.Trim();

            if (serviceType == "Aarvi Water Supplier")
            {
                if (string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(quantity) || string.IsNullOrWhiteSpace(units))
                {
                    MessageBox.Show("Please fill all water supplier fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!int.TryParse(units, out _))
                {
                    MessageBox.Show("Units must be a number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            string connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // Get CustomerId
                    string getCustomerIdQuery = "SELECT Id FROM Customers WHERE FullName = @CustomerName";
                    SqlCommand getIdCmd = new SqlCommand(getCustomerIdQuery, conn, transaction);
                    getIdCmd.Parameters.AddWithValue("@CustomerName", customerName);

                    object result = getIdCmd.ExecuteScalar();
                    if (result == null)
                    {
                        throw new Exception("Customer not found in the database.");
                    }

                    int customerId = Convert.ToInt32(result);

                    // Insert Invoice
                    string insertInvoiceQuery = @"
                        INSERT INTO Invoices (CustomerId, ServiceType, InvoiceDate, InvoiceType, Description, Amount, Status)
                        OUTPUT INSERTED.Id
                        VALUES (@CustomerId,  @ServiceType, @InvoiceDate, @InvoiceType, @Description, @Amount, @Status)";

                    SqlCommand insertCmd = new SqlCommand(insertInvoiceQuery, conn, transaction);
                    insertCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    insertCmd.Parameters.AddWithValue("@ServiceType", serviceType ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@InvoiceDate", invoiceDate);
                    insertCmd.Parameters.AddWithValue("@InvoiceType", invoiceType ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@Amount", amount);
                    insertCmd.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);

                    int invoiceId = Convert.ToInt32(insertCmd.ExecuteScalar());

                    // If water supplier, insert extra details
                    if (serviceType == "Aarvi Water Supplier")
                    {
                        string insertWaterQuery = @"
                            INSERT INTO WaterServiceOrders (InvoiceId, Brand, Quantity, Units)
                            VALUES (@InvoiceId, @Brand, @Quantity, @Units)";
                        SqlCommand waterCmd = new SqlCommand(insertWaterQuery, conn, transaction);
                        waterCmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                        waterCmd.Parameters.AddWithValue("@Brand", brand);
                        waterCmd.Parameters.AddWithValue("@Quantity", quantity);
                        waterCmd.Parameters.AddWithValue("@Units", int.Parse(units));
                        waterCmd.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    MessageBox.Show($"Invoice saved for '{customerName}' with amount ₹{amount}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
