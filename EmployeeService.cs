using NewCustomerWindow.xaml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace NewCustomerWindow
{
    public static class EmployeeService
    {
        private static readonly string connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

        public static List<Employee> GetAllEmployees()
        {
            var employees = new List<Employee>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = @"SELECT Id, FullName, Email, Department, MobilePhone FROM Employees";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    employees.Add(new Employee
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Name = reader["FullName"].ToString(),
                        Email = reader["Email"].ToString(),
                        Department = reader["Department"].ToString(),
                        Mobile = reader["MobilePhone"].ToString()
                    });
                }
            }

            return employees;
        }

        public static void AddEmployee(Employee employee)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO Employees (FullName, Email, Department, MobilePhone) 
                      VALUES (@FullName, @Email, @Department, @MobilePhone)", conn);

                cmd.Parameters.AddWithValue("@FullName", employee.Name);
                cmd.Parameters.AddWithValue("@Email", employee.Email);
                cmd.Parameters.AddWithValue("@Department", employee.Department);
                cmd.Parameters.AddWithValue("@MobilePhone", employee.Mobile);

                cmd.ExecuteNonQuery();
            }
        }

        public static void UpdateEmployee(Employee employee)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(
                    @"UPDATE Employees 
                      SET FullName = @FullName, 
                          Email = @Email, 
                          Department = @Department, 
                          MobilePhone = @MobilePhone 
                      WHERE Id = @Id", conn);

                cmd.Parameters.AddWithValue("@Id", employee.Id);
                cmd.Parameters.AddWithValue("@FullName", employee.Name);
                cmd.Parameters.AddWithValue("@Email", employee.Email);
                cmd.Parameters.AddWithValue("@Department", employee.Department);
                cmd.Parameters.AddWithValue("@MobilePhone", employee.Mobile);

                cmd.ExecuteNonQuery();
            }
        }

        public static void DeleteEmployee(int employeeId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("DELETE FROM Employees WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", employeeId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
