using System;
using System.Collections.Generic;
using NewCustomerWindow.xaml.Class; // ✅ Needed so Invoice can see WaterServices

namespace EmployeeManagerWPF.Models
{
    // Main Invoice class
    public class Invoice
    {
        // Primary Key
        public int InvoiceId { get; set; }

        // Foreign Key - linked to Customer
        public int CustomerId { get; set; }

        // Customer Name (for display only, not DB relation ideally)
        public string CustomerName { get; set; }

        // Customer details for invoice display
        public string CustomerAddress { get; set; }
        public string CustomerCity { get; set; }
        public string CustomerZipCode { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }

        // Invoice type (e.g., Service, Product, Subscription)
        public string InvoiceType { get; set; }

        // Extra description/details about the invoice
        public string Description { get; set; }

        // Invoice Amount (this is the final total)
        public decimal Amount { get; set; }

        // Invoice Status (Paid, Pending, Cancelled, etc.)
        public string Status { get; set; }

        // Date when invoice was issued
        public DateTime InvoiceDate { get; set; }

        // Service Type (for categorization)
        public string ServiceType { get; set; }

        // 💰 Financial fields for itemized invoices
        public decimal Subtotal { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }

        // TotalAmount is the same as Amount (for compatibility)
        public decimal TotalAmount
        {
            get => Amount;
            set => Amount = value;
        }

        // Display properties
        public string DiscountText => $"DISCOUNT ({DiscountPercentage}%)";

        // 📋 Invoice items collection (for multi-item invoices)
        public List<InvoiceItem> InvoiceItems { get; set; }

        // 🔗 Navigation property for Water Supply details
        public WaterServices WaterDetails { get; set; }

        public Invoice()
        {
            InvoiceItems = new List<InvoiceItem>();
            InvoiceDate = DateTime.Now;
            Status = "Unpaid";
            DiscountPercentage = 0;
        }
    }

    // Invoice Item class for line items
    public class InvoiceItem
    {
        public int ItemId { get; set; }
        public int InvoiceId { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public decimal Rate { get; set; }
        public int Quantity { get; set; }
        public decimal Amount => Rate * Quantity;
    }

    // Water service details
    public class WaterServices
    {
        public int OrderID { get; set; }
        public int InvoiceId { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Brand { get; set; }
        public string Quantity { get; set; }
        public int Units { get; set; }
        public string Address { get; set; }
    }
}