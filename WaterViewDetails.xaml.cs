using System;
using System.Windows;

namespace NewCustomerWindow.xaml
{
    public partial class WaterViewDetails : Window
    {
        private WaterOrder currentOrder;

        public WaterViewDetails(WaterOrder order)
        {
            InitializeComponent();
            currentOrder = order;
            LoadOrderDetails();
        }

        private void LoadOrderDetails()
        {
            try
            {
                // Populate all fields
                OrderIdBadge.Text = $"#{currentOrder.WaterInvoiceId}";
                InvoiceIdText.Text = currentOrder.InvoiceId.ToString();
                CustomerNameText.Text = currentOrder.CustomerName;
                BrandText.Text = currentOrder.Brand;
                BottleSizeText.Text = currentOrder.Quantity;
                UnitsText.Text = currentOrder.Units.ToString();
                DeliveryDateText.Text = currentOrder.DeliveryDate.ToString("dddd, dd MMMM yyyy");
                AddressText.Text = currentOrder.Address;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading order details: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewInvoiceButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                WaterInvoiceWindow invoiceWindow = new WaterInvoiceWindow(currentOrder.InvoiceId);
                invoiceWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening invoice: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}