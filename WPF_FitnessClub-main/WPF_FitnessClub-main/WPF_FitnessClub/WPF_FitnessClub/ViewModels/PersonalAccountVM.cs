using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.IO;
using WPF_FitnessClub.Models;
using Newtonsoft.Json;
using static WPF_FitnessClub.Commands;
using WPF_FitnessClub.Data;
using WPF_FitnessClub.Data.Services;
using WPF_FitnessClub.Data.Services.Interfaces;
using System.Collections.ObjectModel;
using WPF_FitnessClub.Repositories;


namespace WPF_FitnessClub.ViewModels
{
	public class PersonalAccountVM : ViewModelBase
	{
		#region Конструктор и инициализация
		public PersonalAccountVM(User user, IWorkoutPlanService workoutPlanService, INutritionPlanService nutritionPlanService)
		{
			_user = user ?? throw new ArgumentNullException(nameof(user));
			_workoutPlanService = workoutPlanService ?? throw new ArgumentNullException(nameof(workoutPlanService));
			_nutritionPlanService = nutritionPlanService ?? throw new ArgumentNullException(nameof(nutritionPlanService));
			
			EditCommand = new RelayCommand(ExecuteEdit);
			SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
			CancelCommand = new RelayCommand(ExecuteCancel);
			CloseCommand = new RelayCommand(ExecuteClose);
			ChangePasswordCommand = new RelayCommand(ExecuteChangePassword, CanExecuteChangePassword);
			ApplyLanguageCommand = new RelayCommand(ExecuteApplyLanguage);
			ApplyThemeCommand = new RelayCommand(ExecuteApplyTheme);
			RefreshWorkoutPlansCommand = new RelayCommand(ExecuteRefreshWorkoutPlans);
			RefreshNutritionPlansCommand = new RelayCommand(ExecuteRefreshNutritionPlans);
			RefreshSubscriptionsCommand = new RelayCommand(ExecuteRefreshSubscriptions);
			CancelSubscriptionCommand = new RelayCommand(ExecuteCancelSubscription);
			MarkWorkoutPlanCompletedCommand = new RelayCommand(ExecuteMarkWorkoutPlanCompleted);
			MarkNutritionPlanCompletedCommand = new RelayCommand(ExecuteMarkNutritionPlanCompleted);

			_originalUsername = user.Login;
			_originalEmail = user.Email;
			_originalPassword = user.Password;
			SelectedTabIndex = 0;

			_userService = new UserService();
			_userSubscriptionRepository = new UserSubscriptionRepository();
			
			WorkoutPlans = new ObservableCollection<WorkoutPlan>();
			NutritionPlans = new ObservableCollection<NutritionPlan>();
			UserSubscriptions = new ObservableCollection<UserSubscriptionViewModel>();
			
			IsEditMode = false;

			CurrentTheme = ThemeManager.Instance.CurrentTheme == ThemeManager.AppTheme.Light 
				? 0 : 1;

			ThemeManager.Instance.ThemeChanged += OnThemeChanged;
			
			if (user.Role == UserRole.Client)
			{
				LoadWorkoutPlans();
				LoadNutritionPlans();
				LoadUserSubscriptions();
			}
		}
		#endregion

		#region Приватные поля
		private bool _isEditMode;
		private readonly User _user;
		private string _originalUsername;
		private string _originalEmail;
		private string _originalPassword;
		private int _selectedTabIndex;
		private UserService _userService;
		private UserSubscriptionRepository _userSubscriptionRepository;
		private string _currentPassword;
		private string _newPassword;
		private string _confirmPassword;
		private int _currentTheme;
		private int _currentLanguage;
		private bool _disposed = false;
		private IWorkoutPlanService _workoutPlanService;
		private INutritionPlanService _nutritionPlanService;
		private ObservableCollection<WorkoutPlan> _workoutPlans;
		private ObservableCollection<NutritionPlan> _nutritionPlans;
		private WorkoutPlan _selectedWorkoutPlan;
		private NutritionPlan _selectedNutritionPlan;
		private ObservableCollection<UserSubscriptionViewModel> _userSubscriptions;
		private UserSubscriptionViewModel _selectedUserSubscription;
		private bool _isLoading;
		private bool _hasSubscriptions;

		private bool _isUsernameError;
		private bool _isEmailError;
		private bool _isFullNameError;
		private bool _isPasswordError;
		private bool _isConfirmPasswordError;
		private string _usernameErrorMessage;
		private string _emailErrorMessage;
		private string _fullNameErrorMessage;
		private string _passwordErrorMessage;
		private string _confirmPasswordErrorMessage;
		#endregion

		#region События
		public event EventHandler<string> LanguageChanged;
		public event EventHandler RequestClose;
		#endregion

		#region Свойства для привязки Visibility
		public Visibility ViewModeVisible => IsEditMode ? Visibility.Collapsed : Visibility.Visible;
		public Visibility EditModeVisible => IsEditMode ? Visibility.Visible : Visibility.Collapsed;
		
		public bool IsClientRole => _user.Role == UserRole.Client;
		#endregion

		#region Команды
		public ICommand EditCommand { get; private set; }
		public ICommand SaveCommand { get; private set; }
		public ICommand CancelCommand { get; private set; }
		public ICommand CloseCommand { get; private set; }
		public ICommand ChangePasswordCommand { get; private set; }
		public ICommand ApplyLanguageCommand { get; private set; }
		public ICommand ApplyThemeCommand { get; private set; }
		public ICommand RefreshWorkoutPlansCommand { get; private set; }
		public ICommand RefreshNutritionPlansCommand { get; private set; }
		public ICommand RefreshSubscriptionsCommand { get; private set; }
		public ICommand CancelSubscriptionCommand { get; private set; }
		public ICommand MarkWorkoutPlanCompletedCommand { get; private set; }
		public ICommand MarkNutritionPlanCompletedCommand { get; private set; }
		#endregion

		#region Свойства для привязки данных
		public ObservableCollection<WorkoutPlan> WorkoutPlans
		{
			get => _workoutPlans;
			set
			{
				_workoutPlans = value;
				OnPropertyChanged(nameof(WorkoutPlans));
			}
		}
		
		public ObservableCollection<NutritionPlan> NutritionPlans
		{
			get => _nutritionPlans;
			set
			{
				_nutritionPlans = value;
				OnPropertyChanged(nameof(NutritionPlans));
			}
		}
		
		public WorkoutPlan SelectedWorkoutPlan
		{
			get => _selectedWorkoutPlan;
			set
			{
				_selectedWorkoutPlan = value;
				OnPropertyChanged(nameof(SelectedWorkoutPlan));
			}
		}
		
		public NutritionPlan SelectedNutritionPlan
		{
			get => _selectedNutritionPlan;
			set
			{
				_selectedNutritionPlan = value;
				OnPropertyChanged(nameof(SelectedNutritionPlan));
			}
		}
		
		public bool IsLoading
		{
			get => _isLoading;
			set
			{
				_isLoading = value;
				OnPropertyChanged(nameof(IsLoading));
			}
		}
		
		public bool IsEditMode
		{
			get => _isEditMode;
			set
			{
				_isEditMode = value;
				OnPropertyChanged(nameof(IsEditMode));
				OnPropertyChanged(nameof(ViewModeVisible));
				OnPropertyChanged(nameof(EditModeVisible));
			}
		}

		public string Username
		{
			get => _user.Login;
			set
			{
				_user.Login = value;
				OnPropertyChanged(nameof(Username));
				if (IsUsernameError)
				{
					IsUsernameError = false;
					UsernameErrorMessage = string.Empty;
				}
			}
		}

		public string Email
		{
			get => _user.Email;
			set
			{
				_user.Email = value;
				OnPropertyChanged(nameof(Email));
				if (IsEmailError)
				{
					IsEmailError = false;
					EmailErrorMessage = string.Empty;
				}
			}
		}

		public string FullName
		{
			get => _user.FullName;
			set
			{
				_user.FullName = value;
				OnPropertyChanged(nameof(FullName));
				if (IsFullNameError)
				{
					IsFullNameError = false;
					FullNameErrorMessage = string.Empty;
				}
			}
		}

		public string Password
		{
			get => _user.Password;
			set
			{
				_user.Password = value;
				OnPropertyChanged(nameof(Password));
			}
		}

		public string CurrentPassword
		{
			get => _currentPassword;
			set
			{
				_currentPassword = value;
				OnPropertyChanged(nameof(CurrentPassword));
			}
		}

		public string NewPassword
		{
			get => _newPassword;
			set
			{
				_newPassword = value;
				OnPropertyChanged(nameof(NewPassword));
				
				if (IsPasswordError)
				{
					IsPasswordError = false;
					PasswordErrorMessage = string.Empty;
				}
			}
		}

		public string ConfirmPassword
		{
			get => _confirmPassword;
			set
			{
				_confirmPassword = value;
				OnPropertyChanged(nameof(ConfirmPassword));
				
				if (IsConfirmPasswordError)
				{
					IsConfirmPasswordError = false;
					ConfirmPasswordErrorMessage = string.Empty;
				}
			}
		}

		public int SelectedTabIndex
		{
			get => _selectedTabIndex;
			set
			{
				_selectedTabIndex = value;
				OnPropertyChanged(nameof(SelectedTabIndex));
			}
		}

		public int CurrentTheme
		{
			get => _currentTheme;
			set
			{
				_currentTheme = value;
				OnPropertyChanged(nameof(CurrentTheme));
			}
		}

		public int CurrentLanguage
		{
			get => _currentLanguage;
			set
			{
				_currentLanguage = value;
				OnPropertyChanged(nameof(CurrentLanguage));
			}
		}

		public User User => _user;

		public ObservableCollection<UserSubscriptionViewModel> UserSubscriptions
		{
			get => _userSubscriptions;
			set
			{
				_userSubscriptions = value;
				OnPropertyChanged(nameof(UserSubscriptions));
				HasSubscriptions = value != null && value.Count > 0;
			}
		}

		public UserSubscriptionViewModel SelectedUserSubscription
		{
			get => _selectedUserSubscription;
			set
			{
				_selectedUserSubscription = value;
				OnPropertyChanged(nameof(SelectedUserSubscription));
			}
		}

		public bool HasSubscriptions
		{
			get => _hasSubscriptions;
			set
			{
				_hasSubscriptions = value;
				OnPropertyChanged(nameof(HasSubscriptions));
				OnPropertyChanged(nameof(NoSubscriptionsVisibility));
			}
		}

		public Visibility NoSubscriptionsVisibility => HasSubscriptions ? Visibility.Collapsed : Visibility.Visible;

		public bool IsUsernameError
		{
			get => _isUsernameError;
			set
			{
				_isUsernameError = value;
				OnPropertyChanged(nameof(IsUsernameError));
			}
		}

		public bool IsEmailError
		{
			get => _isEmailError;
			set
			{
				_isEmailError = value;
				OnPropertyChanged(nameof(IsEmailError));
			}
		}

		public bool IsFullNameError
		{
			get => _isFullNameError;
			set
			{
				_isFullNameError = value;
				OnPropertyChanged(nameof(IsFullNameError));
			}
		}

		public bool IsPasswordError
		{
			get => _isPasswordError;
			set
			{
				_isPasswordError = value;
				OnPropertyChanged(nameof(IsPasswordError));
			}
		}

		public bool IsConfirmPasswordError
		{
			get => _isConfirmPasswordError;
			set
			{
				_isConfirmPasswordError = value;
				OnPropertyChanged(nameof(IsConfirmPasswordError));
			}
		}

		public string UsernameErrorMessage
		{
			get => _usernameErrorMessage;
			set
			{
				_usernameErrorMessage = value;
				OnPropertyChanged(nameof(UsernameErrorMessage));
			}
		}

		public string EmailErrorMessage
		{
			get => _emailErrorMessage;
			set
			{
				_emailErrorMessage = value;
				OnPropertyChanged(nameof(EmailErrorMessage));
			}
		}

		public string FullNameErrorMessage
		{
			get => _fullNameErrorMessage;
			set
			{
				_fullNameErrorMessage = value;
				OnPropertyChanged(nameof(FullNameErrorMessage));
			}
		}

		public string PasswordErrorMessage
		{
			get => _passwordErrorMessage;
			set
			{
				_passwordErrorMessage = value;
				OnPropertyChanged(nameof(PasswordErrorMessage));
			}
		}

		public string ConfirmPasswordErrorMessage
		{
			get => _confirmPasswordErrorMessage;
			set
			{
				_confirmPasswordErrorMessage = value;
				OnPropertyChanged(nameof(ConfirmPasswordErrorMessage));
			}
		}
		#endregion

		#region Методы загрузки данных
		public void LoadWorkoutPlans()
		{
			try
			{
				IsLoading = true;
				
				if (_user.Role == UserRole.Client)
				{
					var plans = _workoutPlanService.GetWorkoutPlansByClientId(_user.Id);
					WorkoutPlans.Clear();
					
					foreach (var plan in plans)
					{
						WorkoutPlans.Add(plan);
					}
				}
				else if (_user.Role == UserRole.Coach)
				{
					var plans = _workoutPlanService.GetWorkoutPlansByCoachId(_user.Id);
					WorkoutPlans.Clear();
					
					foreach (var plan in plans)
					{
						WorkoutPlans.Add(plan);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при загрузке планов тренировок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				IsLoading = false;
			}
		}

		public void LoadNutritionPlans()
		{
			try
			{
				IsLoading = true;
				
				if (_user.Role == UserRole.Client)
				{
					var plans = _nutritionPlanService.GetNutritionPlansByClientId(_user.Id);
					NutritionPlans.Clear();
					
					foreach (var plan in plans)
					{
						NutritionPlans.Add(plan);
					}
				}
				else if (_user.Role == UserRole.Coach)
				{
					var plans = _nutritionPlanService.GetNutritionPlansByCoachId(_user.Id);
					NutritionPlans.Clear();
					
					foreach (var plan in plans)
					{
						NutritionPlans.Add(plan);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при загрузке планов питания: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				IsLoading = false;
			}
		}

		public void LoadUserSubscriptions()
		{
			try
			{
				IsLoading = true;
				
				if (_user.Role == UserRole.Client)
				{
					var subscriptions = _userSubscriptionRepository.GetUserSubscriptionsByUserId(_user.Id);
					UserSubscriptions.Clear();
					
					foreach (var subscription in subscriptions)
					{
						UserSubscriptions.Add(new UserSubscriptionViewModel(subscription));
					}
					
					HasSubscriptions = UserSubscriptions.Count > 0;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при загрузке абонементов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				IsLoading = false;
			}
		}
		#endregion

		#region Обработчики команд
		private void ExecuteRefreshWorkoutPlans(object parameter)
		{
			LoadWorkoutPlans();
		}
		
		private void ExecuteRefreshNutritionPlans(object parameter)
		{
			LoadNutritionPlans();
		}
		
		private void ExecuteRefreshSubscriptions(object parameter)
		{
			LoadUserSubscriptions();
		}
		
		private void ExecuteCancelSubscription(object parameter)
		{
			if (parameter is UserSubscriptionViewModel subscription)
			{
				if (!subscription.IsExpired)
				{
					var result = MessageBox.Show(
						(string)Application.Current.Resources["ConfirmCancelSubscription"],
						(string)Application.Current.Resources["WarningTitle"],
						MessageBoxButton.YesNo,
						MessageBoxImage.Question);
					
					if (result == MessageBoxResult.Yes)
					{
						try
						{
							bool canceled = _userSubscriptionRepository.CancelUserSubscription(subscription.Id);
							
							if (canceled)
							{
								LoadUserSubscriptions();
								
								MessageBox.Show(
									(string)Application.Current.Resources["SubscriptionCancelSuccess"],
									(string)Application.Current.Resources["SuccessTitle"],
									MessageBoxButton.OK,
									MessageBoxImage.Information);
							}
							else
							{
								MessageBox.Show(
									(string)Application.Current.Resources["SubscriptionCancelError"],
									(string)Application.Current.Resources["ErrorTitle"],
									MessageBoxButton.OK,
									MessageBoxImage.Error);
							}
						}
						catch (Exception ex)
						{
							MessageBox.Show(
								$"{(string)Application.Current.Resources["SubscriptionCancelError"]}: {ex.Message}",
								(string)Application.Current.Resources["ErrorTitle"],
								MessageBoxButton.OK,
								MessageBoxImage.Error);
						}
					}
				}
				else
				{
					MessageBox.Show(
						(string)Application.Current.Resources["SubscriptionAlreadyExpired"],
						(string)Application.Current.Resources["InfoTitle"],
						MessageBoxButton.OK,
						MessageBoxImage.Information);
				}
			}
		}

		private void ExecuteEdit(object parameter)
		{
			IsEditMode = true;
		}

		private bool CanExecuteSave(object parameter)
		{
			return true;
		}

		private void ExecuteSave(object parameter)
		{
			try
			{
				List<string> validationErrors = new List<string>();
				
				if (string.IsNullOrWhiteSpace(Username))
				{
					validationErrors.Add((string)Application.Current.Resources["UsernameRequired"]);
					IsUsernameError = true;
					UsernameErrorMessage = (string)Application.Current.Resources["UsernameRequired"];
				}
				else if (Username.Length < 3)
				{
					validationErrors.Add((string)Application.Current.Resources["UsernameTooShort"]);
					IsUsernameError = true;
					UsernameErrorMessage = (string)Application.Current.Resources["UsernameTooShort"];
				}
				else if (!System.Text.RegularExpressions.Regex.IsMatch(Username, @"^[a-zA-Z0-9_]{3,20}$"))
				{
					validationErrors.Add((string)Application.Current.Resources["UsernameInvalidFormat"]);
					IsUsernameError = true;
					UsernameErrorMessage = (string)Application.Current.Resources["UsernameInvalidFormat"];
				}
				else
				{
					IsUsernameError = false;
					UsernameErrorMessage = string.Empty;
				}
				
				if (string.IsNullOrWhiteSpace(Email))
				{
					validationErrors.Add((string)Application.Current.Resources["EmailRequired"]);
					IsEmailError = true;
					EmailErrorMessage = (string)Application.Current.Resources["EmailRequired"];
				}
				else if (!ValidateEmail(Email))
				{
					validationErrors.Add(EmailErrorMessage);
				}
				
				if (string.IsNullOrWhiteSpace(FullName))
				{
					validationErrors.Add((string)Application.Current.Resources["FullNameRequired"]);
					IsFullNameError = true;
					FullNameErrorMessage = (string)Application.Current.Resources["FullNameRequired"];
				}
				else if (FullName.Length < 3)
				{
					validationErrors.Add((string)Application.Current.Resources["FullNameTooShort"]);
					IsFullNameError = true;
					FullNameErrorMessage = (string)Application.Current.Resources["FullNameTooShort"];
				}
				else if (!System.Text.RegularExpressions.Regex.IsMatch(FullName, @"^[а-яА-Яa-zA-ZёЁ\s]+$"))
				{
					validationErrors.Add((string)Application.Current.Resources["FullNameOnlyLetters"]);
					IsFullNameError = true;
					FullNameErrorMessage = (string)Application.Current.Resources["FullNameOnlyLetters"];
				}
				else
				{
					string[] nameParts = FullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					if (nameParts.Length != 3)
					{
						validationErrors.Add((string)Application.Current.Resources["FullNameRequireThreeWords"]);
						IsFullNameError = true;
						FullNameErrorMessage = (string)Application.Current.Resources["FullNameRequireThreeWords"];
					}
					else
					{
						IsFullNameError = false;
						FullNameErrorMessage = string.Empty;
					}
				}
				
				if (!IsUsernameError && Username != _originalUsername)
				{
					var existingUser = _userService.GetByLogin(Username);
					if (existingUser != null && existingUser.Id != _user.Id)
					{
						validationErrors.Add((string)Application.Current.Resources["LoginAlreadyTaken"]);
						IsUsernameError = true;
						UsernameErrorMessage = (string)Application.Current.Resources["LoginAlreadyTaken"];
					}
				}

				if (!IsEmailError && Email != _originalEmail)
				{
					var existingUser = _userService.GetByEmail(Email);
					if (existingUser != null && existingUser.Id != _user.Id)
					{
						validationErrors.Add((string)Application.Current.Resources["EmailAlreadyExists"]);
						IsEmailError = true;
						EmailErrorMessage = (string)Application.Current.Resources["EmailAlreadyExists"];
					}
				}
				
				if (validationErrors.Count > 0)
				{
					StringBuilder errorMessageBuilder = new StringBuilder();
					errorMessageBuilder.AppendLine((string)Application.Current.Resources["ValidationErrorsHeader"]);
					
					foreach (var error in validationErrors)
					{
						errorMessageBuilder.AppendLine("- " + error);
					}
					
					string message = errorMessageBuilder.ToString();
					
					MessageBox.Show(
						message,
						(string)Application.Current.Resources["ValidationErrorTitle"],
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;      
				}

				_userService.Update(_user);

				_originalUsername = Username;
				_originalEmail = Email;

				IsEditMode = false;

				MessageBox.Show(
					(string)Application.Current.Resources["SaveSuccessful"],
					(string)Application.Current.Resources["SuccessTitle"],
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					string.Format((string)Application.Current.Resources["ErrorSavingData"], ex.Message),
					(string)Application.Current.Resources["ErrorTitle"],
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		private bool ValidateUsername(string username)
		{
			if (string.IsNullOrWhiteSpace(username))
			{
				IsUsernameError = true;
				UsernameErrorMessage = (string)Application.Current.Resources["UsernameRequired"];
				return false;
			}
			
			if (username.Length < 3)
			{
				IsUsernameError = true;
				UsernameErrorMessage = (string)Application.Current.Resources["UsernameTooShort"];
				return false;
			}
			
			if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,20}$"))
			{
				IsUsernameError = true;
				UsernameErrorMessage = (string)Application.Current.Resources["UsernameInvalidFormat"];
				return false;
			}
			
			IsUsernameError = false;
			UsernameErrorMessage = string.Empty;
			return true;
		}

		private bool ValidateFullName(string fullName)
		{
			if (string.IsNullOrWhiteSpace(fullName))
			{
				IsFullNameError = true;
				FullNameErrorMessage = (string)Application.Current.Resources["FullNameRequired"];
				return false;
			}
			
			if (fullName.Length < 3)
			{
				IsFullNameError = true;
				FullNameErrorMessage = (string)Application.Current.Resources["FullNameTooShort"];
				return false;
			}
			
			if (!System.Text.RegularExpressions.Regex.IsMatch(fullName, @"^[а-яА-Яa-zA-ZёЁ\s]+$"))
			{
				IsFullNameError = true;
				FullNameErrorMessage = (string)Application.Current.Resources["FullNameOnlyLetters"];
				return false;
			}
			
			string[] nameParts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (nameParts.Length != 3)
			{
				IsFullNameError = true;
				FullNameErrorMessage = (string)Application.Current.Resources["FullNameRequireThreeWords"];
				return false;
			}
			
			IsFullNameError = false;
			FullNameErrorMessage = string.Empty;
			return true;
		}

		private void ExecuteCancel(object parameter)
		{
			Username = _originalUsername;
			Email = _originalEmail;
			IsEditMode = false;
		}

		private void ExecuteClose(object parameter)
		{
			RequestClose?.Invoke(this, EventArgs.Empty);
		}

		private bool CanExecuteChangePassword(object parameter)
		{
			return true;
		}

		private void ExecuteChangePassword(object parameter)
		{
			try
			{
				List<string> validationErrors = new List<string>();
				
				if (string.IsNullOrWhiteSpace(CurrentPassword))
				{
					validationErrors.Add((string)Application.Current.Resources["CurrentPasswordRequired"]);
					IsPasswordError = true;
					PasswordErrorMessage = (string)Application.Current.Resources["CurrentPasswordRequired"];
				}
				else if (CurrentPassword != _originalPassword)
				{
					validationErrors.Add((string)Application.Current.Resources["CurrentPasswordIncorrect"]);
					IsPasswordError = true;
					PasswordErrorMessage = (string)Application.Current.Resources["CurrentPasswordIncorrect"];
				}
				else 
				{
					IsPasswordError = false;
					PasswordErrorMessage = string.Empty;
				}
				
				if (string.IsNullOrWhiteSpace(NewPassword))
				{
					validationErrors.Add((string)Application.Current.Resources["NewPasswordRequired"]);
					IsPasswordError = true;
					PasswordErrorMessage = (string)Application.Current.Resources["NewPasswordRequired"];
				}
				else if (NewPassword.Length < 8)
				{
					validationErrors.Add((string)Application.Current.Resources["PasswordTooShort"]);
					IsPasswordError = true;
					PasswordErrorMessage = (string)Application.Current.Resources["PasswordTooShort"];
				}
				else if (!System.Text.RegularExpressions.Regex.IsMatch(NewPassword, @"[A-Za-z]"))
				{
					validationErrors.Add((string)Application.Current.Resources["PasswordRequireLetter"]);
					IsPasswordError = true;
					PasswordErrorMessage = (string)Application.Current.Resources["PasswordRequireLetter"];
				}
				else if (!System.Text.RegularExpressions.Regex.IsMatch(NewPassword, @"\d"))
				{
					validationErrors.Add((string)Application.Current.Resources["PasswordRequireDigit"]);
					IsPasswordError = true;
					PasswordErrorMessage = (string)Application.Current.Resources["PasswordRequireDigit"];
				}
				
				if (string.IsNullOrWhiteSpace(ConfirmPassword))
				{
					validationErrors.Add((string)Application.Current.Resources["ConfirmPasswordRequired"]);
					IsConfirmPasswordError = true;
					ConfirmPasswordErrorMessage = (string)Application.Current.Resources["ConfirmPasswordRequired"];
				}
				else if (NewPassword != ConfirmPassword)
				{
					validationErrors.Add((string)Application.Current.Resources["PasswordsDoNotMatch"]);
					IsConfirmPasswordError = true;
					ConfirmPasswordErrorMessage = (string)Application.Current.Resources["PasswordsDoNotMatch"];
				}
				else
				{
					IsConfirmPasswordError = false;
					ConfirmPasswordErrorMessage = string.Empty;
				}
				
				if (validationErrors.Count > 0)
				{
					StringBuilder errorMessageBuilder = new StringBuilder();
					errorMessageBuilder.AppendLine((string)Application.Current.Resources["ValidationErrorsHeader"]);
					
					foreach (var error in validationErrors)
					{
						errorMessageBuilder.AppendLine("- " + error);
					}
					
					string message = errorMessageBuilder.ToString();
					
					MessageBox.Show(
						message,
						(string)Application.Current.Resources["ValidationErrorTitle"],
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;      
				}

				_user.Password = NewPassword;
				_userService.Update(_user);

				_originalPassword = NewPassword;

				ClearPasswordFields();

				MessageBox.Show(
					(string)Application.Current.Resources["PasswordChangedSuccess"],
					(string)Application.Current.Resources["SuccessTitle"],
					MessageBoxButton.OK,
					MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					string.Format((string)Application.Current.Resources["PasswordChangeFailed"], ex.Message),
					(string)Application.Current.Resources["ErrorTitle"],
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		private void ExecuteApplyLanguage(object parameter)
		{
			string lang = CurrentLanguage == 0 ? "ru-RU" : "en-US";
			LanguageChanged?.Invoke(this, lang);
		}

		private void ExecuteApplyTheme(object parameter)
		{
			ThemeManager.Instance.SetTheme(CurrentTheme == 0 
				? ThemeManager.AppTheme.Light 
				: ThemeManager.AppTheme.Dark);
		}

		public bool ValidateEmail(string email)
		{
			if (string.IsNullOrWhiteSpace(email))
			{
				IsEmailError = true;
				EmailErrorMessage = (string)Application.Current.Resources["EmailRequired"];
				return false;
			}

			try 
			{
				if (!email.Contains("@"))
				{
					IsEmailError = true;
					EmailErrorMessage = (string)Application.Current.Resources["EmailMustContainAtSymbol"];
					return false;
				}

				string[] parts = email.Split('@');
				if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
				{
					IsEmailError = true;
					EmailErrorMessage = (string)Application.Current.Resources["InvalidEmailFormat"];
					return false;
				}

				string localPart = parts[0];
				string domainPart = parts[1];
				
				if (!System.Text.RegularExpressions.Regex.IsMatch(localPart, @"^[a-zA-Z0-9._\-]+$"))
				{
					IsEmailError = true;
					EmailErrorMessage = (string)Application.Current.Resources["InvalidLocalPartFormat"];
					return false;
				}
				
				if (localPart.Contains(".."))
				{
					IsEmailError = true;
					EmailErrorMessage = (string)Application.Current.Resources["LocalPartDoubleDot"];
					return false;
				}
				
				if (!domainPart.Contains("."))
				{
					IsEmailError = true;
					EmailErrorMessage = (string)Application.Current.Resources["DomainMustContainDot"];
					return false;
				}
				
				if (domainPart.Contains(".."))
				{
					IsEmailError = true;
					EmailErrorMessage = (string)Application.Current.Resources["DomainDoubleDot"];
					return false;
				}
				
				if (domainPart.StartsWith(".") || domainPart.EndsWith("."))
				{
					IsEmailError = true;
					EmailErrorMessage = (string)Application.Current.Resources["DomainStartEndDot"];
					return false;
				}
				
				if (!System.Text.RegularExpressions.Regex.IsMatch(domainPart, @"^[a-zA-Z0-9\.\-]+$"))
				{
					IsEmailError = true;
					EmailErrorMessage = (string)Application.Current.Resources["InvalidDomainFormat"];
					return false;
				}

				IsEmailError = false;
				EmailErrorMessage = string.Empty;
				return true;
			}
			catch 
			{
				IsEmailError = true;
				EmailErrorMessage = (string)Application.Current.Resources["InvalidEmailFormat"];
				return false;
			}
		}

		private void OnThemeChanged(object sender, ThemeManager.AppTheme theme)
		{
			CurrentTheme = theme == ThemeManager.AppTheme.Light ? 0 : 1;
			OnPropertyChanged(nameof(CurrentTheme));
		}

		private void ClearPasswordFields()
		{
			CurrentPassword = string.Empty;
			NewPassword = string.Empty;
			ConfirmPassword = string.Empty;
			
			IsPasswordError = false;
			IsConfirmPasswordError = false;
			PasswordErrorMessage = string.Empty;
			ConfirmPasswordErrorMessage = string.Empty;
		}

		private void ExecuteMarkWorkoutPlanCompleted(object parameter)
		{
			if (parameter is WorkoutPlan workoutPlan)
			{
				try
				{
					System.Diagnostics.Debug.WriteLine($"Начинаем обновление статуса плана тренировок ID={workoutPlan.Id}, текущий статус IsCompleted={workoutPlan.IsCompleted}");
					
					var freshPlan = _workoutPlanService.GetById(workoutPlan.Id);
					if (freshPlan == null)
					{
						MessageBox.Show("План тренировок не найден в базе данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
					}
					
					bool newStatus = workoutPlan.IsCompleted;
					freshPlan.IsCompleted = newStatus;
					freshPlan.UpdatedDate = DateTime.Now;
					
					System.Diagnostics.Debug.WriteLine($"Обновляем план тренировок ID={freshPlan.Id}, новый статус IsCompleted={newStatus}");
					
					var updatedPlan = _workoutPlanService.Update(freshPlan);
					
					if (updatedPlan.IsCompleted != newStatus)
					{
						System.Diagnostics.Debug.WriteLine($"ВНИМАНИЕ: Значение после обновления ({updatedPlan.IsCompleted}) не соответствует заданному ({newStatus})");
						workoutPlan.IsCompleted = updatedPlan.IsCompleted;
					}
					
					System.Diagnostics.Debug.WriteLine($"План тренировок ID={workoutPlan.Id} обновлен, UI статус IsCompleted={workoutPlan.IsCompleted}");
					
					OnPropertyChanged(nameof(WorkoutPlans));
					
					string status = workoutPlan.IsCompleted ? "выполнен" : "не выполнен";
					MessageBox.Show($"Статус плана успешно изменен на '{status}'", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении статуса плана тренировок: {ex.Message}");
					MessageBox.Show($"Ошибка при обновлении статуса плана тренировок: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void ExecuteMarkNutritionPlanCompleted(object parameter)
		{
			if (parameter is NutritionPlan nutritionPlan)
			{
				try
				{
					System.Diagnostics.Debug.WriteLine($"Начинаем обновление статуса плана питания ID={nutritionPlan.Id}, текущий статус IsCompleted={nutritionPlan.IsCompleted}");
					
					var freshPlan = _nutritionPlanService.GetById(nutritionPlan.Id);
					if (freshPlan == null)
					{
						MessageBox.Show("План питания не найден в базе данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
						return;
					}
					
					bool newStatus = nutritionPlan.IsCompleted;
					freshPlan.IsCompleted = newStatus;
					freshPlan.UpdatedDate = DateTime.Now;
					
					System.Diagnostics.Debug.WriteLine($"Обновляем план питания ID={freshPlan.Id}, новый статус IsCompleted={newStatus}");
					
					var updatedPlan = _nutritionPlanService.Update(freshPlan);
					
					if (updatedPlan.IsCompleted != newStatus)
					{
						System.Diagnostics.Debug.WriteLine($"ВНИМАНИЕ: Значение после обновления ({updatedPlan.IsCompleted}) не соответствует заданному ({newStatus})");
						nutritionPlan.IsCompleted = updatedPlan.IsCompleted;
					}
					
					System.Diagnostics.Debug.WriteLine($"План питания ID={nutritionPlan.Id} обновлен, UI статус IsCompleted={nutritionPlan.IsCompleted}");
					
					OnPropertyChanged(nameof(NutritionPlans));
					
					string status = nutritionPlan.IsCompleted ? "выполнен" : "не выполнен";
					MessageBox.Show($"Статус плана успешно изменен на '{status}'", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении статуса плана питания: {ex.Message}");
					MessageBox.Show($"Ошибка при обновлении статуса плана питания: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
		
		#endregion
	}
}
