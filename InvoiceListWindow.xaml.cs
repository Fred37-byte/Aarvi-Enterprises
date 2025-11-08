using ClosedXML.Excel;
using EmployeeManagerWPF.Models;
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

        private void cmbInvoiceType_Loaded(object sender, RoutedEventArgs e)
        {
            var types = _allInvoices
                .Select(inv => inv.InvoiceType)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            if (cmbInvoiceType.Items.Count == 1)
            {
                foreach (var type in types)
                    cmbInvoiceType.Items.Add(type);
            }
        }

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
                LoadInvoices();
            }
        }

        private void DeleteInvoice_Click(object sender, RoutedEventArgs e)
        {
            var invoice = (sender as Button)?.DataContext as Invoice;
            if (invoice == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete invoice #{invoice.InvoiceId}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                InvoiceService.DeleteInvoice(invoice.InvoiceId);
                _allInvoices.Remove(invoice);

                InvoiceGrid.ItemsSource = null;
                InvoiceGrid.ItemsSource = _allInvoices;

                MessageBox.Show("Invoice deleted successfully!", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ViewInvoice_Click(object sender, RoutedEventArgs e)
        {
            var selectedInvoice = (sender as Button)?.DataContext as Invoice;
            if (selectedInvoice == null) return;

            // fetch latest details from DB
            var invoiceFromDb = InvoiceService.GetInvoiceById(selectedInvoice.InvoiceId);

            var viewWindow = new ViewInvoiceWindow(invoiceFromDb);
            viewWindow.ShowDialog();
        }




        private void BtnExportAllTables_Click(object sender, RoutedEventArgs e)
        {
            string connectionString = "Server=DESKTOP-71OLI2R;Database=BusinessManager;Trusted_Connection=True;";

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Workbook|*.xlsx",
                Title = "Save All Tables as Excel File",
                FileName = "DataExport.xlsx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    DataTable schema = conn.GetSchema("Tables");
                    var userTables = new List<string>();
                    foreach (DataRow row in schema.Rows)
                    {
                        string tableType = row["TABLE_TYPE"].ToString();
                        string tableName = row["TABLE_NAME"].ToString();
                        if (tableType == "BASE TABLE")
                            userTables.Add(tableName);
                    }

                    foreach (string tableName in userTables)
                    {
                        try
                        {
                            var dt = new DataTable();

                            using (var cmd = new SqlCommand($"SELECT * FROM {tableName}", conn))
                            using (var adapter = new SqlDataAdapter(cmd))
                            {
                                adapter.Fill(dt);
                            }

                            string[] columnsToSkip = { "RowVersion", "IsDeleted", "PasswordHash" };
                            foreach (string colName in columnsToSkip)
                                if (dt.Columns.Contains(colName)) dt.Columns.Remove(colName);

                            var ws = workbook.Worksheets.Add(dt, tableName);
                            ws.Row(1).Style.Font.Bold = true;
                            ws.Columns().AdjustToContents();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error exporting table {tableName}: {ex.Message}");
                        }
                    }

                    workbook.SaveAs(saveFileDialog.FileName);
                    MessageBox.Show("All tables exported to Excel successfully!", "Export Complete",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void InvoiceGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

      
        private void cmbInvoiceType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // TODO: filter or refresh your list based on selected invoice type
        }

    }
}
