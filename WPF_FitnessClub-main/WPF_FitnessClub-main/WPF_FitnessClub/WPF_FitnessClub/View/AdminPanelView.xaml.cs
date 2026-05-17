using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_FitnessClub.ViewModels;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.View
{
    public partial class AdminPanelView : UserControl
    {
        private AdminPanelViewModel viewModel;
        private bool isViewModelInitialized = false;

        public AdminPanelView()
        {
            InitializeComponent();
            
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                viewModel = new AdminPanelViewModel();
                DataContext = viewModel;
                isViewModelInitialized = true;
                Mouse.OverrideCursor = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(string)Application.Current.Resources["AdminPanelInitError"]}: {ex.Message}", 
                    (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                Mouse.OverrideCursor = null;
            }
            
            Loaded += AdminPanelView_Loaded;
            Unloaded += AdminPanelView_Unloaded;
        }

        private void AdminPanelView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!isViewModelInitialized)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    viewModel = new AdminPanelViewModel();
                    DataContext = viewModel;
                    isViewModelInitialized = true;
                    Mouse.OverrideCursor = null;
                }
                else
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    viewModel.RefreshCommand.Execute(null);
                    Mouse.OverrideCursor = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{(string)Application.Current.Resources["AdminPanelLoadError"]}: {ex.Message}", 
                    (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                Mouse.OverrideCursor = null;
            }
        }

        private void AdminPanelView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel = null;
                isViewModelInitialized = false;
            }
        }
        
        private void RoleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is User user)
            {
                try
                {
                    if (comboBox.SelectedItem is ComboBoxItem selectedItem && 
                        int.TryParse(selectedItem.Tag?.ToString(), out int roleValue))
                    {
                        UserRole selectedRole = (UserRole)roleValue;
                        
                        UserRole oldRole = user.Role;
                        
                        if (oldRole == selectedRole)
                            return;
                            
                        if (user.Id == 1 && selectedRole != UserRole.Admin)
                        {
                            MessageBox.Show((string)Application.Current.Resources["AdminPanelDeleteAdminError"],
                                (string)Application.Current.Resources["AdminPanelLimitationTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
                                
                            comboBox.SelectionChanged -= RoleComboBox_SelectionChanged;
                            
                            foreach (ComboBoxItem item in comboBox.Items)
                            {
                                if (int.TryParse(item.Tag?.ToString(), out int tag) && tag == (int)oldRole)
                                {
                                    comboBox.SelectedItem = item;
                                    break;
                                }
                            }
                            
                            comboBox.SelectionChanged += RoleComboBox_SelectionChanged;
                            return;
                        }
                        
                        User currentUser = viewModel.GetCurrentUser();
                        if (currentUser != null && currentUser.Id == user.Id && selectedRole != UserRole.Admin && oldRole == UserRole.Admin)
                        {
                            MessageBox.Show((string)Application.Current.Resources["AdminPanelBlockSelfError"],
                                (string)Application.Current.Resources["AdminPanelLimitationTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
                                
                            comboBox.SelectionChanged -= RoleComboBox_SelectionChanged;
                            
                            foreach (ComboBoxItem item in comboBox.Items)
                            {
                                if (int.TryParse(item.Tag?.ToString(), out int tag) && tag == (int)oldRole)
                                {
                                    comboBox.SelectedItem = item;
                                    break;
                                }
                            }
                            
                            comboBox.SelectionChanged += RoleComboBox_SelectionChanged;
                            return;
                        }
                        
                        user.Role = selectedRole;
                        
                        Mouse.OverrideCursor = Cursors.Wait;
                        bool success = viewModel.SaveUserChanges(user);
                        Mouse.OverrideCursor = null;
                        
                        if (!success)
                        {
                            comboBox.SelectionChanged -= RoleComboBox_SelectionChanged;
                            
                            user.Role = oldRole;
                            
                            foreach (ComboBoxItem item in comboBox.Items)
                            {
                                if (int.TryParse(item.Tag?.ToString(), out int tag) && tag == (int)oldRole)
                                {
                                    comboBox.SelectedItem = item;
                                    break;
                                }
                            }
                            
                            comboBox.SelectionChanged += RoleComboBox_SelectionChanged;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{(string)Application.Current.Resources["RoleSelectionError"]}: {ex.Message}", 
                        (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                    Mouse.OverrideCursor = null;
                }
            }
        }

        private void RoleComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is User user)
            {
                int roleValue = (int)user.Role;
                
                foreach (ComboBoxItem item in comboBox.Items)
                {
                    if (int.TryParse(item.Tag?.ToString(), out int tag) && tag == roleValue)
                    {
                        comboBox.SelectionChanged -= RoleComboBox_SelectionChanged;
                        
                        comboBox.SelectedItem = item;
                        
                        comboBox.SelectionChanged += RoleComboBox_SelectionChanged;
                        break;
                    }
                }
            }
        }

        private void UsersDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Row.Item is User user)
                {
                    try
                    {
                        string newValue = string.Empty;
                        bool? isBlocked = null;
                        
                        if (e.EditingElement is TextBox textBox)
                        {
                            newValue = textBox.Text;
                        }
                        else if (e.EditingElement is CheckBox checkBox)
                        {
                            isBlocked = checkBox.IsChecked;
                        }
                        
                        string columnHeader = e.Column.Header.ToString();
                        
                        bool isValid = true;
                        string errorMessage = string.Empty;
                        
                        switch (columnHeader)
                        {
                            case "ФИО":
                                if (string.IsNullOrWhiteSpace(newValue))
                                {
                                    isValid = false;
                                    errorMessage = (string)Application.Current.Resources["AddUserValidationEmptyFullName"];
                                }
                                else
                                {
                                    user.FullName = newValue;
                                }
                                break;
                                
                            case "Email":
                                if (string.IsNullOrWhiteSpace(newValue) || !IsValidEmail(newValue))
                                {
                                    isValid = false;
                                    errorMessage = (string)Application.Current.Resources["AddUserValidationInvalidEmail"];
                                }
                                else if (newValue != user.Email && !viewModel.IsEmailUnique(newValue))
                                {
                                    isValid = false;
                                    errorMessage = (string)Application.Current.Resources["AddUserValidationEmailExists"];
                                }
                                else
                                {
                                    user.Email = newValue;
                                }
                                break;
                                
                            case "Логин":
                                if (string.IsNullOrWhiteSpace(newValue) || newValue.Length < 3)
                                {
                                    isValid = false;
                                    errorMessage = (string)Application.Current.Resources["AddUserValidationShortLogin"];
                                }
                                else if (newValue != user.Login && !viewModel.IsLoginUnique(newValue))
                                {
                                    isValid = false;
                                    errorMessage = (string)Application.Current.Resources["AddUserValidationLoginExists"];
                                }
                                else
                                {
                                    user.Login = newValue;
                                }
                                break;
                                
                            case "Заблокирован":
                                if (user.Id == 1 && isBlocked == true)
                                {
                                    isValid = false;
                                    errorMessage = (string)Application.Current.Resources["AdminPanelBlockAdminError"];
                                }
                                
                                User currentUser = viewModel.GetCurrentUser();
                                if (currentUser != null && currentUser.Id == user.Id && isBlocked == true)
                                {
                                    isValid = false;
                                    errorMessage = (string)Application.Current.Resources["AdminPanelBlockSelfError"];
                                }
                                
                                if (isValid && isBlocked.HasValue)
                                {
                                    user.IsBlocked = isBlocked.Value;
                                }
                                break;
                        }
                        
                        if (!isValid)
                        {
                            MessageBox.Show(errorMessage, (string)Application.Current.Resources["AdminPanelWarning"], MessageBoxButton.OK, MessageBoxImage.Warning);
                            e.Cancel = true;
                            return;
                        }
                        
                        if (columnHeader != "Роль")
                        {
                            Mouse.OverrideCursor = Cursors.Wait;
                            bool success = viewModel.SaveUserChanges(user);
                            Mouse.OverrideCursor = null;
                            
                            if (!success)
                            {
                                MessageBox.Show(string.Format((string)Application.Current.Resources["AdminPanelUpdateUserError"], user.Login), 
                                    (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                                e.Cancel = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"{(string)Application.Current.Resources["CellEditError"]}: {ex.Message}", 
                            (string)Application.Current.Resources["AdminPanelError"], MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                        Mouse.OverrideCursor = null;
                    }
                }
            }
        }
        
        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return System.Text.RegularExpressions.Regex.IsMatch(email, pattern);
        }
    }
} 