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
        // Valid service types
        private static readonly HashSet<string> ValidServiceTypes = new HashSet<string>
        {
            "Aarvi Water Supplier",
            "Aarvi Car Washing",
            "Aarvi Laundry Service"
        };

        public static List<Invoice> GetAllInvoices()
        {
            var invoices = new List<Invoice>();

            string connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT i.InvoiceId, i.CustomerId, c.FullName AS CustomerName, 
                           i.ServiceType, i.Description, i.Amount, i.Status, i.InvoiceDate,
                           ISNULL(i.Subtotal, i.Amount) AS Subtotal,
                           ISNULL(i.DiscountPercentage, 0) AS DiscountPercentage,
                           COUNT(ii.ItemId) AS ItemCount
                    FROM Invoices i
                    INNER JOIN Customers c ON i.CustomerId = c.Id
                    LEFT JOIN InvoiceItems ii ON i.InvoiceId = ii.InvoiceId
                    WHERE i.ServiceType IN ('Aarvi Water Supplier', 'Aarvi Car Washing', 'Aarvi Laundry Service')
                    GROUP BY i.InvoiceId, i.CustomerId, c.FullName, i.ServiceType, 
                             i.Description, i.Amount, i.Status, i.InvoiceDate, 
                             i.Subtotal, i.DiscountPercentage";

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
                        InvoiceDate = Convert.ToDateTime(reader["InvoiceDate"]),
                        Subtotal = (decimal)Convert.ToDouble(reader["Subtotal"]),
                        DiscountPercentage = (decimal)Convert.ToDouble(reader["DiscountPercentage"])
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
                // Note: InvoiceItems will be deleted automatically due to CASCADE delete
                string query = "DELETE FROM Invoices WHERE InvoiceId = @InvoiceId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateInvoice(Invoice invoice)
        {
            // Validate service type
            if (!ValidServiceTypes.Contains(invoice.InvoiceType))
            {
                throw new ArgumentException($"Invalid service type: {invoice.InvoiceType}. Must be one of: {string.Join(", ", ValidServiceTypes)}");
            }

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
                        CustomerId = @CustomerId,
                        Subtotal = @Subtotal,
                        DiscountPercentage = @DiscountPercentage,
                        DiscountAmount = @DiscountAmount
                    WHERE InvoiceId = @InvoiceId";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ServiceType", invoice.InvoiceType);
                cmd.Parameters.AddWithValue("@Description", invoice.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Amount", invoice.Amount);
                cmd.Parameters.AddWithValue("@Status", invoice.Status);
                cmd.Parameters.AddWithValue("@InvoiceDate", invoice.InvoiceDate);
                cmd.Parameters.AddWithValue("@CustomerId", invoice.CustomerId);
                cmd.Parameters.AddWithValue("@InvoiceId", invoice.InvoiceId);
                cmd.Parameters.AddWithValue("@Subtotal", invoice.Subtotal);
                cmd.Parameters.AddWithValue("@DiscountPercentage", invoice.DiscountPercentage);
                cmd.Parameters.AddWithValue("@DiscountAmount", invoice.DiscountAmount);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Get complete invoice with all items and customer details
        /// </summary>
        public static Invoice GetInvoiceById(int invoiceId)
        {
            Invoice invoice = null;
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // Use the stored procedure for efficient loading
                using (SqlCommand cmd = new SqlCommand("sp_GetInvoiceDetails", conn))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // First result set: Invoice header with customer details
                        if (reader.Read())
                        {
                            invoice = new Invoice
                            {
                                InvoiceId = Convert.ToInt32(reader["InvoiceId"]),
                                CustomerId = Convert.ToInt32(reader["CustomerId"]),
                                CustomerName = reader["CustomerName"]?.ToString(),
                                InvoiceType = reader["ServiceType"]?.ToString(),
                                ServiceType = reader["ServiceType"]?.ToString(),
                                Description = reader["Description"]?.ToString(),
                                Amount = Convert.ToDecimal(reader["TotalAmount"]),
                                Status = reader["Status"]?.ToString(),
                                InvoiceDate = Convert.ToDateTime(reader["InvoiceDate"]),
                                Subtotal = Convert.ToDecimal(reader["Subtotal"]),
                                DiscountPercentage = Convert.ToDecimal(reader["DiscountPercentage"]),
                                DiscountAmount = Convert.ToDecimal(reader["DiscountAmount"]),

                                // Customer details for invoice display
                                CustomerAddress = reader["Address"]?.ToString(),
                                CustomerCity = $"{reader["City"]}, {reader["State"]} {reader["ZipCode"]}",
                                CustomerZipCode = reader["ZipCode"]?.ToString(),
                                CustomerPhone = reader["Phone"]?.ToString(),
                                CustomerEmail = reader["Email"]?.ToString()
                            };
                        }

                        if (invoice == null) return null;

                        // Second result set: Invoice items
                        if (reader.NextResult())
                        {
                            invoice.InvoiceItems = new List<InvoiceItem>();
                            while (reader.Read())
                            {
                                invoice.InvoiceItems.Add(new InvoiceItem
                                {
                                    ItemId = Convert.ToInt32(reader["ItemId"]),
                                    InvoiceId = Convert.ToInt32(reader["InvoiceId"]),
                                    ItemName = reader["ItemName"]?.ToString(),
                                    Description = reader["Description"]?.ToString(),
                                    Rate = Convert.ToDecimal(reader["Rate"]),
                                    Quantity = Convert.ToInt32(reader["Quantity"])
                                });
                            }
                        }

                        // Third result set: Water details (if applicable)
                        if (reader.NextResult() && reader.Read())
                        {
                            invoice.WaterDetails = new EmployeeManagerWPF.Models.WaterServices
                            {
                                OrderID = Convert.ToInt32(reader["WaterInvoiceId"]),
                                InvoiceId = Convert.ToInt32(reader["InvoiceId"]),
                                DeliveryDate = Convert.ToDateTime(reader["DeliveryDate"]),
                                Brand = reader["Brand"]?.ToString(),
                                Quantity = reader["Quantity"]?.ToString(),
                                Units = Convert.ToInt32(reader["Units"]),
                                Address = reader["Address"]?.ToString()
                            };
                        }
                    }
                }

                // If no items were loaded, create a single default item for backward compatibility
                if (invoice != null && (invoice.InvoiceItems == null || invoice.InvoiceItems.Count == 0))
                {
                    invoice.InvoiceItems = new List<InvoiceItem>
                    {
                        new InvoiceItem
                        {
                            InvoiceId = invoice.InvoiceId,
                            ItemName = invoice.InvoiceType ?? "Service",
                            Description = invoice.Description,
                            Rate = invoice.Amount,
                            Quantity = 1
                        }
                    };

                    // Set subtotal if not already set
                    if (invoice.Subtotal == 0)
                    {
                        invoice.Subtotal = invoice.Amount;
                    }
                }
            }

            return invoice;
        }

        /// <summary>
        /// Create a new invoice with multiple items
        /// </summary>
        public static int CreateInvoice(Invoice invoice, List<InvoiceItem> items)
        {
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Calculate totals
                        decimal subtotal = 0;
                        foreach (var item in items)
                        {
                            subtotal += item.Rate * item.Quantity;
                        }

                        invoice.Subtotal = subtotal;
                        invoice.DiscountAmount = subtotal * (invoice.DiscountPercentage / 100);
                        invoice.Amount = subtotal - invoice.DiscountAmount;

                        // Insert invoice
                        int invoiceId;
                        string insertInvoice = @"
                            INSERT INTO Invoices (CustomerId, ServiceType, InvoiceDate, Amount, 
                                                 Description, Status, InvoiceType, CustomerName, 
                                                 Subtotal, DiscountPercentage)
                            VALUES (@CustomerId, @ServiceType, @InvoiceDate, @Amount, 
                                   @Description, @Status, @InvoiceType, @CustomerName, 
                                   @Subtotal, @DiscountPercentage);
                            SELECT SCOPE_IDENTITY();";

                        using (SqlCommand cmd = new SqlCommand(insertInvoice, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CustomerId", invoice.CustomerId);
                            cmd.Parameters.AddWithValue("@ServiceType", invoice.ServiceType ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@InvoiceDate", invoice.InvoiceDate);
                            cmd.Parameters.AddWithValue("@Amount", invoice.Amount);
                            cmd.Parameters.AddWithValue("@Description", invoice.Description ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Status", invoice.Status);
                            cmd.Parameters.AddWithValue("@InvoiceType", invoice.InvoiceType ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@CustomerName", invoice.CustomerName ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Subtotal", invoice.Subtotal);
                            cmd.Parameters.AddWithValue("@DiscountPercentage", invoice.DiscountPercentage);

                            invoiceId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // Insert invoice items
                        string insertItem = @"
                            INSERT INTO InvoiceItems (InvoiceId, ItemName, Description, Rate, Quantity)
                            VALUES (@InvoiceId, @ItemName, @Description, @Rate, @Quantity)";

                        foreach (var item in items)
                        {
                            using (SqlCommand cmd = new SqlCommand(insertItem, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                                cmd.Parameters.AddWithValue("@ItemName", item.ItemName);
                                cmd.Parameters.AddWithValue("@Description", item.Description ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Rate", item.Rate);
                                cmd.Parameters.AddWithValue("@Quantity", item.Quantity);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Update discount amount
                        string updateDiscount = "UPDATE Invoices SET DiscountAmount = Subtotal * (DiscountPercentage / 100) WHERE InvoiceId = @InvoiceId";
                        using (SqlCommand cmd = new SqlCommand(updateDiscount, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return invoiceId;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public static List<Invoice> GetInvoicesByCustomerId(int customerId)
        {
            var invoices = new List<Invoice>();
            string connStr = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"
                    SELECT i.InvoiceId, i.InvoiceType, i.ServiceType, i.Description, 
                           i.Amount, i.Status, i.InvoiceDate,
                           ISNULL(i.Subtotal, i.Amount) AS Subtotal,
                           ISNULL(i.DiscountPercentage, 0) AS DiscountPercentage
                    FROM Invoices i
                    INNER JOIN Customers c ON i.CustomerId = c.Id
                    WHERE c.Id = @CustomerId
                    AND i.ServiceType IN ('Aarvi Water Supplier', 'Aarvi Car Washing', 'Aarvi Laundry Service')
                    ORDER BY i.InvoiceDate DESC";

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
                            ServiceType = reader["ServiceType"].ToString(),
                            Description = reader["Description"].ToString(),
                            Amount = (decimal)Convert.ToDouble(reader["Amount"]),
                            Status = reader["Status"].ToString(),
                            InvoiceDate = Convert.ToDateTime(reader["InvoiceDate"]),
                            Subtotal = (decimal)Convert.ToDouble(reader["Subtotal"]),
                            DiscountPercentage = (decimal)Convert.ToDouble(reader["DiscountPercentage"])
                        });
                    }
                }
            }

            return invoices;
        }
    }
}