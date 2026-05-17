using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Xml.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Remoting.Channels;
using WPF_FitnessClub;
using System.ComponentModel;
using WPF_FitnessClub.Models;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using WPF_FitnessClub.View;
using WPF_FitnessClub.Data;
using WPF_FitnessClub.Data.Services;
using WPF_FitnessClub.Data.Services.Interfaces;
using WPF_FitnessClub.ViewModels;

namespace WPF_FitnessClub
{
	public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
	{
		public User _user;
		public readonly UserRole currentUserRole;
		public List<Subscription> subscriptions = new List<Subscription>();
		private SubscriptionService _subscriptionService;
		private UserService _userService;
		private IWorkoutPlanService _workoutPlanService;
		private INutritionPlanService _nutritionPlanService;
		private bool _disposed = false;
		
		private List<SubscriptionDetailsView> _openSubscriptionDetailsViews = new List<SubscriptionDetailsView>();
		
		private Visibility editModeVisible = Visibility.Collapsed;
		private Visibility adminRoleVisible = Visibility.Collapsed;
		private Visibility coachRoleVisible = Visibility.Collapsed;
		private Visibility clientRoleVisible = Visibility.Collapsed;
		private Visibility addSubscriptionVisible = Visibility.Collapsed;

		public User CurrentUser => _user;
		
		public Visibility EditModeVisible
		{
			get => editModeVisible;
			set
			{
				editModeVisible = value;
				OnPropertyChanged(nameof(EditModeVisible));
			}
		}

		public Visibility AdminRoleVisible
		{
			get => adminRoleVisible;
			set
			{
				adminRoleVisible = value;
				OnPropertyChanged(nameof(AdminRoleVisible));
			}
		}

		public Visibility CoachRoleVisible
		{
			get => coachRoleVisible;
			set
			{
				coachRoleVisible = value;
				OnPropertyChanged(nameof(CoachRoleVisible));
			}
		}

		public Visibility ClientRoleVisible
		{
			get => clientRoleVisible;
			set
			{
				clientRoleVisible = value;
				OnPropertyChanged(nameof(ClientRoleVisible));
			}
		}

		public Visibility AddSubscriptionVisible
		{
			get => addSubscriptionVisible;
			set
			{
				addSubscriptionVisible = value;
				OnPropertyChanged(nameof(AddSubscriptionVisible));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private SubscriptionsView _homePageView;

		public MainWindow(User user)
		{
			_user = user;
			currentUserRole = user.Role;

			_subscriptionService = new SubscriptionService();
			_userService = new UserService();
			_workoutPlanService = new WorkoutPlanService();
			_nutritionPlanService = new NutritionPlanService();
			
			UserService.SetCurrentUser(user);

			InitializeComponent();
			DataContext = this;

			this.WindowState = WindowState.Maximized;

			NavigationManager.Instance.Initialize(MainContent);

			LoadSubscriptions();

			ShowHomePage();

			switch (currentUserRole)
			{
				case UserRole.Admin:
					EditModeVisible = Visibility.Visible;
					AdminRoleVisible = Visibility.Visible;
					CoachRoleVisible = Visibility.Collapsed;
					ClientRoleVisible = Visibility.Collapsed;
					AddSubscriptionVisible = Visibility.Visible;
					break;
				case UserRole.Coach:
					EditModeVisible = Visibility.Visible;
					AdminRoleVisible = Visibility.Collapsed;
					CoachRoleVisible = Visibility.Visible;
					ClientRoleVisible = Visibility.Collapsed;
					AddSubscriptionVisible = Visibility.Visible;
					break;
				case UserRole.Client:
					EditModeVisible = Visibility.Collapsed;
					AdminRoleVisible = Visibility.Collapsed;
					CoachRoleVisible = Visibility.Collapsed;
					ClientRoleVisible = Visibility.Visible;
					AddSubscriptionVisible = Visibility.Collapsed;
					break;
				default:
					EditModeVisible = Visibility.Collapsed;
					AdminRoleVisible = Visibility.Collapsed;
					CoachRoleVisible = Visibility.Collapsed;
					ClientRoleVisible = Visibility.Collapsed;
					AddSubscriptionVisible = Visibility.Collapsed;
					break;
			}

			ThemeManager.Instance.ThemeChanged += OnThemeChanged;
		}

		public void LoadSubscriptions()
		{
			try
			{
				System.Diagnostics.Debug.WriteLine("Начинаем загрузку абонементов в MainWindow");
				
				subscriptions = _subscriptionService.GetAll().ToList();
				
				if (subscriptions == null)
				{
					System.Diagnostics.Debug.WriteLine("Получен null при загрузке абонементов");
					subscriptions = new List<Subscription>();
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"Загружено {subscriptions.Count} абонементов в MainWindow");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке абонементов: {ex.Message}");
				MessageBox.Show(string.Format((string)Application.Current.Resources["ErrorLoadingData"], ex.Message));
				subscriptions = new List<Subscription>();
			}
		}

		public void SaveSubscriptions()
		{
			try
			{
				System.Diagnostics.Debug.WriteLine("Сохранение абонементов происходит автоматически через сервис");
			}
			catch (Exception ex)
			{
				MessageBox.Show(string.Format((string)Application.Current.Resources["ErrorSavingData"], ex.Message));
			}
		}

		private void ShowHomePage()
		{
			if (_homePageView == null)
			{
				_homePageView = new SubscriptionsView(this, subscriptions);
				_homePageView.SubscriptionSelected += OpenSubscriptionDetails;
			}
			else
			{
				_homePageView.UpdateSubscriptions(subscriptions);
			}

			NavigationManager.Instance.NavigateTo(_homePageView);
		}

		private void OpenSubscriptionDetails(Subscription subscription)
		{
			var detailsWindow = new SubscriptionDetailsView(this, subscriptions, subscription, currentUserRole);
			
			_openSubscriptionDetailsViews.Add(detailsWindow);
			
			detailsWindow.Closed += (sender, e) => 
			{
				_openSubscriptionDetailsViews.Remove(detailsWindow);
			};
			
			detailsWindow.ShowDialog();
		}

		public void UpdateUIWithSubscriptions(List<Subscription> updatedSubscriptions)
		{
			System.Diagnostics.Debug.WriteLine($"UpdateUIWithSubscriptions: Обновление UI с {updatedSubscriptions?.Count ?? 0} абонементами");
			
			subscriptions = new List<Subscription>(updatedSubscriptions ?? new List<Subscription>());
			
			if (_homePageView != null)
			{
				System.Diagnostics.Debug.WriteLine($"UpdateUIWithSubscriptions: Обновление домашней страницы");
				bool resetFilters = subscriptions.Count < _homePageView._viewModel.FilteredSubscriptions.Count;
				_homePageView.UpdateSubscriptions(subscriptions, resetFilters);
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"UpdateUIWithSubscriptions: Домашняя страница не загружена");
			}
			
			foreach (var detailsView in _openSubscriptionDetailsViews.ToList())
			{
				try
				{
					var vm = detailsView.DataContext as SubscriptionDetailsVM;
					if (vm != null)
					{
						int currentSubscriptionId = vm.CurrentSubscriptionId;
						System.Diagnostics.Debug.WriteLine($"UpdateUIWithSubscriptions: Обработка окна с абонементом ID={currentSubscriptionId}");
						
						var updatedCurrent = updatedSubscriptions.FirstOrDefault(s => s.Id == currentSubscriptionId);
						if (updatedCurrent != null)
						{
							System.Diagnostics.Debug.WriteLine($"UpdateUIWithSubscriptions: Обновление данных абонемента ID={currentSubscriptionId}");
							vm.UpdateSubscriptionDetails(updatedCurrent, updatedSubscriptions);
						}
						else
						{
							System.Diagnostics.Debug.WriteLine($"UpdateUIWithSubscriptions: Абонемент ID={currentSubscriptionId} не найден, закрываем окно");
							detailsView.Close();
						}
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении окна деталей абонемента: {ex.Message}");
				}
			}
			
			if (MainContent.Content is AdminPanelView adminView)
			{
				try
				{
					if (adminView.DataContext is ViewModelBase viewModel)
					{
						viewModel.OnPropertyChanged("");
						System.Diagnostics.Debug.WriteLine($"UpdateUIWithSubscriptions: Обновлены данные в AdminPanelView");
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении панели администратора: {ex.Message}");
				}
			}
			
			System.Diagnostics.Debug.WriteLine($"UpdateUIWithSubscriptions: Принудительное обновление UI");
			Application.Current.Dispatcher.BeginInvoke(
				System.Windows.Threading.DispatcherPriority.Background,
				new Action(() => { }));
		}

		public void UpdateSubscriptions()
		{
			LoadSubscriptions();
			
			SaveSubscriptions();

			if (_homePageView != null)
			{
				_homePageView.UpdateSubscriptions(subscriptions);
			}
			
			UpdateAllSubscriptionDetailsViews();
		}

		private void UpdateAllSubscriptionDetailsViews()
		{
			foreach (var detailsView in _openSubscriptionDetailsViews.ToList())
			{
				try
				{
					var viewModel = detailsView.DataContext as ViewModels.SubscriptionDetailsVM;
					if (viewModel != null)
					{
						int subscriptionId = viewModel.CurrentSubscriptionId;
						var updatedSubscription = subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
						
						if (updatedSubscription != null)
						{
							viewModel.UpdateSubscriptionDetails(updatedSubscription, subscriptions);
						}
						else
						{
							detailsView.Close();
						}
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении окна деталей абонемента: {ex.Message}");
				}
			}
		}

		private void PersonalAccountButon_Click(object sender, RoutedEventArgs e)
		{
			var personalAccountView = new PersonalAccountView(_user);
			NavigationManager.Instance.NavigateTo(personalAccountView);
		}

		private void HomeButton_Click(object sender, RoutedEventArgs e)
		{
			ShowHomePage();
		}

		private void AddSubscriptionButton_Click(object sender, RoutedEventArgs e)
		{
			if (currentUserRole != UserRole.Coach && currentUserRole != UserRole.Admin)
			{
				MessageBox.Show(
					(string)Application.Current.Resources["OnlyCoachesCanAddSubscription"],
					(string)Application.Current.Resources["AccessDenied"],
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}
			
			UseAddSubscriptionView();
		}

		private void OnThemeChanged(object sender, ThemeManager.AppTheme e)
		{
			if (_homePageView != null)
			{
				_homePageView.UpdateSubscriptions(subscriptions);
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
			Dispose();
		}

		private void UseAddSubscriptionView()
		{
			var dialog = new Window
			{
				Title = (string)Application.Current.Resources["AddSubscriptionTitle"],
				SizeToContent = SizeToContent.WidthAndHeight,
				WindowStartupLocation = WindowStartupLocation.CenterScreen,
				Style = (Style)Application.Current.Resources["WindowStyle"]
			};

			var subscriptionView = new View.AddSubscriptionView();

			subscriptionView.CloseRequested += (success, subscription) =>
			{
				dialog.DialogResult = success;
				dialog.Close();

				if (success && subscription != null)
				{
					subscriptions.Add(subscription);
					
					if (_homePageView != null)
					{
						_homePageView.UpdateSubscriptions(subscriptions, true);
					}
					
					UpdateAllSubscriptionDetailsViews();
				}
			};

			dialog.Content = subscriptionView;
			dialog.ShowDialog();
		}

		private void DataTableButton_Click(object sender, RoutedEventArgs e)
		{
			var adminPanelWindow = new Window
			{
				Title = "Панель администратора",
				Width = 1200,
				Height = 700,
				WindowStartupLocation = WindowStartupLocation.CenterScreen,
				Content = new View.AdminPanelView()
			};

			adminPanelWindow.Show();
		}

		private void CoachClientsButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var coachClientsView = new View.CoachClientsView(_user);
				
				coachClientsView.Show();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка при открытии окна клиентов тренера: {ex.Message}");
				MessageBox.Show(
					$"Ошибка при открытии окна клиентов: {ex.Message}",
					"Ошибка",
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		private void LogoutButton_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult result = MessageBox.Show(
				(string)Application.Current.Resources["ConfirmLogout"],
				(string)Application.Current.Resources["ConfirmTitle"],
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
				
			if (result == MessageBoxResult.Yes)
			{
				System.Diagnostics.Debug.WriteLine("Выход из системы для пользователя: " + _user.Login);
				
				try
				{
					RegistrationView registrationView = new RegistrationView();
					registrationView.Show();
					
					Dispose();
					
					_user = null;
					
					this.Close();
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Ошибка при выходе из системы: {ex.Message}");
					MessageBox.Show(
						$"{(string)Application.Current.Resources["ErrorLoggingOut"]}: {ex.Message}",
						(string)Application.Current.Resources["ErrorTitle"],
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				}
			}
		}

		public void RefreshUserSubscriptions()
		{
			var personalAccountView = NavigationManager.Instance.CurrentView as PersonalAccountView;
			if (personalAccountView != null)
			{
				var viewModel = personalAccountView.DataContext as ViewModels.PersonalAccountVM;
				if (viewModel != null)
				{
					viewModel.LoadUserSubscriptions();
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_subscriptionService.Dispose();
				}
				
				_disposed = true;
			}
		}

	}
}
