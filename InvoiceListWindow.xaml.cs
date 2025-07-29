using ClosedXML.Excel;
using Microsoft.Win32;
using NewCustomerWindow.xaml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NewCustomerWindow
{
    public partial class InvoiceListWindow : Window
    {
        private List<Invoice> _allInvoices;

        public InvoiceListWindow()
        {
            InitializeComponent();
            LoadInvoices();
        }

        private void LoadInvoices()
        {
            _allInvoices = InvoiceService.GetAllInvoices();
            InvoiceGrid.ItemsSource = _allInvoices;
            lblNoResults.Visibility = Visibility.Collapsed;
        }

        // 👇 Populate ComboBox with unique Invoice Types on load
        private void cmbInvoiceType_Loaded(object sender, RoutedEventArgs e)
        {
            var types = _allInvoices
                .Select(inv => inv.InvoiceType)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            // Skip adding if already loaded
            if (cmbInvoiceType.Items.Count == 1) // Only dummy "-- Select Type --" exists
            {
                foreach (var type in types)
                {
                    cmbInvoiceType.Items.Add(type);
                }
            }
        }

        // 🔍 Filter invoices based on user input
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime? fromDate = dpStartDate.SelectedDate;
            DateTime? toDate = dpEndDate.SelectedDate;
            string selectedType = cmbInvoiceType.SelectedIndex > 0 ? cmbInvoiceType.Text : null;
            string selectedStatus = cmbStatus.SelectedIndex > 0 ? cmbStatus.Text : null;
            string customerSearch = txtCustomerName.Text.Trim().ToLower();

            var filtered = _allInvoices.Where(inv =>
                (!fromDate.HasValue || inv.InvoiceDate >= fromDate.Value) &&
                (!toDate.HasValue || inv.InvoiceDate <= toDate.Value) &&
                (string.IsNullOrEmpty(selectedType) || inv.InvoiceType == selectedType) &&
                (string.IsNullOrEmpty(selectedStatus) || inv.Status == selectedStatus) &&
                (string.IsNullOrEmpty(customerSearch) || inv.CustomerName.ToLower().Contains(customerSearch))
            ).ToList();

            InvoiceGrid.ItemsSource = filtered;
            lblNoResults.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }


        // ❌ Clear all filters and show full list
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            dpStartDate.SelectedDate = null;
            dpEndDate.SelectedDate = null;
            cmbInvoiceType.SelectedIndex = 0;
            cmbStatus.SelectedIndex = 0;
            txtCustomerName.Text = string.Empty;

            InvoiceGrid.ItemsSource = _allInvoices;
            lblNoResults.Visibility = Visibility.Collapsed;
        }


        private void EditInvoice_Click(object sender, RoutedEventArgs e)
        {
            var invoice = (sender as Button)?.DataContext as Invoice;
            if (invoice == null) return;

            var editWindow = new EditInvoiceWindow(invoice);
            if (editWindow.ShowDialog() == true)
            {
                LoadInvoices(); // Refresh grid after editing
            }
        }



        private void DeleteInvoice_Click(object sender, RoutedEventArgs e)
        {
            var invoice = (sender as Button)?.DataContext as Invoice;
            if (invoice == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete invoice #{invoice.Id}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                InvoiceService.DeleteInvoice(invoice.Id); // You should already have this method
                _allInvoices.Remove(invoice);

                InvoiceGrid.ItemsSource = null;
                InvoiceGrid.ItemsSource = _allInvoices;

                MessageBox.Show("Invoice deleted successfully!", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        private void ViewInvoice_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var selectedInvoice = button?.DataContext as Invoice;

            if (selectedInvoice == null) return;

            var customer = CustomerService.GetCustomerByName(selectedInvoice.CustomerName);
            var customerInvoices = InvoiceService.GetInvoicesByCustomerName(selectedInvoice.CustomerName);

            var viewWindow = new ViewInvoiceWindow(customer, customerInvoices);
            viewWindow.ShowDialog();
        }

        private void BtnExportAllTables_Click(object sender, RoutedEventArgs e)
        {
            string connectionString = "Server=DESKTOP-71OLI2R;Database=BusinessManager;Trusted_Connection=True;";


            // Ask user where to save the Excel file
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel Workbook|*.xlsx";
            saveFileDialog.Title = "Save All Tables as Excel File";
            saveFileDialog.FileName = "DataExport.xlsx";

            if (saveFileDialog.ShowDialog() == true)
            {
                using (XLWorkbook workbook = new XLWorkbook())
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // ✅ Automatically get all user table names
                    DataTable schema = conn.GetSchema("Tables");
                    List<string> userTables = new List<string>();
                    foreach (DataRow row in schema.Rows)
                    {
                        string tableType = row["TABLE_TYPE"].ToString();
                        string tableName = row["TABLE_NAME"].ToString();
                        if (tableType == "BASE TABLE")
                        {
                            userTables.Add(tableName);
                        }
                    }

                    // Loop through each table
                    foreach (string tableName in userTables)
                    {
                        try
                        {
                            DataTable dt = new DataTable();

                            using (SqlCommand cmd = new SqlCommand($"SELECT * FROM {tableName}", conn))
                            using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                            {
                                adapter.Fill(dt);
                            }

                            // ✅ OPTIONAL FILTER: Remove system columns like "RowVersion", "IsDeleted", etc.
                            string[] columnsToSkip = { "RowVersion", "IsDeleted", "PasswordHash" }; // Add more as needed
                            foreach (string colName in columnsToSkip)
                            {
                                if (dt.Columns.Contains(colName))
                                    dt.Columns.Remove(colName);
                            }

                            // ✅ Add worksheet & apply formatting
                            var ws = workbook.Worksheets.Add(dt, tableName);

                            // Bold headers
                            ws.Row(1).Style.Font.Bold = true;

                            // Auto-fit columns
                            ws.Columns().AdjustToContents();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error exporting table {tableName}: {ex.Message}");
                        }
                    }

                    workbook.SaveAs(saveFileDialog.FileName);
                    MessageBox.Show("All tables exported to Excel successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}
