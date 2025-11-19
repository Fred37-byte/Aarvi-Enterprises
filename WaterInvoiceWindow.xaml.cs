using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;

namespace NewCustomerWindow.xaml
{
    public partial class WaterInvoiceWindow : Window
    {
        private string connectionString;
        private int invoiceId;
        private List<WaterInvoiceItem> waterOrders = new List<WaterInvoiceItem>();

        public WaterInvoiceWindow(int invoiceId)
        {
            InitializeComponent();
            this.invoiceId = invoiceId;
            connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;
            LoadInvoiceData();
        }

        private void LoadInvoiceData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Load Invoice Header Information
                    string invoiceQuery = @"
                        SELECT 
                            i.InvoiceId,
                            i.CustomerName,
                            i.PhoneNumber,
                            i.Address,
                            i.InvoiceDate,
                            i.PaymentStatus
                        FROM Invoices i
                        WHERE i.InvoiceId = @InvoiceId";

                    SqlCommand invoiceCmd = new SqlCommand(invoiceQuery, conn);
                    invoiceCmd.Parameters.AddWithValue("@InvoiceId", invoiceId);

                    SqlDataReader invoiceReader = invoiceCmd.ExecuteReader();
                    if (invoiceReader.Read())
                    {
                        InvoiceNumberText.Text = $"#{invoiceId.ToString().PadLeft(4, '0')}";
                        CustomerNameText.Text = invoiceReader["CustomerName"].ToString();
                        PhoneText.Text = invoiceReader["PhoneNumber"].ToString();
                        AddressText.Text = invoiceReader["Address"].ToString();
                        InvoiceDateText.Text = Convert.ToDateTime(invoiceReader["InvoiceDate"]).ToString("dd MMM yyyy");
                        PaymentStatusText.Text = invoiceReader["PaymentStatus"].ToString();
                    }
                    invoiceReader.Close();

                    // Load Water Orders
                    string ordersQuery = @"
                        SELECT 
                            w.Brand,
                            w.Quantity,
                            w.Units,
                            w.DeliveryDate,
                            w.PricePerUnit
                        FROM WaterOrders w
                        WHERE w.InvoiceId = @InvoiceId
                        ORDER BY w.WaterInvoiceId";

                    SqlCommand ordersCmd = new SqlCommand(ordersQuery, conn);
                    ordersCmd.Parameters.AddWithValue("@InvoiceId", invoiceId);

                    SqlDataReader ordersReader = ordersCmd.ExecuteReader();
                    waterOrders.Clear();
                    int serialNo = 1;
                    DateTime? deliveryDate = null;

                    while (ordersReader.Read())
                    {
                        int units = Convert.ToInt32(ordersReader["Units"]);
                        decimal pricePerUnit = Convert.ToDecimal(ordersReader["PricePerUnit"]);
                        decimal totalPrice = units * pricePerUnit;

                        waterOrders.Add(new WaterInvoiceItem
                        {
                            SerialNo = serialNo++,
                            Brand = ordersReader["Brand"].ToString(),
                            Quantity = ordersReader["Quantity"].ToString(),
                            Units = units,
                            PricePerUnit = pricePerUnit,
                            TotalPrice = totalPrice
                        });

                        // Get delivery date from first order
                        if (!deliveryDate.HasValue)
                        {
                            deliveryDate = Convert.ToDateTime(ordersReader["DeliveryDate"]);
                        }
                    }
                    ordersReader.Close();

                    // Set delivery date
                    if (deliveryDate.HasValue)
                    {
                        DeliveryDateText.Text = deliveryDate.Value.ToString("dd MMM yyyy");
                    }

                    // Bind data and calculate totals
                    WaterOrdersDataGrid.ItemsSource = waterOrders;
                    CalculateTotals();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading invoice data: {ex.Message}", "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateTotals()
        {
            decimal subtotal = waterOrders.Sum(o => o.TotalPrice);
            decimal tax = 0; // No tax for now
            decimal total = subtotal + tax;

            SubtotalText.Text = $"₹{subtotal:N2}";
            TaxText.Text = $"₹{tax:N2}";
            TotalAmountText.Text = $"₹{total:N2}";
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Implement printing functionality
                MessageBox.Show("Print functionality will be implemented here.\n\n" +
                              "This will send the invoice to your printer.",
                              "Print Invoice",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing invoice: {ex.Message}", "Print Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Invoice_{invoiceId}_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".pdf",
                    Filter = "PDF files (*.pdf)|*.pdf"
                };

                if (dlg.ShowDialog() == true)
                {
                    // TODO: Implement PDF generation
                    MessageBox.Show($"PDF will be saved to:\n{dlg.FileName}\n\n" +
                                  "Note: PDF generation needs to be implemented.",
                                  "Save PDF",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving PDF: {ex.Message}", "PDF Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    // Water Invoice Item Model
    public class WaterInvoiceItem
    {
        public int SerialNo { get; set; }
        public string Brand { get; set; }
        public string Quantity { get; set; }
        public int Units { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal TotalPrice { get; set; }
    }
}