using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewCustomerWindow.xaml.Class
{
    public class WaterServices
    {
        public int OrderID { get; set; }          // Primary Key
        public string Brand { get; set; }         // e.g., Bisleri, Kinley
        public string Quantity { get; set; }      // e.g., 5L, 10L, 20L
        public int Units { get; set; }            // Number of bottles
        public string Address { get; set; }       // Delivery address
    }
}
