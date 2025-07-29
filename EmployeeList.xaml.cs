using NewCustomerWindow.xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NewCustomerWindow
{
    public partial class EmployeeList : Window
    {
        private List<Employee> _allEmployees;
        private List<Employee> _filteredEmployees;

        private int _currentPage = 1;
        private int _rowsPerPage = 10;
        private int _totalPages;

        public EmployeeList()
        {
            InitializeComponent();

            LoadEmployeesFromDatabase();
        }

        private void LoadEmployeesFromDatabase()
        {
            _allEmployees = EmployeeService.GetAllEmployees(); // Must connect to DB
            _filteredEmployees = _allEmployees;
            ApplyPagination();
        }

        private void BackToHome_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Back to Home (not implemented)");
        }

        private void CreateEmployee_Click(object sender, RoutedEventArgs e)
        {
            var detailsWindow = new EmployeeDetailsWindow();
            if (detailsWindow.ShowDialog() == true)
            {
                var newEmp = detailsWindow.NewEmployee;
                EmployeeService.AddEmployee(newEmp); // Save to DB
                LoadEmployeesFromDatabase();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Employee selected)
            {
                var editWindow = new EmployeeDetailsWindow(selected); // pass selected employee
                if (editWindow.ShowDialog() == true)
                {
                    Employee updatedEmp = editWindow.NewEmployee;
                    updatedEmp.Id = selected.Id; // ensure ID is preserved

                    EmployeeService.UpdateEmployee(updatedEmp); // Update in DB
                    LoadEmployeesFromDatabase();
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Employee employeeToDelete)
            {
                var result = MessageBox.Show($"Are you sure you want to delete {employeeToDelete.Name}?", "Confirm Delete", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    EmployeeService.DeleteEmployee(employeeToDelete.Id); // Remove from DB
                    LoadEmployeesFromDatabase();
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterData();
        }

        private void FilterData()
        {
            string searchText = SearchBox.Text.Trim().ToLower();

            _filteredEmployees = _allEmployees
                .Where(emp => emp.Name.ToLower().Contains(searchText) ||
                              emp.Email.ToLower().Contains(searchText) ||
                              emp.Department.ToLower().Contains(searchText) ||
                              emp.Mobile.ToLower().Contains(searchText))
                .ToList();

            _currentPage = 1;
            ApplyPagination();
        }

        private void RefreshGrid()
        {
            ApplyPagination();
        }

        private void ApplyPagination()
        {
            _totalPages = (_filteredEmployees.Count + _rowsPerPage - 1) / _rowsPerPage;

            var pageItems = _filteredEmployees
                .Skip((_currentPage - 1) * _rowsPerPage)
                .Take(_rowsPerPage)
                .ToList();

            EmployeeGrid.ItemsSource = pageItems;
            PageText.Text = $"Page {_currentPage} of {_totalPages}";
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                ApplyPagination();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                ApplyPagination();
            }
        }
    }
}
