using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NewCustomerWindow.xaml
{
    public partial class WaterServiceWindow : Window
    {
        private string connectionString;
        private List<WaterOrder> allOrders = new List<WaterOrder>();

        public WaterServiceWindow()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;
            LoadOrders();
        }

        // 🔹 Load orders with Invoice + Customer info + DeliveryDate
        private void LoadOrders()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT 
                            w.WaterInvoiceId,
                            w.InvoiceId,
                            ISNULL(i.CustomerName, 'Unknown') AS CustomerName,
                            w.Brand,
                            w.Quantity,
                            w.Units,
                            w.DeliveryDate,
                            w.Address
                        FROM WaterOrders w
                        LEFT JOIN Invoices i ON w.InvoiceId = i.InvoiceId
                        ORDER BY w.DeliveryDate DESC, w.WaterInvoiceId DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    allOrders.Clear();

                    while (reader.Read())
                    {
                        allOrders.Add(new WaterOrder
                        {
                            WaterInvoiceId = Convert.ToInt32(reader["WaterInvoiceId"]),
                            InvoiceId = Convert.ToInt32(reader["InvoiceId"]),
                            CustomerName = reader["CustomerName"].ToString(),
                            Brand = reader["Brand"].ToString(),
                            Quantity = reader["Quantity"].ToString(),
                            Units = Convert.ToInt32(reader["Units"]),
                            DeliveryDate = Convert.ToDateTime(reader["DeliveryDate"]),
                            Address = reader["Address"].ToString()
                        });
                    }

                    OrdersDataGrid.ItemsSource = allOrders;
                    UpdateStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading orders: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔹 Update statistics cards
        private void UpdateStatistics()
        {
            try
            {
                // Total Orders
                TotalOrdersText.Text = allOrders.Count.ToString();

                // Total Bottles
                int totalBottles = allOrders.Sum(o => o.Units);
                TotalBottlesText.Text = totalBottles.ToString();

                // Unique Customers
                int uniqueCustomers = allOrders.Select(o => o.CustomerName).Distinct().Count();
                TotalCustomersText.Text = uniqueCustomers.ToString();

                // This Month Orders
                DateTime startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                int thisMonthOrders = allOrders.Count(o => o.DeliveryDate >= startOfMonth);
                ThisMonthText.Text = thisMonthOrders.ToString();
            }
            catch (Exception ex)
            {
                // Silent fail for statistics
                Console.WriteLine($"Statistics update error: {ex.Message}");
            }
        }

        // 🔹 Search/Filter Orders
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                OrdersDataGrid.ItemsSource = allOrders;
            }
            else
            {
                var filtered = allOrders.Where(o =>
                    o.InvoiceId.ToString().Contains(searchText) ||
                    o.CustomerName.ToLower().Contains(searchText) ||
                    o.Brand.ToLower().Contains(searchText) ||
                    o.Quantity.ToLower().Contains(searchText) ||
                    o.Address.ToLower().Contains(searchText) ||
                    o.DeliveryDate.ToString("dd MMM yyyy").ToLower().Contains(searchText)
                ).ToList();

                OrdersDataGrid.ItemsSource = filtered;
            }
        }

        // 🔹 View Details Button in DataGrid row
        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is WaterOrder order)
            {
                // Open the WaterViewDetails window
                WaterViewDetails detailsWindow = new WaterViewDetails(order);
                detailsWindow.ShowDialog();
            }
        }
    }

    // 🔹 Water Order Model
    public class WaterOrder
    {
        public int WaterInvoiceId { get; set; }
        public int InvoiceId { get; set; }
        public string CustomerName { get; set; }
        public string Brand { get; set; }
        public string Quantity { get; set; }
        public int Units { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Address { get; set; }
    }
}