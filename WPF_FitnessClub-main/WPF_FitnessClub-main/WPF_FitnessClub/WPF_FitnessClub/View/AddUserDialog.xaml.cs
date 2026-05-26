using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_FitnessClub.Models;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;

namespace WPF_FitnessClub.View
{
    public partial class AddUserDialog : Window
    {
        public User NewUser { get; private set; }
        private const int MaxPasswordLength = 30;

        public AddUserDialog()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> validationErrors = new List<string>();

            // 1. ВАЛИДАЦИЯ ФИО (сохраняю твой блок)
            if (string.IsNullOrWhiteSpace(FullNameTextBox.Text))
            {
                validationErrors.Add((string)Application.Current.Resources["FullNameRequired"]);
            }
            else
            {
                string trimmedName = FullNameTextBox.Text.Trim();
                if (!Regex.IsMatch(trimmedName, @"^[а-яА-Яa-zA-ZёЁ\s]+$"))
                {
                    validationErrors.Add((string)Application.Current.Resources["FullNameOnlyLetters"]);
                }
                else
                {
                    string[] nameParts = trimmedName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (LoginTextBox.Text.ToLower() != "admin" && nameParts.Length != 3)
                    {
                        validationErrors.Add((string)Application.Current.Resources["FullNameRequireThreeWords"]);
                    }
                }
            }

            // 2. ВАЛИДАЦИЯ EMAIL
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || !IsValidEmail(EmailTextBox.Text))
            {
                validationErrors.Add((string)Application.Current.Resources["InvalidEmail"]);
            }

            // 3. ВАЛИДАЦИЯ ЛОГИНА
            if (string.IsNullOrWhiteSpace(LoginTextBox.Text) || LoginTextBox.Text.Length < 3)
            {
                validationErrors.Add((string)Application.Current.Resources["UsernameTooShort"]);
            }

            // 4. ВАЛИДАЦИЯ ПАРОЛЯ
            if (string.IsNullOrWhiteSpace(PasswordBox.Password) || PasswordBox.Password.Length < 6)
            {
                validationErrors.Add("Пароль должен быть не менее 6 символов");
            }

            // 5. ВЫВОД ОШИБОК
            if (validationErrors.Count > 0)
            {
                MessageBox.Show(string.Join("\n", validationErrors), (string)Application.Current.Resources["ValidationErrorTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 6. СОЗДАНИЕ ПОЛЬЗОВАТЕЛЯ
            UserRole selectedRole = UserRole.Client;
            if (RoleComboBox.SelectedItem is ComboBoxItem selectedItem && int.TryParse(selectedItem.Tag?.ToString(), out int roleValue))
            {
                selectedRole = (UserRole)roleValue;
            }

            // Передаем данные в NewUser (пароль пока текстом)
            NewUser = new User(FullNameTextBox.Text.Trim(), EmailTextBox.Text.Trim(), LoginTextBox.Text.Trim(), PasswordBox.Password, selectedRole)
            {
                IsBlocked = IsBlockedCheckBox.IsChecked ?? false
            };

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
                
            try 
            {
                if (!email.Contains("@"))
                {
                    return false;
                }

                string[] parts = email.Split('@');
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                {
                    return false;
                }

                string localPart = parts[0];
                string domainPart = parts[1];
                
                if (!Regex.IsMatch(localPart, @"^[a-zA-Z0-9._\-]+$"))
                {
                    return false;
                }
                
                if (localPart.Contains(".."))
                {
                    return false;
                }
                
                if (!domainPart.Contains("."))
                {
                    return false;
                }
                
                if (domainPart.Contains(".."))
                {
                    return false;
                }
                
                if (domainPart.StartsWith(".") || domainPart.EndsWith("."))
                {
                    return false;
                }
                
                if (!Regex.IsMatch(domainPart, @"^[a-zA-Z0-9\.\-]+$"))
                {
                    return false;
                }

                return true;
            }
            catch 
            {
                return false;
            }
        }

        private void PasswordBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null)
            {
                if (passwordBox.Password.Length >= MaxPasswordLength && 
                    e.Key != Key.Back && e.Key != Key.Delete && 
                    e.Key != Key.Left && e.Key != Key.Right && 
                    e.Key != Key.Tab && e.Key != Key.Home && 
                    e.Key != Key.End && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    e.Handled = true;
                }
            }
        }
    }
} 