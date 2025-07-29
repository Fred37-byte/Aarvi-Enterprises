using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace NewCustomerWindow
{
    public static class InvoiceService
    {
        public static List<Invoice> GetAllInvoices()
        {
            var invoices = new List<Invoice>();

            string connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT i.Id, c.FullName AS CustomerName, i.InvoiceType, i.Description,
                           i.Amount, i.Status, i.InvoiceDate
                    FROM Invoices i
                    INNER JOIN Customers c ON i.CustomerId = c.Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    invoices.Add(new Invoice
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        CustomerName = reader["CustomerName"].ToString(),
                        InvoiceType = reader["InvoiceType"].ToString(),
                        Description = reader["Description"].ToString(),
                        Amount = Convert.ToDouble(reader["Amount"]),
                        Status = reader["Status"].ToString(),
                        InvoiceDate = Convert.ToDateTime(reader["InvoiceDate"])

                    });
                }
            }

            return invoices;
        }
        public static void DeleteInvoice(int id)
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = "DELETE FROM Invoices WHERE Id = @Id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public static void UpdateInvoice(Invoice invoice)
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"
            UPDATE Invoices 
            SET InvoiceType = @InvoiceType,
                Description = @Description,
                Amount = @Amount,
                Status = @Status,
                InvoiceDate = @InvoiceDate
            WHERE Id = @Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@InvoiceType", invoice.InvoiceType);
                cmd.Parameters.AddWithValue("@Description", invoice.Description);
                cmd.Parameters.AddWithValue("@Amount", invoice.Amount);
                cmd.Parameters.AddWithValue("@Status", invoice.Status);
                cmd.Parameters.AddWithValue("@InvoiceDate", invoice.InvoiceDate);
                cmd.Parameters.AddWithValue("@Id", invoice.Id);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public static List<Invoice> GetInvoicesByCustomerName(string name)
        {
            var invoices = new List<Invoice>();
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"SELECT i.* FROM Invoices i
                         INNER JOIN Customers c ON i.CustomerId = c.Id
                         WHERE c.FullName = @FullName";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@FullName", name);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        invoices.Add(new Invoice
                        {
                            Id = (int)reader["Id"],
                            InvoiceType = reader["InvoiceType"].ToString(),
                            Description = reader["Description"].ToString(),
                            Amount = Convert.ToDouble(reader["Amount"]),
                            Status = reader["Status"].ToString(),
                            InvoiceDate = Convert.ToDateTime(reader["InvoiceDate"])
                        });
                    }
                }
            }

            return invoices;
        }



    }
}
