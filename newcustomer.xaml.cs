using EmployeeManagerWPF;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;

namespace NewCustomerWindow
{
    public partial class newcustomer : Window
    {
        public newcustomer()
        {
            InitializeComponent();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            NameBox.Clear();
            EmailBox.Clear();
            PhoneBox.Clear();
            AddressBox.Clear();
            CityBox.Clear();
            StateBox.Clear();
            ZipBox.Clear();
            ServiceComboBox.SelectedIndex = -1; // clear dropdown
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = NameBox.Text.Trim();
            string email = EmailBox.Text.Trim();
            string phone = PhoneBox.Text.Trim();
            string address = AddressBox.Text.Trim();
            string city = CityBox.Text.Trim();
            string state = StateBox.Text.Trim();
            string zip = ZipBox.Text.Trim();
            string serviceType = (ServiceComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Please enter at least Name and Email.", "Missing Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(serviceType))
            {
                MessageBox.Show("Please select a service.", "Missing Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            string query = @"
                INSERT INTO Customers (FullName, Email, Phone, Address, City, State, ZipCode, ServiceType)
                VALUES (@FullName, @Email, @Phone, @Address, @City, @State, @ZipCode, @ServiceType)
            ";

            using (SqlConnection conn = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@FullName", name);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);
                cmd.Parameters.AddWithValue("@Address", string.IsNullOrWhiteSpace(address) ? (object)DBNull.Value : address);
                cmd.Parameters.AddWithValue("@City", string.IsNullOrWhiteSpace(city) ? (object)DBNull.Value : city);
                cmd.Parameters.AddWithValue("@State", string.IsNullOrWhiteSpace(state) ? (object)DBNull.Value : state);
                cmd.Parameters.AddWithValue("@ZipCode", string.IsNullOrWhiteSpace(zip) ? (object)DBNull.Value : zip);
                cmd.Parameters.AddWithValue("@ServiceType", serviceType);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();

                    MessageBox.Show($"Customer '{name}' saved successfully with service '{serviceType}'!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error saving customer: " + ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateInvoice_Click(object sender, RoutedEventArgs e)
        {
            var invoiceWindow = new NewInvoiceWindow();
            invoiceWindow.ShowDialog();
        }

        private void ShowInvoiceList_Click(object sender, RoutedEventArgs e)
        {
            var invoiceListWindow = new InvoiceListWindow();
            invoiceListWindow.ShowDialog();
        }
    }
}
