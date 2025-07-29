using System;
using System.Windows;
using System.Windows.Controls;

namespace NewCustomerWindow
{
    public partial class EditInvoiceWindow : Window
    {
        private Invoice _invoice;

        public EditInvoiceWindow(Invoice invoice)
        {
            InitializeComponent();
            _invoice = invoice;
            LoadInvoiceData();
        }

        private void LoadInvoiceData()
        {
            txtCustomer.Text = _invoice.CustomerName;
            txtDescription.Text = _invoice.Description;
            txtAmount.Text = _invoice.Amount.ToString();
            dpDate.SelectedDate = _invoice.InvoiceDate;

            // Set Invoice Type in cmbType
            foreach (ComboBoxItem item in cmbType.Items)
            {
                if (item.Content.ToString() == _invoice.InvoiceType)
                {
                    cmbType.SelectedItem = item;
                    break;
                }
            }

            // Set Status in cmbStatus
            foreach (ComboBoxItem item in cmbStatus.Items)
            {
                if (item.Content.ToString() == _invoice.Status)
                {
                    cmbStatus.SelectedItem = item;
                    break;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCustomer.Text) ||
                cmbType.SelectedItem == null ||
                string.IsNullOrWhiteSpace(txtDescription.Text) ||
                string.IsNullOrWhiteSpace(txtAmount.Text) ||
                cmbStatus.SelectedItem == null ||
                dpDate.SelectedDate == null)
            {
                MessageBox.Show("Please fill all fields.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _invoice.CustomerName = txtCustomer.Text;
            _invoice.InvoiceType = (cmbType.SelectedItem as ComboBoxItem)?.Content.ToString();
            _invoice.Description = txtDescription.Text;
            _invoice.Amount = double.TryParse(txtAmount.Text, out double amt) ? amt : 0;
            _invoice.Status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content.ToString();
            _invoice.InvoiceDate = dpDate.SelectedDate.Value;

            InvoiceService.UpdateInvoice(_invoice);

            MessageBox.Show("Invoice updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
