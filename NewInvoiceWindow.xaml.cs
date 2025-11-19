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
        private int _selectedSocietyId;
        private int _editingItemIndex = -1;

        // Water service controls
        private ComboBox _waterBrandCombo;
        private ComboBox _waterQuantityCombo;
        private TextBox _waterUnitsBox;
        private TextBox _waterRateBox;
        private DatePicker _waterDeliveryDatePicker;
        private TextBox _waterAddressBox;
        private Button _addToItemsButton;

        // Car Washing service controls
        private ComboBox _carNameCombo;
        private TextBox _carRegistrationBox;
        private ComboBox _serviceTypeCombo;
        private ComboBox _frequencyCombo;
        private TextBox _carRateBox;
        private Button _addCarItemButton;

        // ✅ FIX 1: Added public properties for CarInvoiceWindow to check
        public bool InvoiceCreated { get; private set; } = false;
        public int GeneratedInvoiceId { get; private set; } = 0;

        private int _prefilledCarInvoiceId = 0;

        // New constructor for pre-filled car washing invoice
        public NewInvoiceWindow(
            int customerId,
            int carInvoiceId,
            string serviceType,
            decimal amount,
            string subscriptionType,
            string society,
            string flat,
            string mobile,
            string carModel,
            string carNumber,
            string washer) : this()
        {
            _prefilledCarInvoiceId = carInvoiceId;

            // Pre-select service type
            foreach (ComboBoxItem item in ServiceTypeComboBox.Items)
            {
                if (item.Content.ToString() == serviceType)
                {
                    ServiceTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // Wait for service type to load societies
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Find and select the society
                foreach (ComboBoxItem item in SocietyComboBox.Items)
                {
                    if (item.Content.ToString() == society)
                    {
                        SocietyComboBox.SelectedItem = item;
                        break;
                    }
                }

                // Wait for customers to load, then select the customer
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (ComboBoxItem item in CustomerComboBox.Items)
                    {
                        var customerData = item.Tag as CustomerData;
                        if (customerData != null && customerData.Id == customerId)
                        {
                            CustomerComboBox.SelectedItem = item;
                            break;
                        }
                    }

                    // Auto-fill car details
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_carNameCombo != null && _carNameCombo.Items.Count > 0)
                        {
                            foreach (ComboBoxItem item in _carNameCombo.Items)
                            {
                                var carData = item.Tag as CarData;
                                if (carData != null &&
                                    carData.CarName == carModel &&
                                    carData.RegistrationNumber == carNumber)
                                {
                                    _carNameCombo.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                    }), System.Windows.Threading.DispatcherPriority.Loaded);

                }), System.Windows.Threading.DispatcherPriority.Loaded);

            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void LinkCarInvoice(int invoiceId, int carInvoiceId)
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                string query = @"INSERT INTO CarInvoices 
                        (InvoiceId, CarInvoiceId, PaymentStatus, CreatedDate)
                        VALUES (@InvoiceId, @CarInvoiceId, 'Unpaid', @CreatedDate)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                    cmd.Parameters.AddWithValue("@CarInvoiceId", carInvoiceId);
                    cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public NewInvoiceWindow()
        {
            InitializeComponent();

            ItemsGrid.ItemsSource = _items;
            InvoiceDatePicker.SelectedDate = DateTime.Today;

            _items.CollectionChanged += (s, e) => RecalculateTotals();
            DiscountPercentBox.TextChanged += RecalculateTotals;

            Loaded += (s, e) => RecalculateTotals();
        }

        private void ServiceTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = ServiceTypeComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            _selectedService = selectedItem.Content.ToString();

            CustomerComboBox.Items.Clear();
            SocietyComboBox.Items.Clear();
            _items.Clear();
            ServiceSpecificPanel.Children.Clear();

            ClearCustomerFields();

            if (_selectedService == "Aarvi Car Washing")
            {
                SocietySection.Visibility = Visibility.Visible;
                CustomerSection.Visibility = Visibility.Collapsed;
                CustomerSectionTitle.Text = "3. Select Customer";
                InvoiceDetailsSectionTitle.Text = "4. Invoice Details";
                ItemsSectionTitle.Text = "6. Invoice Items";

                LoadSocieties();
            }
            else
            {
                SocietySection.Visibility = Visibility.Collapsed;
                CustomerSection.Visibility = Visibility.Visible;
                CustomerSectionTitle.Text = "2. Select Customer";
                InvoiceDetailsSectionTitle.Text = "3. Invoice Details";
                ItemsSectionTitle.Text = "5. Invoice Items";

                LoadCustomers();
            }

            HideAllSectionsAfterCustomer();
            RecalculateTotals();
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

            _selectedSocietyId = Convert.ToInt32(selectedSociety.Tag);

            CustomerComboBox.Items.Clear();
            LoadCustomersForSociety(_selectedSocietyId);

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

            CustomerPhoneBox.Text = customerData.Phone ?? "-";
            CustomerEmailBox.Text = customerData.Email ?? "-";

            string fullAddress = $"{customerData.Address}\n{customerData.City}, {customerData.State} {customerData.ZipCode}";
            CustomerAddressBox.Text = fullAddress.Trim();

            InvoiceDetailsSection.Visibility = Visibility.Visible;

            LoadServiceSpecificFields();

            ItemsSection.Visibility = Visibility.Visible;
            TotalsSection.Visibility = Visibility.Visible;
            SubmitSection.Visibility = Visibility.Visible;

            RecalculateTotals();
        }

        private void LoadServiceSpecificFields()
        {
            ServiceSpecificPanel.Children.Clear();
            ServiceSpecificSection.Visibility = Visibility.Collapsed;

            if (_selectedService == "Aarvi Water Supplier")
            {
                LoadWaterSupplyFields();
            }
            else if (_selectedService == "Aarvi Car Washing")
            {
                LoadCarWashingFields();
            }
        }

        private void LoadWaterSupplyFields()
        {
            ServiceSpecificSection.Visibility = Visibility.Visible;

            var title = new TextBlock
            {
                Text = "4. Water Supply Details",
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

            var brandStack = new StackPanel();
            brandStack.Children.Add(new TextBlock { Text = "Brand *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
            _waterBrandCombo = new ComboBox { Height = 36, FontSize = 13 };
            _waterBrandCombo.Items.Add("Bisleri");
            _waterBrandCombo.Items.Add("Aquafina");
            _waterBrandCombo.Items.Add("Kinley");
            brandStack.Children.Add(_waterBrandCombo);
            Grid.SetColumn(brandStack, 0);
            grid.Children.Add(brandStack);

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

            var unitsStack = new StackPanel();
            unitsStack.Children.Add(new TextBlock { Text = "Number of Bottles *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
            _waterUnitsBox = new TextBox { Height = 36, Padding = new Thickness(10, 8, 10, 8), FontSize = 13 };
            unitsStack.Children.Add(_waterUnitsBox);
            Grid.SetColumn(unitsStack, 0);
            grid2.Children.Add(unitsStack);

            var rateStack = new StackPanel();
            rateStack.Children.Add(new TextBlock { Text = "Rate per Bottle *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
            _waterRateBox = new TextBox { Height = 36, Padding = new Thickness(10, 8, 10, 8), FontSize = 13 };
            rateStack.Children.Add(_waterRateBox);
            Grid.SetColumn(rateStack, 2);
            grid2.Children.Add(rateStack);

            ServiceSpecificPanel.Children.Add(grid2);

            var grid3 = new Grid();
            grid3.Margin = new Thickness(0, 0, 0, 16);
            grid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var dateStack = new StackPanel();
            dateStack.Children.Add(new TextBlock { Text = "Delivery Date *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
            _waterDeliveryDatePicker = new DatePicker { Height = 36, FontSize = 13, SelectedDate = DateTime.Today };
            dateStack.Children.Add(_waterDeliveryDatePicker);
            Grid.SetColumn(dateStack, 0);
            grid3.Children.Add(dateStack);

            ServiceSpecificPanel.Children.Add(grid3);

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

            _addToItemsButton = CreateStyledButton("+ Add to Items", "#10b981");
            _addToItemsButton.Click += AddWaterItemToGrid_Click;

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 16, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            buttonPanel.Children.Add(_addToItemsButton);
            ServiceSpecificPanel.Children.Add(buttonPanel);
        }

        private void LoadCarWashingFields()
        {
            ServiceSpecificSection.Visibility = Visibility.Visible;

            var title = new TextBlock
            {
                Text = "5. Car Service Details",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.Black,
                Margin = new Thickness(0, 0, 0, 16)
            };
            ServiceSpecificPanel.Children.Add(title);

            var grid1 = new Grid();
            grid1.Margin = new Thickness(0, 0, 0, 16);
            grid1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16, GridUnitType.Pixel) });
            grid1.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var carStack = new StackPanel();
            carStack.Children.Add(new TextBlock { Text = "Car Name/Model *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
            _carNameCombo = new ComboBox { Height = 36, FontSize = 13 };
            _carNameCombo.SelectionChanged += CarNameCombo_SelectionChanged;
            LoadCarNamesForCustomer();
            carStack.Children.Add(_carNameCombo);
            Grid.SetColumn(carStack, 0);
            grid1.Children.Add(carStack);

            var regStack = new StackPanel();
            regStack.Children.Add(new TextBlock { Text = "Registration Number *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
            _carRegistrationBox = new TextBox { Height = 36, Padding = new Thickness(10, 8, 10, 8), FontSize = 13, IsReadOnly = true };
            regStack.Children.Add(_carRegistrationBox);
            Grid.SetColumn(regStack, 2);
            grid1.Children.Add(regStack);

            ServiceSpecificPanel.Children.Add(grid1);

            var grid2 = new Grid();
            grid2.Margin = new Thickness(0, 0, 0, 16);
            grid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16, GridUnitType.Pixel) });
            grid2.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var serviceStack = new StackPanel();
            serviceStack.Children.Add(new TextBlock { Text = "Service Type *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
            _serviceTypeCombo = new ComboBox { Height = 36, FontSize = 13, IsReadOnly = true };
            serviceStack.Children.Add(_serviceTypeCombo);
            Grid.SetColumn(serviceStack, 0);
            grid2.Children.Add(serviceStack);

            var freqStack = new StackPanel();
            freqStack.Children.Add(new TextBlock { Text = "Frequency *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
            _frequencyCombo = new ComboBox { Height = 36, FontSize = 13, IsReadOnly = true };
            freqStack.Children.Add(_frequencyCombo);
            Grid.SetColumn(freqStack, 2);
            grid2.Children.Add(freqStack);

            ServiceSpecificPanel.Children.Add(grid2);

            var grid3 = new Grid();
            grid3.Margin = new Thickness(0, 0, 0, 16);
            grid3.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var rateStack = new StackPanel();
            rateStack.Children.Add(new TextBlock { Text = "Service Rate *", FontSize = 13, Foreground = System.Windows.Media.Brushes.DimGray, Margin = new Thickness(0, 0, 0, 6) });
            _carRateBox = new TextBox { Height = 36, Padding = new Thickness(10, 8, 10, 8), FontSize = 13 };
            rateStack.Children.Add(_carRateBox);
            Grid.SetColumn(rateStack, 0);
            grid3.Children.Add(rateStack);

            ServiceSpecificPanel.Children.Add(grid3);

            _addCarItemButton = CreateStyledButton("+ Add to Items", "#10b981");
            _addCarItemButton.Click += AddCarItemToGrid_Click;

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 16, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            buttonPanel.Children.Add(_addCarItemButton);
            ServiceSpecificPanel.Children.Add(buttonPanel);
        }

        private void LoadCarNamesForCustomer()
        {
            if (_selectedCustomerId == 0 || _selectedSocietyId == 0) return;

            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"
            SELECT DISTINCT 
                CarModel,
                CarNumber,
                CarType,
                SubscriptionType,
                Subscription
            FROM CarWashingOrders
            WHERE CustomerId = @CustomerId 
              AND SocietyId = @SocietyId 
              AND Status = 'Active'
            ORDER BY CarModel";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CustomerId", _selectedCustomerId);
                cmd.Parameters.AddWithValue("@SocietyId", _selectedSocietyId);

                try
                {
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    _carNameCombo.Items.Clear();

                    while (reader.Read())
                    {
                        var carData = new CarData
                        {
                            CarName = reader["CarModel"].ToString(),
                            RegistrationNumber = reader["CarNumber"].ToString(),
                            ServiceType = reader["CarType"]?.ToString() ?? "Standard",
                            Frequency = reader["SubscriptionType"]?.ToString() ?? "Monthly",
                            Rate = reader["Subscription"] != DBNull.Value
                                ? Convert.ToDecimal(reader["Subscription"])
                                : 0m
                        };

                        _carNameCombo.Items.Add(new ComboBoxItem
                        {
                            Content = carData.CarName,
                            Tag = carData
                        });
                    }

                    if (_carNameCombo.Items.Count == 0)
                    {
                        MessageBox.Show("No active cars found for this customer in the selected society.",
                            "No Cars Found", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading car details: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CarNameCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = _carNameCombo.SelectedItem as ComboBoxItem;
            if (selected == null) return;

            var carData = selected.Tag as CarData;
            if (carData == null) return;

            _carRegistrationBox.Text = carData.RegistrationNumber;

            _serviceTypeCombo.Items.Clear();
            _serviceTypeCombo.Items.Add(carData.ServiceType);
            _serviceTypeCombo.SelectedIndex = 0;

            _frequencyCombo.Items.Clear();
            _frequencyCombo.Items.Add(carData.Frequency);
            _frequencyCombo.SelectedIndex = 0;

            _carRateBox.Text = carData.Rate.ToString("F2");
        }

        private void AddCarItemToGrid_Click(object sender, RoutedEventArgs e)
        {
            if (_carNameCombo?.SelectedItem == null ||
                string.IsNullOrWhiteSpace(_carRegistrationBox?.Text) ||
                _serviceTypeCombo?.SelectedItem == null ||
                _frequencyCombo?.SelectedItem == null ||
                string.IsNullOrWhiteSpace(_carRateBox?.Text))
            {
                MessageBox.Show("Please fill all car service details.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(_carRateBox.Text, out decimal rate) || rate <= 0)
            {
                MessageBox.Show("Rate must be a valid positive number.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string carName = (_carNameCombo.SelectedItem as ComboBoxItem)?.Content.ToString();
            string registration = _carRegistrationBox.Text;
            string serviceType = _serviceTypeCombo.SelectedItem?.ToString();
            string frequency = _frequencyCombo.SelectedItem?.ToString();

            string itemName = $"{carName} ({registration})";
            string description = $"{serviceType} ({frequency})";

            var newItem = new InvoiceItem
            {
                ItemName = itemName,
                Description = description,
                Rate = rate,
                Quantity = 1
            };

            if (_editingItemIndex >= 0 && _editingItemIndex < _items.Count)
            {
                _items[_editingItemIndex] = newItem;
                _editingItemIndex = -1;
                _addCarItemButton.Content = "+ Add to Items";
                _addCarItemButton.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10b981"));
            }
            else
            {
                _items.Add(newItem);
            }

            ClearCarFields();
            RecalculateTotals();
        }

        private void AddWaterItemToGrid_Click(object sender, RoutedEventArgs e)
        {
            if (_waterBrandCombo?.SelectedItem == null ||
                _waterQuantityCombo?.SelectedItem == null ||
                string.IsNullOrWhiteSpace(_waterUnitsBox?.Text) ||
                string.IsNullOrWhiteSpace(_waterRateBox?.Text) ||
                _waterDeliveryDatePicker?.SelectedDate == null ||
                string.IsNullOrWhiteSpace(_waterAddressBox?.Text))
            {
                MessageBox.Show("Please fill all water supply details.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(_waterUnitsBox.Text, out int units) || units <= 0)
            {
                MessageBox.Show("Number of bottles must be a valid positive number.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(_waterRateBox.Text, out decimal rate) || rate <= 0)
            {
                MessageBox.Show("Rate must be a valid positive number.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string brand = _waterBrandCombo.SelectedItem.ToString();
            string quantity = _waterQuantityCombo.SelectedItem.ToString();
            string description = $"Delivery: {_waterDeliveryDatePicker.SelectedDate.Value:dd-MMM-yyyy}\nAddress: {_waterAddressBox.Text}";

            var newItem = new InvoiceItem
            {
                ItemName = $"{brand} - {quantity}",
                Description = description,
                Rate = rate,
                Quantity = units
            };

            if (_editingItemIndex >= 0 && _editingItemIndex < _items.Count)
            {
                _items[_editingItemIndex] = newItem;
                _editingItemIndex = -1;
                _addToItemsButton.Content = "+ Add to Items";
                _addToItemsButton.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10b981"));
            }
            else
            {
                _items.Add(newItem);
            }

            ClearWaterFields();
            RecalculateTotals();
        }

        private void ItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = ItemsGrid.SelectedItem as InvoiceItem;
            if (selected == null) return;

            if (_selectedService == "Aarvi Water Supplier" && selected.ItemName.Contains("-"))
            {
                LoadWaterItemForEdit(selected);
            }
            else if (_selectedService == "Aarvi Car Washing" && selected.ItemName.Contains("("))
            {
                LoadCarItemForEdit(selected);
            }
        }

        private void LoadWaterItemForEdit(InvoiceItem item)
        {
            var parts = item.ItemName.Split(new[] { " - " }, StringSplitOptions.None);

            try
            {
                if (parts.Length == 2)
                {
                    _waterBrandCombo.SelectedItem = parts[0].Trim();
                    _waterQuantityCombo.SelectedItem = parts[1].Trim();
                }

                var lines = item.Description.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length > 0 && lines[0].StartsWith("Delivery: "))
                {
                    string dateStr = lines[0].Replace("Delivery: ", "").Trim();
                    if (DateTime.TryParseExact(dateStr, "dd-MMM-yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime deliveryDate))
                    {
                        _waterDeliveryDatePicker.SelectedDate = deliveryDate;
                    }
                }

                if (lines.Length > 1 && lines[1].StartsWith("Address: "))
                {
                    _waterAddressBox.Text = lines[1].Replace("Address: ", "").Trim();
                }

                _waterUnitsBox.Text = item.Quantity.ToString();
                _waterRateBox.Text = item.Rate.ToString("F2");

                _editingItemIndex = _items.IndexOf(item);
                _addToItemsButton.Content = "Update Item";
                _addToItemsButton.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f59e0b"));
            }
            catch
            {
                MessageBox.Show("Error loading item details.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCarItemForEdit(InvoiceItem item)
        {
            try
            {
                int startIndex = item.ItemName.IndexOf("(");
                int endIndex = item.ItemName.IndexOf(")");

                if (startIndex > 0 && endIndex > startIndex)
                {
                    string carName = item.ItemName.Substring(0, startIndex).Trim();
                    string registration = item.ItemName.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();

                    foreach (ComboBoxItem comboItem in _carNameCombo.Items)
                    {
                        if (comboItem.Content.ToString() == carName)
                        {
                            _carNameCombo.SelectedItem = comboItem;
                            break;
                        }
                    }
                }

                string desc = item.Description;
                int descStart = desc.IndexOf("(");
                int descEnd = desc.IndexOf(")");

                if (descStart > 0 && descEnd > descStart)
                {
                    string serviceType = desc.Substring(0, descStart).Trim();
                    string frequency = desc.Substring(descStart + 1, descEnd - descStart - 1).Trim();

                    _serviceTypeCombo.Items.Clear();
                    _serviceTypeCombo.Items.Add(serviceType);
                    _serviceTypeCombo.SelectedIndex = 0;

                    _frequencyCombo.Items.Clear();
                    _frequencyCombo.Items.Add(frequency);
                    _frequencyCombo.SelectedIndex = 0;
                }

                _carRateBox.Text = item.Rate.ToString("F2");

                _editingItemIndex = _items.IndexOf(item);
                _addCarItemButton.Content = "Update Item";
                _addCarItemButton.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f59e0b"));
            }
            catch
            {
                MessageBox.Show("Error loading car item details.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Button CreateStyledButton(string content, string colorHex)
        {
            var button = new Button
            {
                Content = content,
                Height = 40,
                Padding = new Thickness(24, 0, 24, 0),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var buttonTemplate = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new System.Windows.TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.PaddingProperty, new System.Windows.TemplateBindingExtension(Button.PaddingProperty));

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);

            buttonTemplate.VisualTree = borderFactory;
            button.Template = buttonTemplate;

            return button;
        }

        private void ClearWaterFields()
        {
            if (_waterBrandCombo != null) _waterBrandCombo.SelectedIndex = -1;
            if (_waterQuantityCombo != null) _waterQuantityCombo.SelectedIndex = -1;
            if (_waterUnitsBox != null) _waterUnitsBox.Clear();
            if (_waterRateBox != null) _waterRateBox.Clear();
            if (_waterDeliveryDatePicker != null) _waterDeliveryDatePicker.SelectedDate = DateTime.Today;
            if (_waterAddressBox != null) _waterAddressBox.Clear();
        }

        private void ClearCarFields()
        {
            if (_carNameCombo != null) _carNameCombo.SelectedIndex = -1;
            if (_carRegistrationBox != null) _carRegistrationBox.Clear();
            if (_serviceTypeCombo != null) _serviceTypeCombo.Items.Clear();
            if (_frequencyCombo != null) _frequencyCombo.Items.Clear();
            if (_carRateBox != null) _carRateBox.Clear();
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = ItemsGrid.SelectedItem as InvoiceItem;
            if (selected != null)
            {
                _items.Remove(selected);

                if (_editingItemIndex >= 0)
                {
                    _editingItemIndex = -1;

                    if (_selectedService == "Aarvi Water Supplier" && _addToItemsButton != null)
                    {
                        _addToItemsButton.Content = "+ Add to Items";
                        _addToItemsButton.Background = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10b981"));
                        ClearWaterFields();
                    }
                    else if (_selectedService == "Aarvi Car Washing" && _addCarItemButton != null)
                    {
                        _addCarItemButton.Content = "+ Add to Items";
                        _addCarItemButton.Background = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10b981"));
                        ClearCarFields();
                    }
                }

                RecalculateTotals();
            }
        }

        private void RecalculateTotals(object sender = null, TextChangedEventArgs e = null)
        {
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

        // ✅ FIX 2: Updated SubmitInvoice_Click to set properties and link car invoice
        private void SubmitInvoice_Click(object sender, RoutedEventArgs e)
        {
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

            try
            {
                // Create the invoice
                int invoiceId = InvoiceService.CreateInvoice(
                    BuildInvoice(),
                    _items.ToList()
                );

                // ✅ Set properties that CarInvoiceWindow will check
                GeneratedInvoiceId = invoiceId;
                InvoiceCreated = true;

                // ✅ Link to CarInvoice if this was pre-filled from a car order
                if (_prefilledCarInvoiceId > 0)
                {
                    LinkCarInvoice(invoiceId, _prefilledCarInvoiceId);
                }

                // ✅ Save water order details if applicable
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
                // ✅ Reset properties on error
                InvoiceCreated = false;
                GeneratedInvoiceId = 0;
                MessageBox.Show($"Error creating invoice: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Invoice BuildInvoice()
        {
            var customerItem = CustomerComboBox.SelectedItem as ComboBoxItem;
            var customerData = customerItem?.Tag as CustomerData;
            var statusItem = StatusComboBox.SelectedItem as ComboBoxItem;

            decimal subtotal = _items.Sum(i => i.Amount);
            decimal discountPercent = decimal.TryParse(DiscountPercentBox.Text, out decimal dp) ? dp : 0;
            decimal discountAmount = Math.Round(subtotal * (discountPercent / 100), 2);
            decimal total = subtotal - discountAmount;

            string fullAddress = "";
            if (customerData != null)
            {
                fullAddress = $"{customerData.Address}, {customerData.City}, {customerData.State}".Trim(' ', ',');
            }

            return new Invoice
            {
                CustomerId = _selectedCustomerId,
                CustomerName = customerItem?.Content.ToString(),
                CustomerAddress = customerData?.Address,
                CustomerCity = customerData?.City,
                CustomerZipCode = customerData?.ZipCode,
                CustomerPhone = customerData?.Phone,
                CustomerEmail = customerData?.Email,
                ServiceType = _selectedService,
                InvoiceType = _selectedService,
                InvoiceDate = InvoiceDatePicker.SelectedDate.Value,
                Status = statusItem?.Content.ToString() ?? "Unpaid",
                Description = NotesBox.Text,
                Subtotal = subtotal,
                DiscountPercentage = discountPercent,
                DiscountAmount = discountAmount,
                Amount = total,
                InvoiceItems = _items.ToList()
            };
        }

        private void SaveWaterOrderDetails(int invoiceId)
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                foreach (var item in _items.Where(i => i.ItemName.Contains("-")))
                {
                    try
                    {
                        var parts = item.ItemName.Split(new[] { " - " }, StringSplitOptions.None);
                        string brand = parts.Length > 0 ? parts[0].Trim() : "";
                        string quantity = parts.Length > 1 ? parts[1].Trim() : "";

                        DateTime deliveryDate = DateTime.Today;
                        string address = "";

                        var lines = item.Description.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        if (lines.Length > 0 && lines[0].StartsWith("Delivery: "))
                        {
                            string dateStr = lines[0].Replace("Delivery: ", "").Trim();
                            DateTime.TryParseExact(dateStr, "dd-MMM-yyyy", null,
                                System.Globalization.DateTimeStyles.None, out deliveryDate);
                        }

                        if (lines.Length > 1 && lines[1].StartsWith("Address: "))
                        {
                            address = lines[1].Replace("Address: ", "").Trim();
                        }

                        string query = @"
                            INSERT INTO WaterOrders (InvoiceId, Brand, Quantity, Units, Address, DeliveryDate)
                            VALUES (@InvoiceId, @Brand, @Quantity, @Units, @Address, @DeliveryDate)";

                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                        cmd.Parameters.AddWithValue("@Brand", brand);
                        cmd.Parameters.AddWithValue("@Quantity", quantity);
                        cmd.Parameters.AddWithValue("@Units", (int)item.Quantity);
                        cmd.Parameters.AddWithValue("@Address", address);
                        cmd.Parameters.AddWithValue("@DeliveryDate", deliveryDate);

                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving water order details: {ex.Message}", "Warning",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
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

        private class CarData
        {
            public string CarName { get; set; }
            public string RegistrationNumber { get; set; }
            public string ServiceType { get; set; }
            public string Frequency { get; set; }
            public decimal Rate { get; set; }
        }
    }
}