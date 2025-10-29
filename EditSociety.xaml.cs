using System.Windows;
using System.Windows.Input;
using System.Data.SqlClient;
using System.Configuration;
using NewCustomerWindow.xaml;

namespace NewCustomerWindow
{
    public partial class EditSociety : Window
    {
        public Society CurrentSociety { get; private set; }
        private string connectionString;

        public EditSociety(Society society)
        {
            InitializeComponent();
            CurrentSociety = society;
            connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            // Pre-fill form
            txtSocietyNameEdit.Text = society.SocietyName;
            txtAddressEdit.Text = society.Address;
            txtContactNumberEdit.Text = society.Phone;
            txtManagerNameEdit.Text = society.ManagerName;
            txtTotalCarsEdit.Text = society.ActiveCars.ToString();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Update object
            CurrentSociety.SocietyName = txtSocietyNameEdit.Text;
            CurrentSociety.Address = txtAddressEdit.Text;
            CurrentSociety.Phone = txtContactNumberEdit.Text;
            CurrentSociety.ManagerName = txtManagerNameEdit.Text;

            if (int.TryParse(txtTotalCarsEdit.Text, out int cars))
                CurrentSociety.ActiveCars = cars;
            else
                CurrentSociety.ActiveCars = 0;

            // 🔹 Update in Database
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"UPDATE CarWashingSocieties 
                                     SET SocietyName=@name, Address=@address, ContactNumber=@phone, ManagerName=@manager
                                     WHERE SocietyId=@id";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@name", CurrentSociety.SocietyName);
                    cmd.Parameters.AddWithValue("@address", CurrentSociety.Address);
                    cmd.Parameters.AddWithValue("@phone", CurrentSociety.Phone);
                    cmd.Parameters.AddWithValue("@manager", CurrentSociety.ManagerName);
                    cmd.Parameters.AddWithValue("@id", CurrentSociety.SocietyId);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Changes saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error updating database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var confirm = MessageBox.Show("Are you sure you want to delete this society?",
                                              "Confirm Delete",
                                              MessageBoxButton.YesNo,
                                              MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        conn.Open();

                        string query = "DELETE FROM CarWashingSocieties WHERE SocietyId=@id";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", CurrentSociety.SocietyId);

                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                        {
                            MessageBox.Show("Society deleted successfully!", "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                            this.DialogResult = true; // signal back to parent that delete happened
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Society not found or already deleted.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error deleting society: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
