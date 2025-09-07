using System;
using NewCustomerWindow.xaml.Class; // ✅ Needed so Invoice can see WaterServices

namespace EmployeeManagerWPF.Models
{
    public class Invoice
    {
        // Primary Key
        public int InvoiceId { get; set; }

        // Foreign Key - linked to Customer
        public int CustomerId { get; set; }

        // Customer Name (for display only, not DB relation ideally)
        public string CustomerName { get; set; }

        // Invoice type (e.g., Service, Product, Subscription)
        public string InvoiceType { get; set; }

        // Extra description/details about the invoice
        public string Description { get; set; }

        // Invoice Amount
        public decimal Amount { get; set; }

        // Invoice Status (Paid, Pending, Cancelled, etc.)
        public string Status { get; set; }

        // Date when invoice was issued
        public DateTime InvoiceDate { get; set; }

        // 🔗 Navigation property for Water Supply details
        public WaterServices WaterDetails { get; set; }
    }
}
