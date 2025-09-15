using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NewCustomerWindow.xaml
{
    public partial class AddSocietyCarWindow : Window
    {
        private string connectionString;

        // This property will hold the new Society for the parent window
        public Society NewSociety { get; private set; }

        public AddSocietyCarWindow()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["EmployeeDB"].ConnectionString;

            // Set placeholders
            SetPlaceholders();
        }

        private void SetPlaceholders()
        {
            SetPlaceholder(txtSocietyName, "Enter society name");
            SetPlaceholder(txtAddress, "Enter complete address");
            SetPlaceholder(txtContactNumber, "Enter 10-digit number");
            SetPlaceholder(txtContactPerson, "Enter manager name");
            SetPlaceholder(txtTotalCars, "Enter number of cars");
        }

        private void SetPlaceholder(TextBox textBox, string placeholder)
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = placeholder;
                textBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text == textBox.Tag.ToString())
            {
                textBox.Text = "";
                textBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = textBox.Tag.ToString();
                textBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            if (textBox == txtSocietyName)
                errorSocietyName.Visibility = Visibility.Collapsed;
            else if (textBox == txtAddress)
                errorAddress.Visibility = Visibility.Collapsed;
            else if (textBox == txtContactNumber)
                errorContactNumber.Visibility = Visibility.Collapsed;
            else if (textBox == txtContactPerson)
                errorManagerName.Visibility = Visibility.Collapsed;
        }

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private bool ValidateForm()
        {
            bool isValid = true;

            errorSocietyName.Visibility = Visibility.Collapsed;
            errorAddress.Visibility = Visibility.Collapsed;
            errorContactNumber.Visibility = Visibility.Collapsed;
            errorManagerName.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(txtSocietyName.Text) ||
                txtSocietyName.Text == txtSocietyName.Tag.ToString())
            {
                errorSocietyName.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtAddress.Text) ||
                txtAddress.Text == txtAddress.Tag.ToString())
            {
                errorAddress.Visibility = Visibility.Visible;
                isValid = false;
            }

            string contactNumber = txtContactNumber.Text == txtContactNumber.Tag.ToString() ?
                                 "" : txtContactNumber.Text.Trim();
            if (string.IsNullOrEmpty(contactNumber) || contactNumber.Length != 10)
            {
                errorContactNumber.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtContactPerson.Text) ||
                txtContactPerson.Text == txtContactPerson.Tag.ToString())
            {
                errorManagerName.Visibility = Visibility.Visible;
                isValid = false;
            }

            return isValid;
        }

        private string GetTextBoxValue(TextBox textBox)
        {
            if (textBox.Text == textBox.Tag.ToString())
                return "";
            return textBox.Text.Trim();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtSocietyName.Text = "";
            txtAddress.Text = "";
            txtContactNumber.Text = "";
            txtContactPerson.Text = "";
            txtTotalCars.Text = "";

            SetPlaceholders();

            errorSocietyName.Visibility = Visibility.Collapsed;
            errorAddress.Visibility = Visibility.Collapsed;
            errorContactNumber.Visibility = Visibility.Collapsed;
            errorManagerName.Visibility = Visibility.Collapsed;

            SuccessMessage.Visibility = Visibility.Collapsed;

            txtSocietyName.Focus();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
                return;

            string societyName = GetTextBoxValue(txtSocietyName);
            string address = GetTextBoxValue(txtAddress);
            string managerName = GetTextBoxValue(txtContactPerson);
            string contactNumber = GetTextBoxValue(txtContactNumber);
            string totalCars = GetTextBoxValue(txtTotalCars);

            try
            {
                var btn = (Button)sender;
                btn.IsEnabled = false;
                btn.Content = "Saving...";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    string query = @"INSERT INTO CarWashingSocieties 
                                    (SocietyName, Address, ContactNumber, ManagerName, CreatedDate, TotalCars) 
                                    VALUES (@SocietyName, @Address, @ContactNumber, @ManagerName, GETDATE(), @TotalCars)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@SocietyName", societyName);
                        cmd.Parameters.AddWithValue("@Address", address);
                        cmd.Parameters.AddWithValue("@ContactNumber", contactNumber);
                        cmd.Parameters.AddWithValue("@ManagerName", managerName);
                        cmd.Parameters.AddWithValue("@TotalCars",
                            string.IsNullOrEmpty(totalCars) ? (object)DBNull.Value : totalCars);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                NewSociety = new Society
                {
                    SocietyName = societyName,
                    Address = address,
                    Phone = contactNumber,
                    ManagerName = managerName,
                    ActiveCars = int.TryParse(totalCars, out int cars) ? cars : 0,
                    MonthlyRevenue = "₹0",
                    Satisfaction = "N/A"
                };

                SuccessMessage.Visibility = Visibility.Visible;

                await Task.Delay(1000);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while saving: {ex.Message}",
                                "Database Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
            finally
            {
                if (IsLoaded)
                {
                    var btn = (Button)sender;
                    btn.IsEnabled = true;
                    btn.Content = "Add Society";
                }
            }
        }
    }
}
