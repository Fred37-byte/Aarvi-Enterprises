// EmployeeManagerWPF.Models (Invoice.cs) -- small additions only
using System;
using System.Collections.Generic;

namespace EmployeeManagerWPF.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }
        public string CustomerCity { get; set; }
        public string CustomerZipCode { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerEmail { get; set; }

        public string InvoiceType { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string ServiceType { get; set; }

        public decimal Subtotal { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }

        public decimal TotalAmount
        {
            get => Amount;
            set => Amount = value;
        }

        public string DiscountText => $"DISCOUNT ({DiscountPercentage}%)";

        public List<InvoiceItem> InvoiceItems { get; set; }

        // NEW: Car Service details (for Car Washing / Car Service invoices)
        public CarServiceDetails CarDetails { get; set; }

        // Also kept WaterDetails for water-supply invoices
        public WaterServices WaterDetails { get; set; }

        public Invoice()
        {
            InvoiceItems = new List<InvoiceItem>();
            InvoiceDate = DateTime.Now;
            Status = "Unpaid";
            DiscountPercentage = 0;
            CarDetails = null; // will be populated later if applicable
        }
    }

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

    // NEW class: Car-specific details
    public class CarServiceDetails
    {
        // Example fields — adjust to match your DB
        public string VehicleModel { get; set; }         // e.g., "Innova Crysta (MH04J4548)"
        public string RegistrationNumber { get; set; }   // e.g., "MH04J4548"
        public string Package { get; set; }              // e.g., "SUV (Yearly)"
        public DateTime? LastServiceDate { get; set; }
        public string Notes { get; set; }
    }

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
