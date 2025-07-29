using PdfSharp.Pdf;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using System.Diagnostics;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace NewCustomerWindow.xaml
{
    public partial class ViewInvoiceWindow : Window
    {
        private Customer _customer;
        private List<Invoice> _invoices;

        public ViewInvoiceWindow(Customer customer, List<Invoice> invoices)
        {
            InitializeComponent();
            _customer = customer;
            _invoices = invoices;

            // Register font resolver (only once globally!)
            if (GlobalFontSettings.FontResolver == null)
                GlobalFontSettings.FontResolver = new CustomFontResolver();

            LoadCustomerInfo(customer);
            invoiceGrid.ItemsSource = invoices;
        }

        private void LoadCustomerInfo(Customer customer)
        {
            txtName.Text = customer.FullName;
            txtEmail.Text = customer.Email;
            txtPhone.Text = customer.Phone;
            txtAddress.Text = $"{customer.Address}, {customer.City}, {customer.State} - {customer.ZipCode}";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = $"Invoice_{_customer.FullName.Replace(" ", "_")}.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    PdfDocument doc = new PdfDocument();
                    doc.Info.Title = "Customer Invoice";

                    PdfPage page = doc.AddPage();
                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    // ✅ PDFsharp 6.2 - Use XFontOptions with XFontStyleEx
                    var titleFont = new XFont("OpenSans", 16, XFontStyleEx.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));
                    var labelFont = new XFont("OpenSans", 12, XFontStyleEx.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
                    var tableFont = new XFont("OpenSans", 10, XFontStyleEx.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));

                    double yValue = 40;

                    // Title
                    gfx.DrawString("Customer Invoice Report", titleFont, XBrushes.Black,
                        new XRect(XUnit.FromPoint(0), XUnit.FromPoint(yValue), page.Width, XUnit.FromPoint(30)),
                        XStringFormats.TopCenter);
                    yValue += 40;

                    // Customer Info
                    gfx.DrawString($"Name: {_customer.FullName}", labelFont, XBrushes.Black, XUnit.FromPoint(40), XUnit.FromPoint(yValue)); yValue += 20;
                    gfx.DrawString($"Email: {_customer.Email}", labelFont, XBrushes.Black, XUnit.FromPoint(40), XUnit.FromPoint(yValue)); yValue += 20;
                    gfx.DrawString($"Phone: {_customer.Phone}", labelFont, XBrushes.Black, XUnit.FromPoint(40), XUnit.FromPoint(yValue)); yValue += 20;
                    gfx.DrawString($"Address: {_customer.Address}, {_customer.City}, {_customer.State} - {_customer.ZipCode}", labelFont, XBrushes.Black, XUnit.FromPoint(40), XUnit.FromPoint(yValue)); yValue += 30;

                    // Table Header
                    gfx.DrawString("Date", tableFont, XBrushes.Black, XUnit.FromPoint(40), XUnit.FromPoint(yValue));
                    gfx.DrawString("Type", tableFont, XBrushes.Black, XUnit.FromPoint(110), XUnit.FromPoint(yValue));
                    gfx.DrawString("Description", tableFont, XBrushes.Black, XUnit.FromPoint(240), XUnit.FromPoint(yValue));
                    gfx.DrawString("Amount", tableFont, XBrushes.Black, XUnit.FromPoint(400), XUnit.FromPoint(yValue));
                    gfx.DrawString("Status", tableFont, XBrushes.Black, XUnit.FromPoint(480), XUnit.FromPoint(yValue));
                    yValue += 15;

                    gfx.DrawLine(XPens.Black, XUnit.FromPoint(40), XUnit.FromPoint(yValue), page.Width - XUnit.FromPoint(40), XUnit.FromPoint(yValue));
                    yValue += 10;

                    // Table Rows
                    foreach (var inv in _invoices)
                    {
                        gfx.DrawString(inv.InvoiceDate.ToShortDateString(), tableFont, XBrushes.Black, XUnit.FromPoint(40), XUnit.FromPoint(yValue));
                        gfx.DrawString(inv.InvoiceType, tableFont, XBrushes.Black, XUnit.FromPoint(110), XUnit.FromPoint(yValue));
                        gfx.DrawString(inv.Description, tableFont, XBrushes.Black, XUnit.FromPoint(240), XUnit.FromPoint(yValue));
                        gfx.DrawString($"₹{inv.Amount:N2}", tableFont, XBrushes.Black, XUnit.FromPoint(400), XUnit.FromPoint(yValue));
                        gfx.DrawString(inv.Status, tableFont, XBrushes.Black, XUnit.FromPoint(480), XUnit.FromPoint(yValue));
                        yValue += 20;

                        // Page overflow check
                        if (yValue > page.Height.Point - 40)
                        {
                            page = doc.AddPage();
                            gfx = XGraphics.FromPdfPage(page);
                            yValue = 40;
                        }
                    }

                    doc.Save(dlg.FileName);
                    MessageBox.Show("PDF downloaded successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    Process.Start("explorer.exe", $"/select,\"{dlg.FileName}\"");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error generating PDF: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
