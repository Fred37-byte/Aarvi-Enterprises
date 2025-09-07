using EmployeeManagerWPF.Models;
using NewCustomerWindow.xaml.Class;
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
                    SELECT i.InvoiceId, i.CustomerId, c.FullName AS CustomerName, 
                           i.ServiceType, i.Description, i.Amount, i.Status, i.InvoiceDate
                    FROM Invoices i
                    INNER JOIN Customers c ON i.CustomerId = c.Id";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    invoices.Add(new Invoice
                    {
                        InvoiceId = Convert.ToInt32(reader["InvoiceId"]),
                        CustomerId = Convert.ToInt32(reader["CustomerId"]),
                        CustomerName = reader["CustomerName"].ToString(),
                        InvoiceType = reader["ServiceType"].ToString(),
                        Description = reader["Description"].ToString(),
                        Amount = (decimal)Convert.ToDouble(reader["Amount"]),
                        Status = reader["Status"].ToString(),
                        InvoiceDate = Convert.ToDateTime(reader["InvoiceDate"])
                    });
                }
            }

            return invoices;
        }

        public static void DeleteInvoice(int invoiceId)
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = "DELETE FROM Invoices WHERE InvoiceId = @InvoiceId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
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
                    SET ServiceType = @ServiceType,
                        Description = @Description,
                        Amount = @Amount,
                        Status = @Status,
                        InvoiceDate = @InvoiceDate,
                        CustomerId = @CustomerId
                    WHERE InvoiceId = @InvoiceId";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ServiceType", invoice.InvoiceType);
                cmd.Parameters.AddWithValue("@Description", invoice.Description);
                cmd.Parameters.AddWithValue("@Amount", invoice.Amount);
                cmd.Parameters.AddWithValue("@Status", invoice.Status);
                cmd.Parameters.AddWithValue("@InvoiceDate", invoice.InvoiceDate);
                cmd.Parameters.AddWithValue("@CustomerId", invoice.CustomerId);
                cmd.Parameters.AddWithValue("@InvoiceId", invoice.InvoiceId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static Invoice GetInvoiceById(int invoiceId)
        {
            Invoice invoice = null;
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // 1. Fetch the main invoice
                string query = @"
            SELECT i.InvoiceId, i.CustomerId, c.FullName AS CustomerName, 
                   i.ServiceType, i.Description, i.Amount, i.Status, i.InvoiceDate
            FROM Invoices i
            LEFT JOIN Customers c ON i.CustomerId = c.Id
            WHERE i.InvoiceId = @InvoiceId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            invoice = new Invoice
                            {
                                InvoiceId = Convert.ToInt32(reader["InvoiceId"]),
                                CustomerId = Convert.ToInt32(reader["CustomerId"]),
                                CustomerName = reader["CustomerName"].ToString(),
                                InvoiceType = reader["ServiceType"].ToString(),
                                Description = reader["Description"].ToString(),
                                Amount = (decimal)Convert.ToDouble(reader["Amount"]),
                                Status = reader["Status"].ToString(),
                                InvoiceDate = Convert.ToDateTime(reader["InvoiceDate"])
                            };
                        }
                    }
                }

                if (invoice == null) return null;

                // 2. Fetch Water Order details if ServiceType = Water Supplier
                if (invoice.InvoiceType == "Aarvi Water Supplier")
                {
                    string waterQuery = @"
                SELECT WaterInvoiceId, InvoiceId, DeliveryDate, Brand, Quantity, Units, Address
                FROM WaterOrders
                WHERE InvoiceId = @InvoiceId";

                    using (SqlCommand cmd = new SqlCommand(waterQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@InvoiceId", invoice.InvoiceId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                invoice.WaterDetails = new WaterServices
                                {
                                    OrderID = Convert.ToInt32(reader["WaterInvoiceId"]),
                                    InvoiceId = Convert.ToInt32(reader["InvoiceId"]),
                                    DeliveryDate = Convert.ToDateTime(reader["DeliveryDate"]),
                                    Brand = reader["Brand"].ToString(),
                                    Quantity = reader["Quantity"].ToString(),
                                    Units = Convert.ToInt32(reader["Units"]),
                                    Address = reader["Address"].ToString()
                                };
                            }
                        }
                    }
                }

                // Later we can add Car Wash / Laundry similar blocks here
            }

            return invoice;
        }


        public static List<Invoice> GetInvoicesByCustomerId(int customerId)
        {
            var invoices = new List<Invoice>();
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"SELECT i.* 
                         FROM Invoices i
                         INNER JOIN Customers c ON i.CustomerId = c.Id
                         WHERE c.Id = @CustomerId";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CustomerId", customerId);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        invoices.Add(new Invoice
                        {
                            InvoiceId = (int)reader["InvoiceId"],
                            InvoiceType = reader["ServiceType"].ToString(),
                            Description = reader["Description"].ToString(),
                            Amount = (decimal)Convert.ToDouble(reader["Amount"]),
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
