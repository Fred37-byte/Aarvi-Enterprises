using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NewCustomerWindow
{
    public partial class CarInvoiceWindow : Window
    {
        private int carInvoiceId;
        private string connectionString;
        private int generatedInvoiceId = 0;
        private bool isInvoiceGenerated = false;

        public CarInvoiceWindow(int carInvoiceId)
        {
            InitializeComponent();
            this.carInvoiceId = carInvoiceId;
            connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            LoadCarDetails();
            GenerateInvoiceNumber();
            txtInvoiceDate.Text = DateTime.Now.ToString("dd MMM yyyy");
        }

        private void LoadCarDetails()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"SELECT 
                        CustomerName, Society, Flat, Mobile,
                        CarModel, CarNumber, 
                        Subscription, SubscriptionType,
                        OrderDate, NextDueDate, Washer
                    FROM CarWashingOrders 
                    WHERE CarInvoiceId = @carInvoiceId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@carInvoiceId", carInvoiceId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Customer Details
                                txtCustomerName.Text = reader["CustomerName"]?.ToString() ?? "N/A";
                                txtSociety.Text = reader["Society"]?.ToString() ?? "N/A";
                                txtFlat.Text = reader["Flat"]?.ToString() ?? "N/A";
                                txtMobile.Text = reader["Mobile"]?.ToString() ?? "N/A";

                                // Car Details
                                txtCarModel.Text = reader["CarModel"]?.ToString() ?? "N/A";
                                txtCarNumber.Text = reader["CarNumber"]?.ToString() ?? "N/A";

                                // Subscription Details
                                string subscriptionType = reader["SubscriptionType"]?.ToString() ?? "Monthly";
                                txtSubscriptionType.Text = subscriptionType;

                                string amount = reader["Subscription"]?.ToString() ?? "0";
                                txtAmount.Text = $"₹{amount}";
                                txtSubtotal.Text = $"₹{amount}";
                                txtTotal.Text = $"₹{amount}";

                                // Dates
                                DateTime? startDate = reader["OrderDate"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["OrderDate"])
                                    : (DateTime?)null;

                                DateTime? dueDate = reader["NextDueDate"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["NextDueDate"])
                                    : (DateTime?)null;

                                txtStartDate.Text = startDate?.ToString("dd MMM yyyy") ?? "N/A";
                                txtDueDate.Text = dueDate?.ToString("dd MMM yyyy") ?? "N/A";

                                // Washer
                                txtWasher.Text = reader["Washer"]?.ToString() ?? "Unassigned";


                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading car details: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateInvoiceNumber()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Get the last invoice number for car washing service
                    string query = @"SELECT TOP 1 InvoiceId 
                                   FROM Invoices 
                                   WHERE ServiceType = 'CarWash' 
                                   ORDER BY InvoiceId DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        int lastId = result != null ? Convert.ToInt32(result) : 0;
                        int nextId = lastId + 1;

                        // Format: INV-CAR-YYYY-XXX
                        string invoiceNumber = $"INV-CAR-{DateTime.Now.Year}-{nextId:D3}";
                        txtInvoiceNumber.Text = invoiceNumber;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating invoice number: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (isInvoiceGenerated)
            {
                MessageBox.Show("Invoice already generated!", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        // Get car order details
                        string getOrderQuery = @"SELECT CustomerId, Subscription, SubscriptionType, NextDueDate 
                                               FROM CarWashingOrders 
                                               WHERE CarInvoiceId = @carInvoiceId";

                        int customerId;
                        decimal amount;
                        string subscriptionType;
                        DateTime dueDate;

                        using (SqlCommand cmd = new SqlCommand(getOrderQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@carInvoiceId", carInvoiceId);
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    throw new Exception("Car order not found!");
                                }

                                customerId = Convert.ToInt32(reader["CustomerId"]);
                                amount = Convert.ToDecimal(reader["Subscription"]);
                                subscriptionType = reader["SubscriptionType"]?.ToString() ?? "Monthly";
                                dueDate = reader["NextDueDate"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["NextDueDate"])
                                    : DateTime.Now.AddMonths(1);
                            }
                        }

                        // Step 1: Insert into main Invoices table
                        string insertInvoiceQuery = @"INSERT INTO Invoices 
                            (CustomerId, ServiceType, InvoiceDate, Amount, Status, InvoiceType, Description)
                            OUTPUT INSERTED.InvoiceId
                            VALUES (@CustomerId, 'CarWash', @InvoiceDate, @Amount, 'Pending', 'Service', @Description)";

                        using (SqlCommand cmd = new SqlCommand(insertInvoiceQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CustomerId", customerId);
                            cmd.Parameters.AddWithValue("@InvoiceDate", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Amount", amount);
                            cmd.Parameters.AddWithValue("@Description",
                                $"Car Wash - {subscriptionType} Subscription - {txtInvoiceNumber.Text}");

                            generatedInvoiceId = (int)cmd.ExecuteScalar();
                        }

                        // Step 2: Insert into CarInvoices table
                        string insertCarInvoiceQuery = @"INSERT INTO CarInvoices 
                            (InvoiceId, CarInvoiceId, PaymentStatus, CreatedDate)
                            VALUES (@InvoiceId, @CarInvoiceId, 'Unpaid', @CreatedDate)";

                        using (SqlCommand cmd = new SqlCommand(insertCarInvoiceQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@InvoiceId", generatedInvoiceId);
                            cmd.Parameters.AddWithValue("@CarInvoiceId", carInvoiceId);
                            cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);

                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        isInvoiceGenerated = true;
                        btnGenerate.IsEnabled = false;
                        btnPrint.IsEnabled = true;

                        MessageBox.Show($"Invoice generated successfully!\n\nInvoice Number: {txtInvoiceNumber.Text}\nAmount: ₹{amount}",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception($"Transaction failed: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating invoice: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a print dialog
                PrintDialog printDialog = new PrintDialog();

                if (printDialog.ShowDialog() == true)
                {
                    // Get the visual element to print (the invoice content)
                    var visual = this.Content as Visual;

                    if (visual != null)
                    {
                        printDialog.PrintVisual(visual, $"Invoice - {txtInvoiceNumber.Text}");
                        MessageBox.Show("Invoice sent to printer!", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing invoice: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}