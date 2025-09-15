using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewCustomerWindow.xaml.Class
{
    public class Society
    {
        public int SocietyId { get; set; }
        public string SocietyName { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string ManagerName { get; set; }
        public int TotalCars { get; set; }
        public DateTime CreatedDate { get; set; }

        // Extra calculated fields (optional for display)
        public string MonthlyRevenue { get; set; } = "₹0";
        public string Satisfaction { get; set; } = "⭐️⭐️⭐️⭐️";
        public int ActiveCars => TotalCars; // for now, same as TotalCars
    }
}
