using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using EmployeeManagerWPF.Models;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace NewCustomerWindow.xaml
{
    public partial class ViewInvoiceWindow : Window
    {
        private Invoice _invoice;

        public ViewInvoiceWindow(Invoice invoice)
        {
            InitializeComponent();
            _invoice = invoice ?? throw new ArgumentNullException(nameof(invoice));

            DataContext = _invoice;

            // Set status-based styling
            UpdateStatusDisplay();
        }

        private void UpdateStatusDisplay()
        {
            string status = _invoice.Status ?? "Unpaid";

            // Update status badge
            switch (status.ToLower())
            {
                case "paid":
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Green
                    StatusText.Text = "✓ PAID";
                    StatusText.Foreground = Brushes.White;

                    // Update balance section
                    BalanceLabel.Text = "AMOUNT PAID";
                    BalanceDueBorder.Background = new SolidColorBrush(Color.FromRgb(236, 253, 245)); // Light green
                    BalanceDueLabel.Text = "AMOUNT PAID";
                    BalanceDueLabel.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    break;

                case "pending":
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Orange
                    StatusText.Text = "⏳ PENDING";
                    StatusText.Foreground = Brushes.White;

                    // Update balance section
                    BalanceLabel.Text = "BALANCE DUE";
                    BalanceDueBorder.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199)); // Light orange
                    BalanceDueLabel.Text = "BALANCE DUE";
                    BalanceDueLabel.Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11));
                    break;

                case "unpaid":
                default:
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                    StatusText.Text = "⚠ DUE";
                    StatusText.Foreground = Brushes.White;

                    // Update balance section
                    BalanceLabel.Text = "BALANCE DUE";
                    BalanceDueBorder.Background = new SolidColorBrush(Color.FromRgb(248, 249, 250)); // Light gray
                    BalanceDueLabel.Text = "BALANCE DUE";
                    BalanceDueLabel.Foreground = Brushes.Black;
                    break;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_invoice == null)
            {
                MessageBox.Show("No invoice loaded!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = $"Invoice_INV{_invoice.InvoiceId:D4}_{DateTime.Now:yyyyMMdd}.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    GenerateInvoicePdf(dlg.FileName);
                    MessageBox.Show("PDF downloaded successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    Process.Start(new ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error generating PDF: " + ex.Message, "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GenerateInvoicePdf(string filename)
        {
            var doc = new PdfDocument();
            doc.Info.Title = $"Invoice INV{_invoice.InvoiceId:D4}";
            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var gfx = XGraphics.FromPdfPage(page);

            // Colors
            var darkText = XColor.FromArgb(44, 62, 80);
            var grayText = XColor.FromArgb(108, 117, 125);
            var lightBg = XColor.FromArgb(248, 249, 250);
            var borderColor = XColor.FromArgb(222, 226, 230);

            // Status colors
            var greenColor = XColor.FromArgb(16, 185, 129);
            var redColor = XColor.FromArgb(239, 68, 68);
            var orangeColor = XColor.FromArgb(245, 158, 11);

            // Fonts
            var companyFont = new XFont("Arial", 20, XFontStyleEx.Bold);
            var invoiceTitleFont = new XFont("Arial", 32, XFontStyleEx.Bold);
            var headerFont = new XFont("Arial", 12, XFontStyleEx.Bold);
            var labelFont = new XFont("Arial", 8, XFontStyleEx.Bold);
            var textFont = new XFont("Arial", 10, XFontStyleEx.Regular);
            var smallFont = new XFont("Arial", 8, XFontStyleEx.Regular);

            double y = 50;
            double leftMargin = 50;
            double rightMargin = page.Width.Point - 50;
            double contentWidth = rightMargin - leftMargin;

            // HEADER SECTION
            gfx.DrawString("Aarvi Enterprise", companyFont, new XSolidBrush(darkText), leftMargin, y);
            gfx.DrawString("INVOICE", invoiceTitleFont, new XSolidBrush(darkText),
                new XRect(0, y - 5, rightMargin, 35), XStringFormats.TopRight);
            y += 25;

            gfx.DrawString("Anil Patil", smallFont, new XSolidBrush(grayText), leftMargin, y);
            gfx.DrawString($"INV{_invoice.InvoiceId:D4}", headerFont, new XSolidBrush(darkText),
                new XRect(0, y - 2, rightMargin, 20), XStringFormats.TopRight);
            y += 12;

            // Company details
            gfx.DrawString("Business Number 9868964797", smallFont, new XSolidBrush(grayText), leftMargin, y);
            y += 12;
            gfx.DrawString("Patil niwas, opposite priya haveli,", smallFont, new XSolidBrush(grayText), leftMargin, y);
            y += 10;
            gfx.DrawString("near madhuaai temple, waghbil", smallFont, new XSolidBrush(grayText), leftMargin, y);
            y += 10;
            gfx.DrawString("gaon, GB road, Thane W, 400615", smallFont, new XSolidBrush(grayText), leftMargin, y);
            y += 10;
            gfx.DrawString("Thane, Maharashtra 400615", smallFont, new XSolidBrush(grayText), leftMargin, y);
            y += 12;
            gfx.DrawString("9980875757", smallFont, new XSolidBrush(grayText), leftMargin, y);
            y += 10;
            gfx.DrawString("aarvi.com", smallFont, new XSolidBrush(XColors.Blue), leftMargin, y);
            y += 10;
            gfx.DrawString("aarvienterprise@gmail.com", smallFont, new XSolidBrush(grayText), leftMargin, y);
            y += 25;

            // DATE, STATUS, BALANCE DUE Section
            double col1X = leftMargin;
            double col2X = leftMargin + (contentWidth / 3);
            double col3X = leftMargin + (contentWidth * 2 / 3);

            gfx.DrawString("DATE", labelFont, new XSolidBrush(XColors.Black), col1X, y);
            gfx.DrawString("STATUS", labelFont, new XSolidBrush(XColors.Black), col2X, y);

            string status = _invoice.Status ?? "Unpaid";
            string balanceLabel = status.Equals("Paid", StringComparison.OrdinalIgnoreCase) ? "AMOUNT PAID" : "BALANCE DUE";
            gfx.DrawString(balanceLabel, labelFont, new XSolidBrush(XColors.Black),
                new XRect(col3X, y, rightMargin - col3X, 15), XStringFormats.TopRight);
            y += 13;

            gfx.DrawString(_invoice.InvoiceDate.ToString("MMM d, yyyy"), textFont, new XSolidBrush(darkText), col1X, y);

            // Draw status with color
            XColor statusColor;
            string statusText;
            switch (status.ToLower())
            {
                case "paid":
                    statusColor = greenColor;
                    statusText = "✓ PAID";
                    break;
                case "pending":
                    statusColor = orangeColor;
                    statusText = "⏳ PENDING";
                    break;
                default:
                    statusColor = redColor;
                    statusText = "⚠ DUE";
                    break;
            }

            // Draw status badge
            DrawRoundedBox(gfx, col2X, y - 2, 60, 14, statusColor);
            gfx.DrawString(statusText, new XFont("Arial", 8, XFontStyleEx.Bold),
                new XSolidBrush(XColors.White), col2X + 5, y + 8);

            gfx.DrawString($"INR ₹{_invoice.Amount:N2}", textFont, new XSolidBrush(darkText),
                new XRect(col3X, y, rightMargin - col3X, 20), XStringFormats.TopRight);
            y += 30;

            // BILL TO Section
            gfx.DrawString("BILL TO", labelFont, new XSolidBrush(XColors.Black), leftMargin, y);
            y += 13;
            gfx.DrawString(_invoice.CustomerName ?? "-", headerFont, new XSolidBrush(darkText), leftMargin, y);
            y += 14;

            if (!string.IsNullOrEmpty(_invoice.CustomerAddress))
            {
                gfx.DrawString(_invoice.CustomerAddress, smallFont, new XSolidBrush(grayText), leftMargin, y);
                y += 11;
            }
            if (!string.IsNullOrEmpty(_invoice.CustomerCity))
            {
                gfx.DrawString(_invoice.CustomerCity, smallFont, new XSolidBrush(grayText), leftMargin, y);
                y += 11;
            }
            if (!string.IsNullOrEmpty(_invoice.CustomerZipCode))
            {
                gfx.DrawString(_invoice.CustomerZipCode, smallFont, new XSolidBrush(grayText), leftMargin, y);
                y += 11;
            }
            if (!string.IsNullOrEmpty(_invoice.CustomerPhone))
            {
                gfx.DrawString(_invoice.CustomerPhone, smallFont, new XSolidBrush(grayText), leftMargin, y);
                y += 11;
            }
            if (!string.IsNullOrEmpty(_invoice.CustomerEmail))
            {
                gfx.DrawString(_invoice.CustomerEmail, smallFont, new XSolidBrush(grayText), leftMargin, y);
                y += 11;
            }
            y += 15;

            // ITEMS TABLE HEADER
            gfx.DrawLine(new XPen(XColors.Black, 2), leftMargin, y, rightMargin, y);
            y += 12;

            double descColX = leftMargin;
            double rateColX = leftMargin + contentWidth * 0.55;
            double qtyColX = leftMargin + contentWidth * 0.73;
            double amtColX = leftMargin + contentWidth * 0.82;

            gfx.DrawString("DESCRIPTION", labelFont, new XSolidBrush(XColors.Black), descColX, y);
            gfx.DrawString("RATE", labelFont, new XSolidBrush(XColors.Black),
                new XRect(rateColX, y, 80, 15), XStringFormats.TopRight);
            gfx.DrawString("QTY", labelFont, new XSolidBrush(XColors.Black),
                new XRect(qtyColX, y, 50, 15), XStringFormats.TopRight);
            gfx.DrawString("AMOUNT", labelFont, new XSolidBrush(XColors.Black),
                new XRect(amtColX, y, rightMargin - amtColX, 15), XStringFormats.TopRight);
            y += 15;

            // Item Rows
            if (_invoice.InvoiceItems != null && _invoice.InvoiceItems.Count > 0)
            {
                foreach (var item in _invoice.InvoiceItems)
                {
                    gfx.DrawLine(new XPen(borderColor, 1), leftMargin, y, rightMargin, y);
                    y += 12;

                    gfx.DrawString(item.ItemName ?? "-", new XFont("Arial", 11, XFontStyleEx.Bold),
                        new XSolidBrush(darkText), descColX, y);
                    y += 13;

                    if (!string.IsNullOrEmpty(item.Description))
                    {
                        var descLines = WrapText(item.Description, 50);
                        foreach (var line in descLines)
                        {
                            gfx.DrawString(line, smallFont, new XSolidBrush(grayText), descColX, y);
                            y += 10;
                        }
                    }

                    double itemRowY = y - (string.IsNullOrEmpty(item.Description) ? 13 : 23);
                    gfx.DrawString($"₹{item.Rate:N2}", textFont, new XSolidBrush(darkText),
                        new XRect(rateColX, itemRowY, 80, 15), XStringFormats.TopRight);
                    gfx.DrawString(item.Quantity.ToString(), textFont, new XSolidBrush(darkText),
                        new XRect(qtyColX, itemRowY, 50, 15), XStringFormats.TopRight);
                    gfx.DrawString($"₹{item.Amount:N2}", textFont, new XSolidBrush(darkText),
                        new XRect(amtColX, itemRowY, rightMargin - amtColX, 15), XStringFormats.TopRight);

                    y += 10;
                }
            }

            y += 25;

            // TOTALS SECTION
            double totalsX = rightMargin - 200;

            // Subtotal
            gfx.DrawString("SUBTOTAL", textFont, new XSolidBrush(darkText),
                new XRect(totalsX, y, 100, 15), XStringFormats.TopRight);
            gfx.DrawString($"₹{_invoice.Subtotal:N2}", textFont, new XSolidBrush(darkText),
                new XRect(totalsX + 105, y, 95, 15), XStringFormats.TopRight);
            y += 15;

            // Discount
            gfx.DrawString($"DISCOUNT ({_invoice.DiscountPercentage}%)", textFont, new XSolidBrush(darkText),
                new XRect(totalsX, y, 100, 15), XStringFormats.TopRight);
            gfx.DrawString($"-₹{_invoice.DiscountAmount:N2}", textFont, new XSolidBrush(darkText),
                new XRect(totalsX + 105, y, 95, 15), XStringFormats.TopRight);
            y += 20;

            // Separator
            gfx.DrawLine(new XPen(borderColor, 1), totalsX, y, rightMargin, y);
            y += 15;

            // Total
            gfx.DrawString("TOTAL", headerFont, new XSolidBrush(darkText),
                new XRect(totalsX, y, 100, 15), XStringFormats.TopRight);
            gfx.DrawString($"₹{_invoice.Amount:N2}", headerFont, new XSolidBrush(darkText),
                new XRect(totalsX + 105, y, 95, 15), XStringFormats.TopRight);
            y += 30;

            // BALANCE DUE / PAID Box
            XColor boxColor = status.Equals("Paid", StringComparison.OrdinalIgnoreCase)
                ? XColor.FromArgb(236, 253, 245)  // Light green for paid
                : lightBg;  // Light gray for unpaid/pending

            DrawBox(gfx, rightMargin - 280, y, 280, 35, boxColor);
            gfx.DrawString(balanceLabel, headerFont,
                new XSolidBrush(status.Equals("Paid", StringComparison.OrdinalIgnoreCase) ? greenColor : XColors.Black),
                rightMargin - 265, y + 10);
            gfx.DrawString($"INR ₹{_invoice.Amount:N2}",
                new XFont("Arial", 12, XFontStyleEx.Bold), new XSolidBrush(darkText),
                new XRect(rightMargin - 280, y + 18, 265, 20), XStringFormats.TopRight);

            doc.Save(filename);
        }

        private void DrawBox(XGraphics gfx, double x, double y, double width, double height, XColor color)
        {
            gfx.DrawRectangle(new XSolidBrush(color), x, y, width, height);
        }

        private void DrawRoundedBox(XGraphics gfx, double x, double y, double width, double height, XColor color)
        {
            gfx.DrawRectangle(new XSolidBrush(color), x, y, width, height);
        }

        private string[] WrapText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return new[] { text ?? "" };

            var words = text.Split(' ');
            var lines = new List<string>();
            var currentLine = "";

            foreach (var word in words)
            {
                if ((currentLine + " " + word).Length > maxLength)
                {
                    if (!string.IsNullOrEmpty(currentLine))
                        lines.Add(currentLine.Trim());
                    currentLine = word;
                }
                else
                {
                    currentLine += (string.IsNullOrEmpty(currentLine) ? "" : " ") + word;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
                lines.Add(currentLine.Trim());

            return lines.ToArray();
        }
    }
}