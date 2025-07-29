using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace EmployeeManagerWPF
{
    public partial class NewInvoiceWindow : Window
    {
        public NewInvoiceWindow(string customerName = "")
        {
            InitializeComponent();

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                CustomerNameBox.Text = customerName;
            }
        }
        private void CreateInvoice_Click(object sender, RoutedEventArgs e)
        {
            string customerName = CustomerNameBox.Text.Trim();

            var invoiceWindow = new NewInvoiceWindow(customerName);
            invoiceWindow.ShowDialog();
        }

        private void SubmitInvoice_Click(object sender, RoutedEventArgs e)
        {
            string customerName = CustomerNameBox.Text.Trim();
            string invoiceType = (InvoiceTypeBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string description = DescriptionBox.Text.Trim();
            string status = (StatusBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string amountText = AmountBox.Text.Trim();
            DateTime? invoiceDate = InvoiceDatePicker.SelectedDate;

            if (string.IsNullOrEmpty(customerName) || invoiceDate == null || string.IsNullOrEmpty(amountText))
            {
                MessageBox.Show("Please fill all required fields.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!float.TryParse(amountText, out float amount))
            {
                MessageBox.Show("Invalid amount entered.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // First, get CustomerId by name
                    string getCustomerIdQuery = "SELECT Id FROM Customers WHERE FullName = @CustomerName";
                    SqlCommand getIdCmd = new SqlCommand(getCustomerIdQuery, conn);
                    getIdCmd.Parameters.AddWithValue("@CustomerName", customerName);

                    object result = getIdCmd.ExecuteScalar();

                    if (result == null)
                    {
                        MessageBox.Show("Customer not found in the database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    int customerId = Convert.ToInt32(result);

                    // Then insert into Invoices table
                    string insertInvoiceQuery = @"
                        INSERT INTO Invoices (CustomerId, InvoiceDate, InvoiceType, Description, Amount, Status)
                        VALUES (@CustomerId, @InvoiceDate, @InvoiceType, @Description, @Amount, @Status)";

                    SqlCommand insertCmd = new SqlCommand(insertInvoiceQuery, conn);
                    insertCmd.Parameters.AddWithValue("@CustomerId", customerId);
                    insertCmd.Parameters.AddWithValue("@InvoiceDate", invoiceDate);
                    insertCmd.Parameters.AddWithValue("@InvoiceType", invoiceType);
                    insertCmd.Parameters.AddWithValue("@Description", description);
                    insertCmd.Parameters.AddWithValue("@Amount", amount);
                    insertCmd.Parameters.AddWithValue("@Status", status);

                    insertCmd.ExecuteNonQuery();

                    MessageBox.Show($"Invoice saved for '{customerName}' with amount ₹{amount}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

