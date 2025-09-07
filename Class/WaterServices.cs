using System;

namespace NewCustomerWindow.xaml.Class
{
    public class WaterServices
    {
        public int OrderID { get; set; }      // Primary Key (WaterInvoiceId in DB)
        public int InvoiceId { get; set; }    // 🔗 Foreign Key (link to InvoiceId)
        public DateTime DeliveryDate { get; set; } // Delivery date

        public string Brand { get; set; }     // e.g., Bisleri, Kinley
        public string Quantity { get; set; }  // e.g., "20 Liters"
        public int Units { get; set; }        // Number of bottles
        public string Address { get; set; }   // Delivery address

        public string CustomerName { get; set; }
    }
}
