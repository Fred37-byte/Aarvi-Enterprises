using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EmployeeManagerWPF.Models;
using NewCustomerWindow;

namespace EmployeeManagerWPF
{
    public partial class NewInvoiceWindow : Window
    {
        private ObservableCollection<InvoiceItem> _items = new ObservableCollection<InvoiceItem>();
        private string _selectedService;
        private int _selectedCustomerId;

        // Service-specific controls
        private ComboBox _waterBrandCombo;
        private ComboBox _waterQuantityCombo;
        private TextBox _waterUnitsBox;
        private DatePicker _waterDeliveryDatePicker;
        private TextBox _waterAddressBox;

        public NewInvoiceWindow()
        {
            InitializeComponent();

            // Wire UI after InitializeComponent so controls exist
            ItemsGrid.ItemsSource = _items;
            InvoiceDatePicker.SelectedDate = DateTime.Today;

            // Recalculate when items change
            _items.CollectionChanged += (s, e) => RecalculateTotals();

            // Wire the discount TextChanged in code (we removed it from XAML)
            DiscountPercentBox.TextChanged += RecalculateTotals;

            // Ensure we compute once the Window is fully loaded
            Loaded += (s, e) => RecalculateTotals();
        }

        private void ServiceTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ServiceTypeComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            _selectedService = selectedItem.Content.ToString();

            // Reset everything
            CustomerComboBox.Items.Clear();
            SocietyComboBox.Items.Clear();
            _items.Clear();
            ServiceSpecificPanel.Children.Clear();

            ClearCustomerFields();

            // Show/hide sections based on service
            if (_selectedService == "Aarvi Car Washing")
            {
                // Car Washing: Service → Society → Customer
                SocietySection.Visibility = Visibility.Visible;
                CustomerSection.Visibility = Visibility.Collapsed;
                CustomerSectionTitle.Text = "3. Select Customer";
                InvoiceDetailsSectionTitle.Text = "4. Invoice Details";
                ItemsSectionTitle.Text = "5. Add Items";

                LoadSocieties();
            }
            else
            {
                // Water/Laundry: Service → Customer
                SocietySection.Visibility = Visibility.Collapsed;
                CustomerSection.Visibility = Visibility.Visible;
                CustomerSectionTitle.Text = "2. Select Customer";
                InvoiceDetailsSectionTitle.Text = "3. Invoice Details";
                ItemsSectionTitle.Text = "4. Add Items";

                LoadCustomers();
            }

            HideAllSectionsAfterCustomer();
            RecalculateTotals(); // keep totals in sync when service changes
        }

        private void LoadSocieties()
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = "SELECT SocietyId, SocietyName FROM CarWashingSocieties ORDER BY SocietyName";
                SqlCommand cmd = new SqlCommand(query, conn);

                try
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        SocietyComboBox.Items.Add(new ComboBoxItem
                        {
                            Content = reader["SocietyName"].ToString(),
                            Tag = reader["SocietyId"]
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading societies: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SocietyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedSociety = SocietyComboBox.SelectedItem as ComboBoxItem;
            if (selectedSociety == null) return;

            int societyId = Convert.ToInt32(selectedSociety.Tag);

            CustomerComboBox.Items.Clear();
            LoadCustomersForSociety(societyId);

            CustomerSection.Visibility = Visibility.Visible;
            HideAllSectionsAfterCustomer();
            RecalculateTotals();
        }

        private void LoadCustomersForSociety(int societyId)
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"
                    SELECT DISTINCT cw.CustomerId, c.FullName, c.Phone, c.Email, c.Address, c.City, c.State, c.ZipCode
                    FROM CarWashingOrders cw
                    INNER JOIN Customers c ON cw.CustomerId = c.Id
                    WHERE cw.SocietyId = @SocietyId AND cw.Status = 'Active'
                    ORDER BY c.FullName";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@SocietyId", societyId);

                try
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var item = new ComboBoxItem
                        {
                            Content = reader["FullName"].ToString(),
                            Tag = new CustomerData
                            {
                                Id = Convert.ToInt32(reader["CustomerId"]),
                                Phone = reader["Phone"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                Address = reader["Address"]?.ToString(),
                                City = reader["City"]?.ToString(),
                                State = reader["State"]?.ToString(),
                                ZipCode = reader["ZipCode"]?.ToString()
                            }
                        };
                        CustomerComboBox.Items.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading customers: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadCustomers()
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;
            string serviceType = _selectedService;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"
                    SELECT Id, FullName, Phone, Email, Address, City, State, ZipCode
                    FROM Customers
                    WHERE ServiceType = @ServiceType
                    ORDER BY FullName";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ServiceType", serviceType);

                try
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var item = new ComboBoxItem
                        {
                            Content = reader["FullName"].ToString(),
                            Tag = new CustomerData
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Phone = reader["Phone"]?.ToString(),
                                Email = reader["Email"]?.ToString(),
                                Address = reader["Address"]?.ToString(),
                                City = reader["City"]?.ToString(),
                                State = reader["State"]?.ToString(),
                                ZipCode = reader["ZipCode"]?.ToString()
                            }
                        };
                        CustomerComboBox.Items.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading customers: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCustomer = CustomerComboBox.SelectedItem as ComboBoxItem;
            if (selectedCustomer == null) return;

            var customerData = selectedCustomer.Tag as CustomerData;
            if (customerData == null) return;

            _selectedCustomerId = customerData.Id;

            // Auto-fill customer details
            CustomerPhoneBox.Text = customerData.Phone ?? "-";
            CustomerEmailBox.Text = customerData.Email ?? "-";

            string fullAddress = $"{customerData.Address}\n{customerData.City}, {customerData.State} {customerData.ZipCode}";
            CustomerAddressBox.Text = fullAddress.Trim();

            // Show remaining sections
            InvoiceDetailsSection.Visibility = Visibility.Visible;
            ItemsSection.Visibility = Visibility.Visible;
            TotalsSection.Visibility = Visibility.Visible;
            SubmitSection.Visibility = Visibility.Visible;

            // Load service-specific fields
            LoadServiceSpecificFields();

            RecalculateTotals();
        }

        private void LoadServiceSpecificFields()
        {
            ServiceSpecificPanel.Children.Clear();
            ServiceSpecificSection.Visibility = Visibility.Collapsed;

            if (_selectedService == "Aarvi Water Supplier")
            {
                ServiceSpecificSection.Visibility = Visibility.Visible;

                // Title
                var title = new TextBlock
                {
                    Text = "Water Supply Details",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = System.Windows.Media.Brushes.Black,
                    Margin = new Thickness(0, 0, 0, 16)
                };
                ServiceSpecificPanel.Children.Add(title);

                var grid = new Grid();
                grid.Margin = new Thickness(0, 0, 0, 16);
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Brand
                var brandStack = new StackPanel();
                brandStack.Children.Add(new TextBlock { Text = "Brand *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
                _waterBrandCombo = new ComboBox { Height = 36, FontSize = 13 };
                _waterBrandCombo.Items.Add("Bisleri");
                _waterBrandCombo.Items.Add("Aquafina");
                _waterBrandCombo.Items.Add("Kinley");
                brandStack.Children.Add(_waterBrandCombo);
                Grid.SetColumn(brandStack, 0);
                grid.Children.Add(brandStack);

                // Quantity
                var qtyStack = new StackPanel();
                qtyStack.Children.Add(new TextBlock { Text = "Bottle Size *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
                _waterQuantityCombo = new ComboBox { Height = 36, FontSize = 13 };
                _waterQuantityCombo.Items.Add("5 Liters");
                _waterQuantityCombo.Items.Add("10 Liters");
                _waterQuantityCombo.Items.Add("20 Liters");
                qtyStack.Children.Add(_waterQuantityCombo);
                Grid.SetColumn(qtyStack, 2);
                grid.Children.Add(qtyStack);

                ServiceSpecificPanel.Children.Add(grid);

                var grid2 = new Grid();
                grid2.Margin = new Thickness(0, 0, 0, 16);
                grid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16, GridUnitType.Pixel) });
                grid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Units
                var unitsStack = new StackPanel();
                unitsStack.Children.Add(new TextBlock { Text = "Number of Bottles *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
                _waterUnitsBox = new TextBox
                {
                    Height = 36,
                    Padding = new Thickness(10, 8, 10, 8), // left, top, right, bottom
                    FontSize = 13
                };
                unitsStack.Children.Add(_waterUnitsBox);
                Grid.SetColumn(unitsStack, 0);
                grid2.Children.Add(unitsStack);

                // Delivery Date
                var dateStack = new StackPanel();
                dateStack.Children.Add(new TextBlock { Text = "Delivery Date *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
                _waterDeliveryDatePicker = new DatePicker { Height = 36, FontSize = 13, SelectedDate = DateTime.Today };
                dateStack.Children.Add(_waterDeliveryDatePicker);
                Grid.SetColumn(dateStack, 2);
                grid2.Children.Add(dateStack);

                ServiceSpecificPanel.Children.Add(grid2);

                // Delivery Address
                ServiceSpecificPanel.Children.Add(new TextBlock { Text = "Delivery Address *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
                _waterAddressBox = new TextBox
                {
                    Height = 70,
                    Padding = new Thickness(10),
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                ServiceSpecificPanel.Children.Add(_waterAddressBox);
            }
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            _items.Add(new InvoiceItem
            {
                ItemName = "New Item",
                Description = "",
                Rate = 0,
                Quantity = 1
            });
            RecalculateTotals();
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = ItemsGrid.SelectedItem as InvoiceItem;
            if (selected != null)
            {
                _items.Remove(selected);
                RecalculateTotals();
            }
        }

        private void RecalculateTotals(object sender = null, TextChangedEventArgs e = null)
        {
            // If any of the referenced controls are not yet created, exit safely.
            if (SubtotalText == null || DiscountAmountText == null || TotalText == null || DiscountPercentBox == null)
                return;

            decimal subtotal = 0m;
            try
            {
                subtotal = _items?.Sum(i => i.Amount) ?? 0m;
            }
            catch
            {
                subtotal = 0m;
            }

            decimal discountPercent = 0m;
            var raw = DiscountPercentBox.Text;
            if (!string.IsNullOrWhiteSpace(raw) && decimal.TryParse(raw, out var dp))
                discountPercent = Math.Max(0m, Math.Min(100m, dp));

            var discountAmount = Math.Round(subtotal * (discountPercent / 100m), 2);
            var total = subtotal - discountAmount;

            SubtotalText.Text = $"₹{subtotal:N2}";
            DiscountAmountText.Text = $"-₹{discountAmount:N2}";
            TotalText.Text = $"₹{total:N2}";
        }

        private void SubmitInvoice_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrEmpty(_selectedService))
            {
                MessageBox.Show("Please select a service type.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedCustomerId == 0)
            {
                MessageBox.Show("Please select a customer.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_items.Count == 0)
            {
                MessageBox.Show("Please add at least one item.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (InvoiceDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select an invoice date.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Water-specific validation
            if (_selectedService == "Aarvi Water Supplier")
            {
                if (_waterBrandCombo?.SelectedItem == null ||
                    _waterQuantityCombo?.SelectedItem == null ||
                    string.IsNullOrWhiteSpace(_waterUnitsBox?.Text) ||
                    _waterDeliveryDatePicker?.SelectedDate == null ||
                    string.IsNullOrWhiteSpace(_waterAddressBox?.Text))
                {
                    MessageBox.Show("Please fill all water supply details.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(_waterUnitsBox.Text, out int units) || units <= 0)
                {
                    MessageBox.Show("Number of bottles must be a valid number.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            // Save invoice
            try
            {
                int invoiceId = InvoiceService.CreateInvoice(
                    BuildInvoice(),
                    _items.ToList()
                );

                // Save water details if applicable
                if (_selectedService == "Aarvi Water Supplier")
                {
                    SaveWaterOrderDetails(invoiceId);
                }

                MessageBox.Show($"Invoice created successfully!\nInvoice ID: {invoiceId}", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating invoice: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Invoice BuildInvoice()
        {
            var customerItem = CustomerComboBox.SelectedItem as ComboBoxItem;
            var statusItem = StatusComboBox.SelectedItem as ComboBoxItem;

            decimal subtotal = _items.Sum(i => i.Amount);
            decimal discountPercent = decimal.TryParse(DiscountPercentBox.Text, out decimal dp) ? dp : 0;
            decimal discountAmount = Math.Round(subtotal * (discountPercent / 100), 2);
            decimal total = subtotal - discountAmount;

            return new Invoice
            {
                CustomerId = _selectedCustomerId,
                CustomerName = customerItem?.Content.ToString(),
                ServiceType = _selectedService,
                InvoiceType = _selectedService,
                InvoiceDate = InvoiceDatePicker.SelectedDate.Value,
                Status = statusItem?.Content.ToString() ?? "Unpaid",
                Description = NotesBox.Text,
                Subtotal = subtotal,
                DiscountPercentage = discountPercent,
                DiscountAmount = discountAmount,
                Amount = total
            };
        }

        private void SaveWaterOrderDetails(int invoiceId)
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"
                    INSERT INTO WaterOrders (InvoiceId, Brand, Quantity, Units, Address, DeliveryDate)
                    VALUES (@InvoiceId, @Brand, @Quantity, @Units, @Address, @DeliveryDate)";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                cmd.Parameters.AddWithValue("@Brand", _waterBrandCombo.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@Quantity", _waterQuantityCombo.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@Units", int.Parse(_waterUnitsBox.Text));
                cmd.Parameters.AddWithValue("@Address", _waterAddressBox.Text);
                cmd.Parameters.AddWithValue("@DeliveryDate", _waterDeliveryDatePicker.SelectedDate.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void HideAllSectionsAfterCustomer()
        {
            InvoiceDetailsSection.Visibility = Visibility.Collapsed;
            ItemsSection.Visibility = Visibility.Collapsed;
            ServiceSpecificSection.Visibility = Visibility.Collapsed;
            TotalsSection.Visibility = Visibility.Collapsed;
            SubmitSection.Visibility = Visibility.Collapsed;
        }

        private void ClearCustomerFields()
        {
            CustomerPhoneBox.Text = "";
            CustomerEmailBox.Text = "";
            CustomerAddressBox.Text = "";
            _selectedCustomerId = 0;
        }

        // Helper class for customer data
        private class CustomerData
        {
            public int Id { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string ZipCode { get; set; }
        }
    }
}
