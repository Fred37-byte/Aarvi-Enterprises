using EmployeeManagerWPF;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NewCustomerWindow
{
    public partial class CarInvoiceWindow : Window
    {
        private int carInvoiceId;
        private string connectionString;
        private int generatedInvoiceId = 0; // used after invoice number generation / invoice creation
        private bool isInvoiceGenerated = false;
        public bool InvoiceCreated { get; private set; } = false;
        public int GeneratedInvoiceId { get; private set; } = 0;

        public CarInvoiceWindow(int carInvoiceId)
        {
            InitializeComponent();
            this.carInvoiceId = carInvoiceId;
            connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            LoadCarDetails();
            GenerateInvoiceNumber();
            txtInvoiceDate.Text = DateTime.Now.ToString("dd MMM yyyy");
        }

        /// <summary>
        /// Attempts to read the first available column (from candidates) from a SqlDataReader.
        /// Returns defaultValue if none of the candidates are present or non-null.
        /// </summary>
        private string GetSafeString(SqlDataReader reader, string[] candidateNames, string defaultValue = null)
        {
            foreach (var name in candidateNames)
            {
                try
                {
                    int ord = reader.GetOrdinal(name);
                    if (!reader.IsDBNull(ord))
                        return reader.GetString(ord);
                }
                catch (IndexOutOfRangeException)
                {
                    // column not present - try next
                }
                catch (InvalidCastException)
                {
                    // column present but not string - read as object
                    try
                    {
                        int ord = reader.GetOrdinal(name);
                        if (!reader.IsDBNull(ord))
                            return reader.GetValue(ord)?.ToString();
                    }
                    catch { }
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Loads car & customer data for the provided CarInvoiceId.
        /// Uses column names from your DB (CarWashingOrders, Customers).
        /// </summary>
        private void LoadCarDetails()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // We select the whole CarWashingOrders row and the customer's FullName (if any)
                    string query = @"
                        SELECT cw.*, c.FullName AS CustomerFullName
                        FROM CarWashingOrders cw
                        LEFT JOIN Customers c ON c.Id = cw.CustomerId
                        WHERE cw.CarInvoiceId = @carInvoiceId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@carInvoiceId", carInvoiceId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Customer info (the view/table uses CustomerName and CustomerId)
                                txtCustomerName.Text = GetSafeString(reader, new[] { "CustomerFullName", "CustomerName", "FullName", "Name" }, "N/A");
                                txtSociety.Text = GetSafeString(reader, new[] { "Society" }, "N/A");
                                txtFlat.Text = GetSafeString(reader, new[] { "Flat", "Apartment" }, "N/A");
                                txtMobile.Text = GetSafeString(reader, new[] { "Mobile", "Phone", "PhoneNumber", "Contact" }, "N/A");

                                // Car details — these exist in your schema
                                txtCarModel.Text = GetSafeString(reader, new[] { "CarModel", "Model", "Make" }, "N/A");
                                txtCarNumber.Text = GetSafeString(reader, new[] { "CarNumber", "RegistrationNumber", "RegNo", "CarNo" }, "N/A");

                                // Subscription/service fields
                                string subscriptionType = GetSafeString(reader, new[] { "SubscriptionType", "Subscription", "Frequency", "ServiceType" }, "Monthly");
                                txtSubscriptionType.Text = subscriptionType;

                                // Subscription amount / rate — your script originally stores Subscription as text sometimes,
                                // Invoice.Amount / Invoices.Amount exists too — prefer cw.Subscription then fallback to invoice amount later.
                                string subscriptionValue = GetSafeString(reader, new[] { "Subscription", "Rate", "Amount", "Price", "Fee" }, "0");
                                decimal amountDecimal = 0m;
                                if (!decimal.TryParse(subscriptionValue, out amountDecimal))
                                {
                                    // try to extract numeric characters
                                    var cleaned = new string(subscriptionValue.Where(ch => char.IsDigit(ch) || ch == '.' || ch == ',').ToArray());
                                    decimal.TryParse(cleaned, out amountDecimal);
                                }

                                txtAmount.Text = $"₹{amountDecimal:N2}";
                                txtSubtotal.Text = $"₹{amountDecimal:N2}";
                                txtTotal.Text = $"₹{amountDecimal:N2}";

                                // Dates
                                string orderDateStr = GetSafeString(reader, new[] { "OrderDate", "StartDate", "CreatedDate" }, null);
                                if (DateTime.TryParse(orderDateStr, out DateTime orderDt))
                                    txtStartDate.Text = orderDt.ToString("dd MMM yyyy");
                                else
                                    txtStartDate.Text = "N/A";

                                string nextDueStr = GetSafeString(reader, new[] { "NextDueDate", "NextDue", "DueDate", "ExpiryDate" }, null);
                                if (DateTime.TryParse(nextDueStr, out DateTime dueDt))
                                    txtDueDate.Text = dueDt.ToString("dd MMM yyyy");
                                else
                                    txtDueDate.Text = "N/A";

                                // Washer
                                txtWasher.Text = GetSafeString(reader, new[] { "Washer", "AssignedTo", "WasherName" }, "Unassigned");
                            }
                            else
                            {
                                MessageBox.Show("No car order found for this CarInvoiceId.", "Not found", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading car details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Check if a specific column exists in a table.
        /// </summary>
        private bool ColumnExists(SqlConnection conn, string tableName, string columnName)
        {
            string q = @"
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @table AND COLUMN_NAME = @column";
            using (SqlCommand cmd = new SqlCommand(q, conn))
            {
                cmd.Parameters.AddWithValue("@table", tableName);
                cmd.Parameters.AddWithValue("@column", columnName);
                var result = cmd.ExecuteScalar();
                return result != null;
            }
        }

        /// <summary>
        /// Generates invoice number. Uses InvoiceId counts and checks ServiceType in Invoices (your schema).
        /// </summary>
        private void GenerateInvoiceNumber()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Determine how many existing car-wash related invoices exist.
                    // Your DB contains a ServiceType column with values like 'Aarvi Car Washing' or earlier 'CarWash'.
                    string query = @"
                        SELECT COUNT(*) FROM Invoices
                        WHERE ServiceType = 'Aarvi Car Washing'
                           OR ServiceType = 'CarWash'
                           OR ServiceType LIKE '%Car Wash%'
                           OR ServiceType LIKE '%CarWash%'";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        int nextNumber = count + 1;
                        generatedInvoiceId = nextNumber; // used later to avoid unused warning

                        // Format: INV-CAR-YYYY-XXX
                        string invoiceNumber = $"INV-CAR-{DateTime.Now.Year}-{nextNumber:D3}";
                        txtInvoiceNumber.Text = invoiceNumber;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating invoice number: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (isInvoiceGenerated)
            {
                MessageBox.Show("Invoice already generated!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Get order details — use columns as defined in your DDL
                    string getOrderQuery = @"SELECT * FROM CarWashingOrders WHERE CarInvoiceId = @carInvoiceId";

                    int customerId = 0;
                    decimal amount = 0m;
                    string subscriptionType = "Monthly";
                    DateTime dueDate = DateTime.Now.AddMonths(1);
                    string society = "", flat = "", mobile = "", carModel = "", carNumber = "", washer = "";

                    using (SqlCommand cmd = new SqlCommand(getOrderQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@carInvoiceId", carInvoiceId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                                throw new Exception("Car order not found!");

                            string customerIdStr = GetSafeString(reader, new[] { "CustomerId", "CustId", "Customer_Id" }, "0");
                            int.TryParse(customerIdStr, out customerId);

                            string amountStr = GetSafeString(reader, new[] { "Subscription", "Rate", "Amount", "Price", "Fee" }, "0");
                            var cleaned = new string((amountStr ?? "0").Where(ch => char.IsDigit(ch) || ch == '.' || ch == ',').ToArray());
                            decimal.TryParse(cleaned, out amount);

                            subscriptionType = GetSafeString(reader, new[] { "SubscriptionType", "Subscription", "Frequency", "ServiceType" }, "Monthly");

                            string nextDueStr = GetSafeString(reader, new[] { "NextDueDate", "NextDue", "DueDate" }, null);
                            if (!DateTime.TryParse(nextDueStr, out dueDate))
                                dueDate = DateTime.Now.AddMonths(1);

                            society = GetSafeString(reader, new[] { "Society" }, "");
                            flat = GetSafeString(reader, new[] { "Flat", "Apartment" }, "");
                            mobile = GetSafeString(reader, new[] { "Mobile", "Phone", "Contact" }, "");
                            carModel = GetSafeString(reader, new[] { "CarModel", "Model", "Make" }, "");
                            carNumber = GetSafeString(reader, new[] { "CarNumber", "RegistrationNumber", "RegNo", "CarNo" }, "");
                            washer = GetSafeString(reader, new[] { "Washer", "AssignedTo" }, "");
                        }
                    }

                    // Open NewInvoiceWindow with pre-filled data (keeps your existing constructor usage)
                    var newInvoiceWindow = new NewInvoiceWindow(
                        customerId,
                        carInvoiceId,
                        "Aarvi Car Washing",
                        amount,
                        subscriptionType,
                        society,
                        flat,
                        mobile,
                        carModel,
                        carNumber,
                        washer
                    );

                    // Show dialog
                    newInvoiceWindow.ShowDialog();

                    // Read back whether invoice was created via reflection so code compiles even if NewInvoiceWindow
                    // doesn't define those properties at compile time.
                    bool invoiceCreated = false;
                    try
                    {
                        var pi = newInvoiceWindow.GetType().GetProperty("InvoiceCreated");
                        if (pi != null)
                        {
                            var val = pi.GetValue(newInvoiceWindow);
                            if (val is bool b && b) invoiceCreated = true;
                        }
                    }
                    catch { /* ignore reflection errors */ }

                    if (invoiceCreated)
                    {
                        isInvoiceGenerated = true;
                        btnGenerate.IsEnabled = false;
                        btnPrint.IsEnabled = true;

                        // attempt to capture generated invoice id (if NewInvoiceWindow exposes it)
                        try
                        {
                            var piId = newInvoiceWindow.GetType().GetProperty("GeneratedInvoiceId");
                            if (piId != null)
                            {
                                var idVal = piId.GetValue(newInvoiceWindow);
                                if (idVal != null && int.TryParse(idVal.ToString(), out int parsedId))
                                {
                                    generatedInvoiceId = parsedId;
                                }
                            }
                        }
                        catch { }

                        MessageBox.Show("Invoice created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // fallback: if NewInvoiceWindow has a DialogResult or similar, you could check it
                        MessageBox.Show("Invoice window closed. If an invoice was created, ensure NewInvoiceWindow sets an 'InvoiceCreated' property or returns status.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening invoice window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();

                if (printDialog.ShowDialog() == true)
                {
                    var visual = this.Content as Visual;
                    if (visual != null)
                    {
                        printDialog.PrintVisual(visual, $"Invoice - {txtInvoiceNumber.Text}");
                        MessageBox.Show("Invoice sent to printer!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing invoice: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
