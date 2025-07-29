using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NewCustomerWindow.xaml
{
    public partial class EmployeeDetailsWindow : Window
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;
        private bool isEditMode = false;
        private int existingEmployeeId = 0;

        public Employee NewEmployee { get; private set; }

        public EmployeeDetailsWindow()
        {
            InitializeComponent();
        }

        // ✅ New Constructor for Edit Mode
        public EmployeeDetailsWindow(Employee existingEmployee) : this()
        {
            if (existingEmployee != null)
            {
                isEditMode = true;
                existingEmployeeId = existingEmployee.Id;

                // Pre-fill the form
                NameBox.Text = existingEmployee.Name;
                EmailBox.Text = existingEmployee.Email;
                DepartmentBox.Text = existingEmployee.Department;
                MobileBox.Text = existingEmployee.Mobile;

                // For simplicity: You can also load the rest if necessary
            }
        }

        private void BackToList_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Prepare data
            string fullName = NameBox.Text;
            string email = EmailBox.Text;
            string department = DepartmentBox.Text;
            string mobile = MobileBox.Text;

            // Additional fields as needed
            StringBuilder selectedFields = new StringBuilder();
            foreach (var item in FieldBox.SelectedItems)
            {
                if (item is ListBoxItem listBoxItem)
                {
                    selectedFields.Append(listBoxItem.Content.ToString()).Append(", ");
                }
            }
            string fieldsString = selectedFields.ToString().TrimEnd(',', ' ');

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd;

                    if (isEditMode)
                    {
                        // ✅ UPDATE QUERY
                        cmd = new SqlCommand(@"UPDATE Employees SET 
                            FullName = @FullName,
                            Email = @Email,
                            Department = @Department,
                            MobilePhone = @MobilePhone,
                            Field = @Field
                            WHERE Id = @Id", conn);

                        cmd.Parameters.AddWithValue("@Id", existingEmployeeId);
                    }
                    else
                    {
                        // ✅ INSERT QUERY
                        cmd = new SqlCommand(@"INSERT INTO Employees 
                            (FullName, Email, Department, MobilePhone, Field)
                            VALUES 
                            (@FullName, @Email, @Department, @MobilePhone, @Field)", conn);
                    }

                    // Common parameters
                    cmd.Parameters.AddWithValue("@FullName", fullName);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Department", department);
                    cmd.Parameters.AddWithValue("@MobilePhone", mobile);
                    cmd.Parameters.AddWithValue("@Field", fieldsString);

                    cmd.ExecuteNonQuery();
                }

                // For communication back to the caller
                NewEmployee = new Employee
                {
                    Id = existingEmployeeId,
                    Name = fullName,
                    Email = email,
                    Department = department,
                    Mobile = mobile
                };

                MessageBox.Show(isEditMode ? "✅ Employee updated!" : "✅ Employee added!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Database Error: " + ex.Message);
            }
        }
    }
}
