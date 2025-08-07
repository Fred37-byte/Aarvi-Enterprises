using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace NewCustomerWindow.xaml
{
    public partial class EmployeeDetailsWindow : Window
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;
        private bool isEditMode = false;
        private int existingEmployeeId = 0;
        private bool isSaving = false; // To prevent double-save

        public Employee NewEmployee { get; private set; }

        public EmployeeDetailsWindow()
        {
            InitializeComponent();
        }

        public EmployeeDetailsWindow(Employee existingEmployee) : this()
        {
            if (existingEmployee != null)
            {
                isEditMode = true;
                existingEmployeeId = existingEmployee.Id;

                NameBox.Text = existingEmployee.Name;
                EmailBox.Text = existingEmployee.Email;
                DepartmentBox.Text = existingEmployee.Department;
                MobileBox.Text = existingEmployee.Mobile;

                // You can also pre-fill other fields if needed
            }
        }

        private void BackToList_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (isSaving)
            {
                MessageBox.Show("⏳ Already saving. Please wait...");
                return;
            }

            isSaving = true;

            string fullName = NameBox.Text.Trim();
            string email = EmailBox.Text.Trim().ToLower();
            string mobile = MobileBox.Text.Trim();
            string department = DepartmentBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(mobile))
            {
                MessageBox.Show("❗ Please fill out Name, Email, and Mobile.");
                isSaving = false;
                return;
            }

            string title = (TitleBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string status = (StatusBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            string field = GetSelectedFields();
            string address = AddressBox.Text.Trim();
            string postCode = PostCodeBox.Text.Trim();
            DateTime? dob = DobBox.SelectedDate;
            DateTime? dateStarted = DateStartedBox.SelectedDate;
            string reportsTo = ReportsToBox.Text.Trim();
            string partnerName = PartnerNameBox.Text.Trim();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    if (!isEditMode)
                    {
                        // Check for duplicates (case-insensitive)
                        string checkQuery = @"
                    SELECT COUNT(*) FROM Employees 
                    WHERE LOWER(Email) = @Email AND MobilePhone = @MobilePhone";

                        using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@Email", email);
                            checkCmd.Parameters.AddWithValue("@MobilePhone", mobile);

                            int duplicateCount = (int)checkCmd.ExecuteScalar();
                            if (duplicateCount > 0)
                            {
                                MessageBox.Show("⚠️ Duplicate entry: Same email and mobile already exist.");
                                isSaving = false;
                                return;
                            }
                        }
                    }

                    SqlCommand cmd;
                    if (isEditMode)
                    {
                        cmd = new SqlCommand(@"UPDATE Employees SET 
                    Title = @Title,
                    Status = @Status,
                    FullName = @FullName,
                    Field = @Field,
                    Address = @Address,
                    PostCode = @PostCode,
                    MobilePhone = @MobilePhone,
                    Email = @Email,
                    DateOfBirth = @DateOfBirth,
                    DateStarted = @DateStarted,
                    Department = @Department,
                    ReportsTo = @ReportsTo,
                    PartnerName = @PartnerName
                    WHERE Id = @Id", conn);
                        cmd.Parameters.AddWithValue("@Id", existingEmployeeId);
                    }
                    else
                    {
                        cmd = new SqlCommand(@"INSERT INTO Employees (
                    Title, Status, FullName, Field, Address, PostCode,
                    MobilePhone, Email, DateOfBirth, DateStarted, Department,
                    ReportsTo, PartnerName
                ) VALUES (
                    @Title, @Status, @FullName, @Field, @Address, @PostCode,
                    @MobilePhone, @Email, @DateOfBirth, @DateStarted, @Department,
                    @ReportsTo, @PartnerName
                )", conn);
                    }

                    cmd.Parameters.AddWithValue("@Title", title ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Status", status ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@FullName", fullName);
                    cmd.Parameters.AddWithValue("@Field", field);
                    cmd.Parameters.AddWithValue("@Address", address);
                    cmd.Parameters.AddWithValue("@PostCode", postCode);
                    cmd.Parameters.AddWithValue("@MobilePhone", mobile);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@DateOfBirth", (object)dob ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DateStarted", (object)dateStarted ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Department", department);
                    cmd.Parameters.AddWithValue("@ReportsTo", reportsTo);
                    cmd.Parameters.AddWithValue("@PartnerName", partnerName);

                    cmd.ExecuteNonQuery();
                }

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
                MessageBox.Show("❌ Error: " + ex.Message);
            }
            finally
            {
                isSaving = false;
            }
        }



        private string GetSelectedFields()
        {
            StringBuilder selectedFields = new StringBuilder();
            foreach (var item in FieldBox.SelectedItems)
            {
                if (item is ListBoxItem listBoxItem)
                {
                    selectedFields.Append(listBoxItem.Content.ToString()).Append(", ");
                }
            }
            return selectedFields.ToString().TrimEnd(',', ' ');
        }
    }
}
