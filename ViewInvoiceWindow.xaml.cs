using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows;
using EmployeeManagerWPF.Models;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using NewCustomerWindow.xaml.Class; // WaterServices

namespace NewCustomerWindow.xaml
{
    public partial class ViewInvoiceWindow : Window
    {
        private Invoice _invoice;
        private readonly string _connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

        // ✅ Constructor now takes the full Invoice (already loaded via InvoiceService)
        public ViewInvoiceWindow(Invoice invoice)
        {
            InitializeComponent();

            _invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));

            // Safety net: if WaterDetails wasn't loaded, try to load it here.
            if (_invoice.WaterDetails == null)
                _invoice.WaterDetails = TryLoadWaterDetails(_invoice.InvoiceId);

            DataContext = _invoice;
        }

        private WaterServices TryLoadWaterDetails(int invoiceId)
        {
            try
            {
                using (var conn = new SqlConnection(_connStr))
                using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 WaterInvoiceId, InvoiceId, DeliveryDate, Brand, Quantity, Units, Address
                    FROM WaterOrders
                    WHERE InvoiceId = @InvoiceId", conn))
                {
                    cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                    conn.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            return new WaterServices
                            {
                                OrderID = (int)r["WaterInvoiceId"],
                                InvoiceId = (int)r["InvoiceId"],
                                DeliveryDate = r.GetDateTime(r.GetOrdinal("DeliveryDate")),
                                Brand = r["Brand"].ToString(),
                                Quantity = r["Quantity"].ToString(),
                                Units = Convert.ToInt32(r["Units"]),
                                Address = r["Address"].ToString()
                            };
                        }
                    }
                }
            }
            catch
            {
                // swallow; UI will just hide the section if null
            }
            return null;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void DownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_invoice == null)
            {
                MessageBox.Show("No invoice loaded!", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = $"Invoice_{_invoice.InvoiceId}.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var doc = new PdfDocument();
                    doc.Info.Title = "Invoice";

                    var page = doc.AddPage();
                    var gfx = XGraphics.FromPdfPage(page);

                    var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
                    var labelFont = new XFont("Arial", 12, XFontStyleEx.Bold);
                    var textFont = new XFont("Arial", 12, XFontStyleEx.Regular);

                    double y = 40;

                    // Header
                    gfx.DrawString("AARVI ENTERPRISES", titleFont, XBrushes.Black,
                        new XRect(0, y, page.Width.Point, 30), XStringFormats.TopCenter);
                    y += 30;

                    gfx.DrawString("Waghbil, Thane, Maharashtra • +91-9987064748 • aarvienterprises@gmail.com",
                        textFont, XBrushes.Black, new XRect(0, y, page.Width.Point, 20), XStringFormats.TopCenter);
                    y += 30;

                    // Invoice Details
                    gfx.DrawString("Invoice Details", labelFont, XBrushes.Black, 40, y); y += 20;

                    gfx.DrawString("Invoice No:", labelFont, XBrushes.Black, 40, y);
                    gfx.DrawString(_invoice.InvoiceId.ToString(), textFont, XBrushes.Black, 140, y); y += 18;

                    gfx.DrawString("Date:", labelFont, XBrushes.Black, 40, y);
                    gfx.DrawString(_invoice.InvoiceDate.ToShortDateString(), textFont, XBrushes.Black, 140, y); y += 18;

                    gfx.DrawString("Customer:", labelFont, XBrushes.Black, 40, y);
                    gfx.DrawString(_invoice.CustomerName ?? "-", textFont, XBrushes.Black, 140, y); y += 18;

                    gfx.DrawString("Type:", labelFont, XBrushes.Black, 40, y);
                    gfx.DrawString(_invoice.InvoiceType ?? "-", textFont, XBrushes.Black, 140, y); y += 18;

                    gfx.DrawString("Description:", labelFont, XBrushes.Black, 40, y);
                    gfx.DrawString(_invoice.Description ?? "-", textFont, XBrushes.Black, 140, y); y += 18;

                    gfx.DrawString("Amount:", labelFont, XBrushes.Black, 40, y);
                    gfx.DrawString($"₹{_invoice.Amount:N2}", textFont, XBrushes.Black, 140, y); y += 18;

                    gfx.DrawString("Status:", labelFont, XBrushes.Black, 40, y);
                    gfx.DrawString(_invoice.Status ?? "-", textFont, XBrushes.Black, 140, y); y += 25;

                    // Water Supply (only if present)
                    if (_invoice.WaterDetails != null)
                    {
                        gfx.DrawString("Water Supply Details", labelFont, XBrushes.Black, 40, y); y += 20;

                        gfx.DrawString("Brand:", labelFont, XBrushes.Black, 40, y);
                        gfx.DrawString(_invoice.WaterDetails.Brand ?? "-", textFont, XBrushes.Black, 140, y); y += 18;

                        gfx.DrawString("Quantity:", labelFont, XBrushes.Black, 40, y);
                        gfx.DrawString(_invoice.WaterDetails.Quantity ?? "-", textFont, XBrushes.Black, 140, y); y += 18;

                        gfx.DrawString("Units:", labelFont, XBrushes.Black, 40, y);
                        gfx.DrawString(_invoice.WaterDetails.Units.ToString(), textFont, XBrushes.Black, 140, y); y += 18;

                        gfx.DrawString("Address:", labelFont, XBrushes.Black, 40, y);
                        gfx.DrawString(_invoice.WaterDetails.Address ?? "-", textFont, XBrushes.Black, 140, y); y += 18;
                    }

                    // Save + Open
                    doc.Save(dlg.FileName);
                    MessageBox.Show("PDF downloaded successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    Process.Start("explorer.exe", $"/select,\"{dlg.FileName}\"");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error generating PDF: " + ex.Message,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
