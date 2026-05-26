using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_FitnessClub.Models;
using static WPF_FitnessClub.Commands;
using System.Text.RegularExpressions;
using WPF_FitnessClub.Data;
using WPF_FitnessClub.Data.Services;
using System.Configuration;

namespace WPF_FitnessClub.ViewModels
{
	public class RegistrationVM : ViewModelBase
	{

		#region Приватные поля

		string _login;
		string _password;
		string _fullname;
		string _email;
		string _regLogin;
		string _regPassword;
		string _confirmPassword;
		string roleName;
		string welcomeMessage;

		private List<User> _users;
		private readonly UserService _userService;
		private readonly DatabaseConnectionService _databaseConnectionService;

		#endregion

		#region Свойства для привязки комманд

		public string Login
		{
			get => _login;
			set
			{
				if (_login != value)
				{
					_login = value;
					OnPropertyChanged(nameof(Login));
				}
			}
		}

		public string Password
		{
			get => _password;
			set
			{
				if (_password != value)
				{
					_password = value;
					OnPropertyChanged(nameof(Password));
				}
			}
		}

		public string FullName
		{
			get => _fullname;
			set
			{
				if (_fullname != value)
				{
					_fullname = value;
					OnPropertyChanged(nameof(FullName));
				}	
			}
		}

		public string Email
		{
			get => _email;
			set
			{
				if (_email != value)
				{
					_email = value;
					OnPropertyChanged(nameof(Email));
				}
			}
		}

		public string RegLogin
		{
			get => _regLogin;
			set
			{
				if (_regLogin != value)	
				{
					_regLogin = value;
					OnPropertyChanged(nameof(RegLogin));
				}
			}
		}

		public string RegPassword	
		{
			get => _regPassword;
			set
			{
				if (_regPassword != value)
				{
					_regPassword = value;
					OnPropertyChanged(nameof(RegPassword));
				}
			}
		}
		
		public string ConfirmPassword
		{
			get => _confirmPassword;
			set
			{
				if (_confirmPassword != value)
				{
					_confirmPassword = value;
					OnPropertyChanged(nameof(ConfirmPassword));
				}
			}	
		}

		
		#endregion


		#region Комманды

		public ICommand EnterCommand { get; set; }
		public ICommand RegisterCommand {  get; set; }

		#endregion

		#region События
		public event EventHandler RequestClose;
		#endregion

		#region Методы команд

		private void RaiseRequestClose()
		{
			RequestClose?.Invoke(this, EventArgs.Empty);
		}

        private void OpenMainWindow(User user, string plainPassword)
        {
            MainWindow mainWindow = new MainWindow(user, plainPassword);
            mainWindow.WindowState = WindowState.Maximized;
            mainWindow.Show();
            RaiseRequestClose();
        }

        #endregion

        public RegistrationVM()
		{
			EnterCommand = new RelayCommand(ExecuteEnterCommand);
			RegisterCommand = new RelayCommand(ExecuteRegisterCommand);
			
			_userService = new UserService();
			_databaseConnectionService = new DatabaseConnectionService(
				ConfigurationManager.ConnectionStrings["FitnessClubConnectionString"].ConnectionString);
			
			_users = LoadUsers();
		}

		public List<User> LoadUsers()
		{
			try
			{
				try
				{
					if (_databaseConnectionService.IsDatabaseExists())
					{
						
						var users = _userService.GetAll();
						if (users.Count > 0)
						{
							foreach (var user in users)
							{
								if (!Enum.IsDefined(typeof(UserRole), user.Role))
								{
									user.Role = UserRole.Client;
								}
							}
							return users;
						}
					
					}
				
				}
				catch (Exception dbEx)
				{
				}
				

				var defaultUsers = CreateDefaultUsers();
				
				if (_databaseConnectionService.IsDatabaseExists())
				{
					foreach (var user in defaultUsers)
					{
						try
						{
							_userService.Add(user);
						}
						catch (Exception ex)
						{
							System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении пользователя в БД: {ex.Message}");
						}
					}
				}
				
				return defaultUsers;
			}
			catch (Exception ex)
			{
				return new List<User>();
			}
		}

		private List<User> CreateDefaultUsers()
		{
			var defaultUsers = new List<User>
			{
				new User("Admin", "admin@example.com", "admin", "admin", UserRole.Admin),
				new User("Coach", "coach@example.com", "coach", "coach", UserRole.Coach),
				new User("Client", "client@example.com", "client", "client", UserRole.Client)
			};
			
			return defaultUsers;
		}

        private void ExecuteEnterCommand(object parameter)
        {
            PasswordBox passwordBox = parameter as PasswordBox;
            if (passwordBox == null) return;

            string inputPassword = passwordBox.Password;

            if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(inputPassword))
            {
                ShowWarning("PleaseEnterLoginPass");
                return;
            }

            User foundUser = _userService.GetByLogin(Login);
            if (foundUser == null) foundUser = _users.FirstOrDefault(u => u.Login == Login);

            if (foundUser != null)
            {
                if (foundUser.IsBlocked) { ShowWarning("UserBlocked"); return; }

                // Гибридная проверка
                bool isCorrect = WPF_FitnessClub.Data.PasswordHasher.VerifyPassword(inputPassword, foundUser.Password)
                                 || foundUser.Password == inputPassword;

                if (isCorrect)
                {
                    // 1. Устанавливаем пользователя
                    UserService.SetCurrentUser(foundUser);

                    // 2. ПОКАЗЫВАЕМ СООБЩЕНИЕ (которое пропало)
                    string roleNameLocal = GetRoleNameInCurrentLanguage(foundUser.Role);
                    string msg = string.Format((string)Application.Current.Resources["WelcomeUser"], foundUser.Login, roleNameLocal);
                    MessageBox.Show(msg, (string)Application.Current.Resources["SuccessTitle"], MessageBoxButton.OK, MessageBoxImage.Information);

                    // 3. Открываем окно
                    OpenMainWindow(foundUser, inputPassword);

                    // 4. Закрываем текущее
                    Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is View.RegistrationView)?.Close();
                    return;
                }
                else
                {
                    ShowWarning("InvalidLoginCredentials");
                    return;
                }
            }

            ShowWarning("UserNotFound");
        }

        private void ExecuteRegisterCommand(object parameter)
        {
            var passwordBox = parameter as PasswordBox;
            var confirmPasswordBox = passwordBox?.Tag as PasswordBox;
            if (passwordBox == null || confirmPasswordBox == null) return;

            RegPassword = passwordBox.Password;
            ConfirmPassword = confirmPasswordBox.Password;

            // 1. Проверка на пустые поля
            if (string.IsNullOrEmpty(RegLogin) || string.IsNullOrEmpty(RegPassword) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(FullName))
            {
                ShowWarning("FillAllFields");
                return;
            }

            // 2. Проверка совпадения паролей
            if (RegPassword != ConfirmPassword) { ShowWarning("PasswordsMismatch"); return; }

            // 3. ВАЛИДАЦИЯ ПАРОЛЯ (Строго мин. 6 символов)
            if (RegPassword.Length < 6) { ShowWarning("PasswordTooShort"); return; }

            // 4. ВАЛИДАЦИЯ ФИО (Только буквы и строго 3 слова)
            if (!Regex.IsMatch(FullName, @"^[а-яА-Яa-zA-ZёЁ\s]+$")) { ShowWarning("FullNameOnlyLetters"); return; }

            string[] nameParts = FullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (RegLogin.ToLower() != "admin" && nameParts.Length != 3)
            {
                ShowWarning("FullNameRequireThreeWords");
                return;
            }

            // 5. ПРОВЕРКА УНИКАЛЬНОСТИ В БД
            bool isLoginUnique = true;
            bool isEmailUnique = true;
            if (_databaseConnectionService.IsDatabaseExists())
            {
                try
                {
                    isLoginUnique = _userService.IsLoginUnique(RegLogin);
                    isEmailUnique = _userService.IsEmailUnique(Email);
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Ошибка БД: {ex.Message}"); }
            }

            if (!isLoginUnique) { ShowWarning("LoginAlreadyExists"); return; }
            if (!isEmailUnique) { ShowWarning("EmailAlreadyExists"); return; }

            // === ШАГ ХЕШИРОВАНИЯ ===
            // Хешируем пароль перед созданием объекта пользователя
            string hashedPassword = WPF_FitnessClub.Data.PasswordHasher.HashPassword(RegPassword);

            // Создаем пользователя уже с ХЕШЕМ в поле Password
            var newUser = new User(FullName, Email, RegLogin, hashedPassword, UserRole.Client);

            if (_userService.Add(newUser) > 0)
            {
                MessageBox.Show(
                    (string)Application.Current.Resources["RegistrationSuccess"],
                    (string)Application.Current.Resources["SuccessTitle"],
                    MessageBoxButton.OK, MessageBoxImage.Information);

                OpenMainWindow(newUser, RegPassword);

                // ЗАКРЫВАЕМ ОКНО РЕГИСТРАЦИИ ПОСЛЕ УСПЕХА
                var regWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is View.RegistrationView);
                regWindow?.Close();
            }
        }
        private bool IsValid(string input, string pattern, string errorKey)
		{
			if (!Regex.IsMatch(input, pattern))
			{
				ShowWarning(errorKey);
				return false;
			}
			return true;
		}

		private void ShowWarning(string messageKey, string titleKey = "ValidationErrorTitle")
		{
			MessageBox.Show(
				(string)Application.Current.Resources[messageKey], 
				(string)Application.Current.Resources[titleKey],
				MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		private string GetRoleNameInCurrentLanguage(UserRole role)
		{
			switch (role)
			{
				case UserRole.Admin:
					return (string)Application.Current.Resources["AdminRole"] ?? "Администратор";
				case UserRole.Coach:
					return (string)Application.Current.Resources["CoachRole"] ?? "Тренер";
				case UserRole.Client:
					return (string)Application.Current.Resources["ClientRole"] ?? "Клиент";
				default:
					return role.ToString();
			}
		}
	}
}
