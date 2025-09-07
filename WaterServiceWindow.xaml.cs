using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace NewCustomerWindow.xaml
{
    public partial class WaterServiceWindow : Window
    {
        private string connectionString = @"Data Source=DESKTOP-71OLI2R;Initial Catalog=BusinessManager;Integrated Security=True";

        public WaterServiceWindow()
        {
            InitializeComponent();
            LoadOrders();
        }

        // 🔹 Load orders with Invoice + Customer info
        private void LoadOrders()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT 
                            w.InvoiceId,
                            i.CustomerName AS Customer,
                            w.Brand,
                            w.Quantity,
                            w.Units,
                            w.Address
                        FROM WaterOrders w
                        INNER JOIN Invoices i ON w.InvoiceId = i.InvoiceId";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    var orders = new System.Collections.Generic.List<dynamic>();

                    while (reader.Read())
                    {
                        orders.Add(new
                        {
                            InvoiceId = reader["InvoiceId"].ToString(),
                            Customer = reader["Customer"].ToString(), // ✅ FIXED: use alias "Customer"
                            Brand = reader["Brand"].ToString(),
                            Quantity = reader["Quantity"].ToString(),
                            Units = reader["Units"].ToString(),
                            Address = reader["Address"].ToString()
                        });
                    }

                    OrdersDataGrid.ItemsSource = orders;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading orders: " + ex.Message);
            }
        }

        // 🔹 Search Orders
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FilterOrders(SearchTextBox.Text.Trim());
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterOrders(SearchTextBox.Text.Trim());
        }

        private void FilterOrders(string searchText)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT 
                            w.InvoiceId,
                            i.CustomerName AS Customer,
                            w.Brand,
                            w.Quantity,
                            w.Units,
                            w.Address
                        FROM WaterOrders w
                        INNER JOIN Invoices i ON w.InvoiceId = i.InvoiceId
                        WHERE i.CustomerName LIKE @search
                           OR w.Brand LIKE @search
                           OR w.Quantity LIKE @search
                           OR w.Address LIKE @search
                           OR CAST(w.Units AS NVARCHAR) LIKE @search
                           OR CAST(w.InvoiceId AS NVARCHAR) LIKE @search";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    adapter.SelectCommand.Parameters.AddWithValue("@search", "%" + searchText + "%");

                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    OrdersDataGrid.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error searching orders: " + ex.Message);
            }
        }

        // 🔹 Ignore Add/Update/Delete for now
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Adding new orders is disabled for now.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Updating orders is disabled for now.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Deleting orders is disabled for now.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
