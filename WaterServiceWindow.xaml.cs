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

        // Add Order
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string brand = (BrandCombo.SelectedItem as ComboBoxItem)?.Content.ToString();
                string quantity = (QuantityCombo.SelectedItem as ComboBoxItem)?.Content.ToString();
                int units = int.Parse(UnitsTextBox.Text);
                string address = AddressTextBox.Text;

                if (string.IsNullOrWhiteSpace(brand) || string.IsNullOrWhiteSpace(quantity) ||
                    units <= 0 || string.IsNullOrWhiteSpace(address))
                {
                    MessageBox.Show("Please fill all fields correctly.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO WaterOrders (Brand, Quantity, Units, Address) VALUES (@Brand, @Quantity, @Units, @Address)";
                    SqlCommand cmd = new SqlCommand(query, conn);

                    cmd.Parameters.AddWithValue("@Brand", brand);
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    cmd.Parameters.AddWithValue("@Units", units);
                    cmd.Parameters.AddWithValue("@Address", address);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    LoadOrders();
                }

                MessageBox.Show("Order placed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadOrders();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                SELECT Brand, Quantity, Units, Address 
                FROM WaterOrders
                WHERE Brand LIKE @search 
                   OR Quantity LIKE @search 
                   OR Address LIKE @search
                   OR CAST(Units AS NVARCHAR) LIKE @search";  // ✅ Added Units search

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



        // Load orders into DataGrid
        private void LoadOrders()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "SELECT * FROM WaterOrders";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    var orders = new System.Collections.Generic.List<dynamic>();

                    while (reader.Read())
                    {
                        orders.Add(new
                        {
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

        // Clear form
        private void ClearForm()
        {
            BrandCombo.SelectedIndex = -1;
            QuantityCombo.SelectedIndex = -1;
            UnitsTextBox.Clear();
            AddressTextBox.Clear();
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
