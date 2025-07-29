using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewCustomerWindow

{
    public class Invoice
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string InvoiceType { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public string Status { get; set; }
        public DateTime InvoiceDate { get; set; }

    }
}
