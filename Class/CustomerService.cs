using System;
using System.Configuration;
using System.Data.SqlClient;

namespace NewCustomerWindow
{
    public static class CustomerService
    {
        public static Customer GetCustomerByName(string fullName)
        {
            Customer customer = null;
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = "SELECT * FROM Customers WHERE FullName = @FullName";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@FullName", fullName);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        customer = new Customer
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            FullName = reader["FullName"].ToString(),
                            Email = reader["Email"].ToString(),
                            Phone = reader["Phone"].ToString(),
                            Address = reader["Address"].ToString(),
                            City = reader["City"].ToString(),
                            State = reader["State"].ToString(),
                            ZipCode = reader["ZipCode"].ToString()
                        };
                    }
                }
            }

            return customer;
        }
    }
}
