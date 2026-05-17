using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_FitnessClub.Models;
using WPF_FitnessClub.Data;
using WPF_FitnessClub.Data.Services;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using WPF_FitnessClub.Repositories;
using static WPF_FitnessClub.Commands;

namespace WPF_FitnessClub.ViewModels
{
	public class SubscriptionDetailsVM : ViewModelBase
	{
		#region Приватные поля
		private Subscription _currentSubscription;
		private ObservableCollection<Subscription> _subscriptions;
		private UserRole _currentUserRole;
		private MainWindow _mainWindow;
		private SubscriptionService _subscriptionService;
		private ReviewService _reviewService;
		private UserService _userService;
		private UserSubscriptionRepository _userSubscriptionRepository;

		private string _name;
		private string _imagePath;
		private string _price;
		private string _type;
		private string _duration;
		private string _description;
		private bool _isEditMode;
		private string _reviewComment;
		private int _reviewRating;
		private ObservableCollection<Review> _reviews;
		private bool _isLoading;
		private bool _disposed = false;
		private bool _hasUserReviewed;
		private bool _justDeletedReview = false;
		private int _lastDeletedReviewId = 0;
		private bool _canSubscribe;
		private bool _canReviewSubscription;

		private readonly Dictionary<string, string> _typeTranslations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "Unlimited", "Безлимит" },
			{ "Standard", "Обычный" },
			{ "Безлимит", "Безлимит" },
			{ "Обычный", "Обычный" },
		};

		private readonly Dictionary<string, string> _durationTranslations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "1 Month", "1 месяц" },
			{ "3 Months", "3 месяца" },
			{ "6 Months", "6 месяцев" },
			{ "12 Months", "12 месяцев" },
			{ "1 месяц", "1 месяц" },
			{ "3 месяца", "3 месяца" },
			{ "6 месяцев", "6 месяцев" },
			{ "12 месяцев", "12 месяцев" }
		};

		private Dictionary<string, string> GetReverseTypeTranslations()
		{
			var reverseDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			
			foreach (var pair in _typeTranslations.Where(p => p.Value != null))
			{
				if (!reverseDict.ContainsKey(pair.Value))
				{
					reverseDict.Add(pair.Value, GetCurrentLanguage() == "ru" ? pair.Value : pair.Key);
				}
			}
			
			return reverseDict;
		}

		private Dictionary<string, string> GetReverseDurationTranslations()
		{
			var reverseDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			
			foreach (var pair in _durationTranslations.Where(p => p.Value != null))
			{
				if (!reverseDict.ContainsKey(pair.Value))
				{
					reverseDict.Add(pair.Value, GetCurrentLanguage() == "ru" ? pair.Value : pair.Key);
				}
			}
			
			return reverseDict;
		}

		private string GetCurrentLanguage()
		{
			try
			{
				ResourceDictionary currentDict = Application.Current.Resources.MergedDictionaries
					.FirstOrDefault(d => d.Source?.OriginalString.Contains("Dictionary_") == true);
					
				if (currentDict != null)
				{
					string sourceUri = currentDict.Source.OriginalString;
					if (sourceUri.Contains("Dictionary_ru"))
						return "ru";
					else if (sourceUri.Contains("Dictionary_en"))
						return "en";
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка при определении языка: {ex.Message}");
			}
			
			return "ru";
		}

		private string GetLocalizedValue(string dbValue, Dictionary<string, string> reverseTranslations)
		{
			if (string.IsNullOrEmpty(dbValue))
				return string.Empty;
				
			if (GetCurrentLanguage() == "ru")
				return dbValue;
				
			if (reverseTranslations.TryGetValue(dbValue, out string localizedValue))
			{
				return localizedValue;
			}
			
			return dbValue;
		}

		private string GetDbValue(string uiValue, Dictionary<string, string> translations)
		{
			if (string.IsNullOrEmpty(uiValue))
				return string.Empty;
				
			if (translations.TryGetValue(uiValue, out string dbValue))
			{
				return dbValue;
			}
			
			if (GetCurrentLanguage() == "ru")
				return uiValue;
				
			string normalizedValue = uiValue.ToLower().Trim();
			foreach (var pair in translations)
			{
				string normalizedKey = pair.Key.ToLower();
				if (normalizedKey == normalizedValue || normalizedValue.Contains(normalizedKey) || normalizedKey.Contains(normalizedValue))
				{
					return pair.Value;
				}
			}
			
			return uiValue;
		}
		#endregion

		#region Свойства
		public int CurrentSubscriptionId => _currentSubscription?.Id ?? 0;

		public double Rating
		{
			get => _currentSubscription?.Rating ?? 0;
			set
			{
				if (_currentSubscription != null && Math.Abs(_currentSubscription.Rating - value) > 0.01)
				{
					_currentSubscription.Rating = value;
					OnPropertyChanged(nameof(Rating));
				}
			}
		}

		public string SubscrName
		{
			get => _name;
			set
			{
				_name = value;
				OnPropertyChanged("SubscrName");
			}
		}

		public string ImagePath
		{
			get => _imagePath;
			set
			{
				_imagePath = value;
				OnPropertyChanged("ImagePath");
			}
		}

		public string Price
		{
			get => _price;
			set
			{
				_price = value;
				OnPropertyChanged("Price");
			}
		}

		public string Description
		{
			get => _description;
			set
			{
				_description = value;
				OnPropertyChanged("Description");
			}
		}

		public string Duration
		{
			get => _duration;
			set
			{
				_duration = value;
				OnPropertyChanged("Duration");
			}
		}

		public string Type
		{
			get => _type;
			set
			{
				_type = value;
				OnPropertyChanged("Type");
			}
		}

		public bool IsEditMode
		{
			get => _isEditMode;
			set
			{
				_isEditMode = value;
				OnPropertyChanged("IsEditMode");
				OnPropertyChanged(nameof(ViewModeVisible));
				OnPropertyChanged(nameof(EditModeVisible));
				OnPropertyChanged(nameof(AdminEditVisible));
			}
		}

		public string ReviewComment
		{
			get => _reviewComment;
			set
			{
				_reviewComment = value;
				OnPropertyChanged("ReviewComment");
			}
		}

		public int ReviewRating
		{
			get => _reviewRating;
			set
			{
				_reviewRating = value;
				OnPropertyChanged("ReviewRating");
			}
		}

		public ObservableCollection<Review> Reviews
		{
			get => _reviews;
			set
			{
				_reviews = value;
				OnPropertyChanged("Reviews");
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

		public bool HasUserReviewed
		{
			get => _hasUserReviewed;
			set
			{
				if (_hasUserReviewed != value)
				{
					_hasUserReviewed = value;
					OnPropertyChanged(nameof(HasUserReviewed));
					OnPropertyChanged(nameof(WriteReviewVisible));
				}
			}
		}

		public Visibility ViewModeVisible => IsEditMode ? Visibility.Collapsed : Visibility.Visible;

		public Visibility EditModeVisible
		{
			get
			{
				if (_currentUserRole == UserRole.Coach || _currentUserRole == UserRole.Admin)
				{
					return IsEditMode ? Visibility.Visible : Visibility.Collapsed;
				}
				return Visibility.Collapsed;
			}
		}

		public Visibility AdminEditVisible
		{
			get
			{
				if ((_currentUserRole == UserRole.Coach || _currentUserRole == UserRole.Admin) && !IsEditMode)
				{
					return Visibility.Visible;
				}
				return Visibility.Collapsed;
			}
		}

		public Visibility CanSubscribeVisible => !IsEditMode && CanSubscribe ? Visibility.Visible : Visibility.Collapsed;

		public bool CanSubscribe
		{
			get => _canSubscribe;
			private set
			{
				if (_canSubscribe != value)
				{
					_canSubscribe = value;
					OnPropertyChanged(nameof(CanSubscribe));
					OnPropertyChanged(nameof(CanSubscribeVisible));
				}
			}
		}

		public Visibility DeleteReviewVisible => _currentUserRole == UserRole.Admin ? Visibility.Visible : Visibility.Collapsed;
		public Visibility WriteReviewVisible
		{
			get
			{
				if (_currentUserRole != UserRole.Client)
					return Visibility.Collapsed;
				
				if (HasUserReviewed || _mainWindow == null || _mainWindow._user == null)
					return Visibility.Collapsed;
				
				if (_canReviewSubscription)
				{
					return Visibility.Visible;
				}
				
				return Visibility.Collapsed;
			}
		}

		public Visibility SubscribeToReviewVisible
		{
			get
			{
				if (_currentUserRole == UserRole.Client && 
					_mainWindow != null && 
					_mainWindow._user != null && 
					!_hasUserReviewed && 
					!_canReviewSubscription)
				{
					return Visibility.Visible;
				}
				return Visibility.Collapsed;
			}
		}
		#endregion

		#region Команды
		public ICommand ChooseImageCommand { get; private set; }
		public ICommand EditCommand { get; private set; }
		public ICommand SaveCommand { get; private set; }
		public ICommand DeleteCommand { get; private set; }
		public ICommand CancelCommand { get; private set; }
		public ICommand CloseCommand { get; private set; }
		public ICommand DeleteReviewCommand { get; private set; }
		public ICommand AddReviewCommand { get; private set; }
		public ICommand SubscribeCommand { get; private set; }
		#endregion

		#region События
		public event EventHandler RequestClose;
		public event EventHandler<Review> ReviewAdded;
		public event EventHandler<Review> ReviewDeleted;
		public event EventHandler<Subscription> SubscriptionDeleted;
		#endregion

		#region Конструктор
		public SubscriptionDetailsVM(MainWindow mainWindow, List<Subscription> subscriptions, Subscription subscription, UserRole role)
		{
			_mainWindow = mainWindow;
			_subscriptions = new ObservableCollection<Subscription>(subscriptions);
			_currentSubscription = subscription;
			_currentUserRole = role;
			
			_subscriptionService = new SubscriptionService();
			_reviewService = new ReviewService();
			_userService = new UserService();
			_userSubscriptionRepository = new WPF_FitnessClub.Repositories.UserSubscriptionRepository();
			
			_reviews = new ObservableCollection<Review>();
			_isEditMode = false;
			_hasUserReviewed = false;
			_canReviewSubscription = false;
			_canSubscribe = false;

			ChooseImageCommand = new RelayCommand(ExecuteChooseImage);
			EditCommand = new RelayCommand(ExecuteEdit);
			SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
			DeleteCommand = new RelayCommand(ExecuteDelete);
			CancelCommand = new RelayCommand(ExecuteCancel);
			CloseCommand = new RelayCommand(ExecuteClose);
			DeleteReviewCommand = new RelayCommand(ExecuteDeleteReview);
			AddReviewCommand = new RelayCommand(ExecuteAddReview, CanExecuteAddReview);
			SubscribeCommand = new RelayCommand(ExecuteSubscribe);

			LoadDetails();
			LoadReviews();
			
			CheckIfUserHasReviewed();
			CheckCanSubscribe();
		}
		#endregion

		#region Методы команд
		private void ExecuteEdit(object parameter)
		{
			if (_currentUserRole == UserRole.Coach || _currentUserRole == UserRole.Admin)
			{
				Type = (string)Application.Current.Resources[GetTypeResourceKey(_currentSubscription.SubscriptionType)];
				Duration = (string)Application.Current.Resources[GetDurationResourceKey(_currentSubscription.Duration)];
				
				IsEditMode = true;
				OnPropertyChanged(nameof(EditModeVisible));
				OnPropertyChanged(nameof(AdminEditVisible));
			}
		}

		private string GetTypeResourceKey(string subscriptionType)
		{
			if (string.IsNullOrEmpty(subscriptionType))
				return "Standard";   
				
			if (subscriptionType.Equals("Безлимит", StringComparison.OrdinalIgnoreCase))
				return "Unlimited";
			else if (subscriptionType.Equals("Обычный", StringComparison.OrdinalIgnoreCase))
				return "Standard";
				
			return "Standard";   
		}

		private string GetDurationResourceKey(string duration)
		{
			if (string.IsNullOrEmpty(duration))
				return "OneMonth";   
				
			if (duration.Equals("1 месяц", StringComparison.OrdinalIgnoreCase))
				return "OneMonth";
			else if (duration.Equals("3 месяца", StringComparison.OrdinalIgnoreCase))
				return "ThreeMonths";
			else if (duration.Equals("6 месяцев", StringComparison.OrdinalIgnoreCase))
				return "SixMonths";
			else if (duration.Equals("12 месяцев", StringComparison.OrdinalIgnoreCase))
				return "OneYear";
				
			return "OneMonth";   
		}

		private bool CanExecuteSave(object parameter)
		{
			return true;
		}

		private void ExecuteSave(object parameter)
		{
			try
			{
				IsLoading = true;
				
				List<string> validationErrors = new List<string>();
				
				if (string.IsNullOrEmpty(SubscrName?.Trim()))
				{
					validationErrors.Add((string)Application.Current.Resources["NameRequired"]);
				}
				
				decimal priceValue = 0;
				string normalizedPrice = _price?.Replace(',', '.');
				
				if (string.IsNullOrEmpty(_price?.Trim()))
				{
					validationErrors.Add((string)Application.Current.Resources["PriceRequired"]);
				}
				else if (!decimal.TryParse(normalizedPrice, System.Globalization.NumberStyles.Any, 
				                     System.Globalization.CultureInfo.InvariantCulture, out priceValue))
				{
					validationErrors.Add((string)Application.Current.Resources["InvalidPrice"]);
				}
				else if (priceValue <= 0)
				{
					validationErrors.Add((string)Application.Current.Resources["InvalidPrice"]);
				}
				
				if (string.IsNullOrEmpty(Description?.Trim()))
				{
					validationErrors.Add((string)Application.Current.Resources["EnterDescription"]);
				}
				
				if (string.IsNullOrEmpty(ImagePath?.Trim()))
				{
					validationErrors.Add((string)Application.Current.Resources["EmptyImagePath"]);
				}
				
				if (string.IsNullOrEmpty(Type?.Trim()))
				{
					validationErrors.Add((string)Application.Current.Resources["EmptySubscriptionType"]);
				}
				
				if (string.IsNullOrEmpty(Duration?.Trim()))
				{
					validationErrors.Add((string)Application.Current.Resources["EmptyDuration"]);
				}
				
				if (validationErrors.Count > 0)
				{
					string errorList = string.Join("\n- ", validationErrors);
					errorList = "- " + errorList;
					
					string message = string.Format(
						(string)Application.Current.Resources["ValidationSummary"], 
						errorList);
						
					MessageBox.Show(
						message,
						(string)Application.Current.Resources["ValidationErrorTitle"],
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					
					IsLoading = false;
					return;      
				}
				
				var currentReviews = _currentSubscription.Reviews;
				
				_currentSubscription.Name = SubscrName;
				_currentSubscription.ImagePath = ImagePath;
				_currentSubscription.Price = priceValue;
				
				if (Type == (string)Application.Current.Resources["Unlimited"])
					_currentSubscription.SubscriptionType = "Безлимит";
				else if (Type == (string)Application.Current.Resources["Standard"])
					_currentSubscription.SubscriptionType = "Обычный";
				else
					_currentSubscription.SubscriptionType = GetDbValue(Type, _typeTranslations);
				
				if (Duration == (string)Application.Current.Resources["OneMonth"])
					_currentSubscription.Duration = "1 месяц";
				else if (Duration == (string)Application.Current.Resources["ThreeMonths"])
					_currentSubscription.Duration = "3 месяца";
				else if (Duration == (string)Application.Current.Resources["SixMonths"])
					_currentSubscription.Duration = "6 месяцев";
				else if (Duration == (string)Application.Current.Resources["OneYear"])
					_currentSubscription.Duration = "12 месяцев";
				else
					_currentSubscription.Duration = GetDbValue(Duration, _durationTranslations);
				
				_currentSubscription.Description = Description;

				bool isSuccess = _subscriptionService.Update(_currentSubscription);
				
				if (isSuccess)
				{
					var freshSubscription = _subscriptionService.GetById(_currentSubscription.Id);
					if (freshSubscription != null)
					{
						int subscriptionIndex = -1;
						for (int i = 0; i < _subscriptions.Count; i++)
						{
							if (_subscriptions[i].Id == _currentSubscription.Id)
							{
								subscriptionIndex = i;
								break;
							}
						}
						
						if (subscriptionIndex >= 0)
						{
							_subscriptions[subscriptionIndex] = freshSubscription;
						}
						
						freshSubscription.Reviews = currentReviews;
						_currentSubscription = freshSubscription;
					}
					
					IsEditMode = false;
					
					LoadDetails();
					
					if (_mainWindow != null)
					{
						_mainWindow.UpdateUIWithSubscriptions(_subscriptions.ToList());
					}
					
					MessageBox.Show(
						(string)Application.Current.Resources["SubscriptionUpdatedSuccess"],
						(string)Application.Current.Resources["Success"],
						MessageBoxButton.OK,
						MessageBoxImage.Information);
				}
				else
				{
					MessageBox.Show(
						(string)Application.Current.Resources["SubscriptionUpdateFailed"],
						(string)Application.Current.Resources["Error"],
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error saving subscription: {ex.Message}");
				MessageBox.Show(
					$"{(string)Application.Current.Resources["SubscriptionUpdateFailed"]}\n\n{ex.Message}",
					(string)Application.Current.Resources["Error"],
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
			finally
			{
				IsLoading = false;
				OnPropertyChanged(nameof(ViewModeVisible));
				OnPropertyChanged(nameof(EditModeVisible));
				OnPropertyChanged(nameof(AdminEditVisible));
			}
		}

		private void ExecuteDelete(object parameter)
		{
			if (_currentUserRole != UserRole.Coach && _currentUserRole != UserRole.Admin)
			{
				MessageBox.Show(
					"Только тренеры и администраторы могут удалять абонементы.",
					(string)Application.Current.Resources["AccessDenied"],
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}
			
			var result = MessageBox.Show(
				(string)Application.Current.Resources["DeleteConfirmation"],
				(string)Application.Current.Resources["ConfirmationTitle"],
				MessageBoxButton.YesNo,
				MessageBoxImage.Question);
				
			if (result == MessageBoxResult.Yes)
			{
				try
				{
					IsLoading = true;
					
					bool isSuccess = _subscriptionService.Delete(_currentSubscription.Id);
					
					if (isSuccess)
					{
						for (int i = 0; i < _subscriptions.Count; i++)
						{
							if (_subscriptions[i].Id == _currentSubscription.Id)
							{
								_subscriptions.RemoveAt(i);
								break;
							}
						}
						
						if (_mainWindow != null)
						{
							_mainWindow.UpdateUIWithSubscriptions(_subscriptions.ToList());
						}
						
						SubscriptionDeleted?.Invoke(this, _currentSubscription);
						
						CloseWindow();
						
						MessageBox.Show(
							(string)Application.Current.Resources["SubscriptionDeleted"],
							(string)Application.Current.Resources["Success"],
							MessageBoxButton.OK,
							MessageBoxImage.Information);
					}
					else
					{
						MessageBox.Show(
							(string)Application.Current.Resources["SubscriptionDeleteFailed"],
							(string)Application.Current.Resources["Error"],
							MessageBoxButton.OK,
							MessageBoxImage.Error);
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Error deleting subscription: {ex.Message}");
					MessageBox.Show(
						$"{(string)Application.Current.Resources["SubscriptionDeleteFailed"]}\n\n{ex.Message}",
						(string)Application.Current.Resources["Error"],
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				}
				finally
				{
					IsLoading = false;
				}
			}
		}

		private void ExecuteCancel(object parameter)
		{
			IsEditMode = false;
			LoadDetails();
			OnPropertyChanged(nameof(ViewModeVisible));
			OnPropertyChanged(nameof(EditModeVisible));
			OnPropertyChanged(nameof(AdminEditVisible));
		}

		private void ExecuteClose(object parameter)
		{
			CloseWindow();
		}

		private void CloseWindow()
		{
			RequestClose?.Invoke(this, EventArgs.Empty);
		}

		private void ExecuteDeleteReview(object parameter)
		{
			if (parameter is int reviewId)
			{
				var result = MessageBox.Show((string)Application.Current.Resources["DeleteReviewConfirm"],
					(string)Application.Current.Resources["DeleteConfirmTitle"],
					MessageBoxButton.YesNo, MessageBoxImage.Question);

				if (result == MessageBoxResult.Yes)
				{
					try
					{
						
						string currentUserName = _mainWindow?._user?.Login;
						bool isCurrentUserReview = false;
						
						var reviewToRemove = Reviews.FirstOrDefault(r => r.Id == reviewId);
						if (reviewToRemove != null)
						{
							if (!string.IsNullOrEmpty(currentUserName))
							{
								isCurrentUserReview = string.Equals(reviewToRemove.User, currentUserName, StringComparison.OrdinalIgnoreCase);
							}
							
							_reviewService.Delete(reviewId);
							
							Reviews.Remove(reviewToRemove);
							
							if (isCurrentUserReview)
							{
								HasUserReviewed = false;
							}
							
							if (_currentSubscription.Reviews != null)
							{
								var subReview = _currentSubscription.Reviews.FirstOrDefault(r => r.Id == reviewId);
								if (subReview != null)
								{
									_currentSubscription.Reviews.Remove(subReview);

								}
							}
							
							RecalculateRating();
							
							var subscriptionInCollection = _subscriptions.FirstOrDefault(s => s.Id == _currentSubscription.Id);
							if (subscriptionInCollection != null)
							{
								subscriptionInCollection.Rating = Rating;
							}
							
							_currentSubscription.Rating = Rating;
							bool updateSuccess = _subscriptionService.Update(_currentSubscription);
						

							_justDeletedReview = true;
							_lastDeletedReviewId = reviewId;
							
							ReviewDeleted?.Invoke(this, reviewToRemove);
							
							var updatedSubscriptions = _subscriptionService.GetAll().ToList();
							_subscriptions = new ObservableCollection<Subscription>(updatedSubscriptions);
							
							_mainWindow.UpdateUIWithSubscriptions(_subscriptions.ToList());
							

							MessageBox.Show((string)Application.Current.Resources["ReviewDeletedSuccessfully"],
								(string)Application.Current.Resources["SuccessTitle"],
								MessageBoxButton.OK, MessageBoxImage.Information);
							
							LoadReviews();
						}
						else
						{

							_reviewService.Delete(reviewId);
							
							var freshSubscription = _subscriptionService.GetById(_currentSubscription.Id);
							if (freshSubscription != null)
							{
								_currentSubscription = freshSubscription;
								RecalculateRating();
								
								_currentSubscription.Rating = Rating;
								_subscriptionService.Update(_currentSubscription);
								
								var subscriptionInCollection = _subscriptions.FirstOrDefault(s => s.Id == _currentSubscription.Id);
								if (subscriptionInCollection != null)
								{
									subscriptionInCollection.Rating = Rating;
								}
								
								var updatedSubscriptions = _subscriptionService.GetAll().ToList();
								_subscriptions = new ObservableCollection<Subscription>(updatedSubscriptions);
								_mainWindow.UpdateUIWithSubscriptions(_subscriptions.ToList());
							}
							
							LoadReviews();
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show($"{(string)Application.Current.Resources["ErrorDeletingReview"]}: {ex.Message}",
							(string)Application.Current.Resources["ErrorTitle"],
							MessageBoxButton.OK, MessageBoxImage.Error);
					}
					finally
					{
						_justDeletedReview = false;
					}
				}
			}
		}

		private bool CanExecuteAddReview(object parameter)
		{
			return true;
		}

		private void ExecuteAddReview(object parameter)
		{
			try
			{
				IsLoading = true;
				
				List<string> validationErrors = new List<string>();
				
				if (string.IsNullOrEmpty(ReviewComment?.Trim()))
				{
					validationErrors.Add((string)Application.Current.Resources["CommentRequired"]);
				}
				else if (ReviewComment.Length < 3)
				{
					validationErrors.Add((string)Application.Current.Resources["CommentTooShort"]);
				}
				
				if (ReviewRating <= 0)
				{
					validationErrors.Add((string)Application.Current.Resources["RatingRequired"]);
				}
				
				if (validationErrors.Count > 0)
				{
					string errorList = string.Join("\n- ", validationErrors);
					errorList = "- " + errorList;
					
					string message = string.Format(
						(string)Application.Current.Resources["ValidationSummary"], 
						errorList);
						
					MessageBox.Show(
						message,
						(string)Application.Current.Resources["ValidationErrorTitle"],
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					
					IsLoading = false;
					return;      
				}
				
				string userName = _mainWindow._user.Login;    
				int userId = _mainWindow._user.Id;
				
				System.Diagnostics.Debug.WriteLine($"ExecuteAddReview: Добавление отзыва от пользователя {userName} для абонемента {_currentSubscription.Name} (ID: {_currentSubscription.Id})");
				
				bool hasReviewed = _reviewService.HasUserReviewedSubscription(userName, _currentSubscription.Id);
				
				if (hasReviewed)
				{
					MessageBox.Show((string)Application.Current.Resources["UserAlreadyReviewed"], 
						(string)Application.Current.Resources["InfoTitle"], 
						MessageBoxButton.OK, MessageBoxImage.Information);
						
					ReviewComment = string.Empty;
					ReviewRating = 0;
					System.Diagnostics.Debug.WriteLine($"ExecuteAddReview: Пользователь {userName} уже оставлял отзыв для абонемента {_currentSubscription.Id}");
					return;
				}
				
				bool canReviewSubscription = _reviewService.CanUserReviewSubscription(userId, _currentSubscription.Id);
				
				if (!canReviewSubscription)
				{
					MessageBox.Show(
						"Вы не можете оставить отзыв на абонемент, который никогда не приобретали.",
						"Ограничение доступа", 
						MessageBoxButton.OK, MessageBoxImage.Warning);
						
					ReviewComment = string.Empty;
					ReviewRating = 0;
					System.Diagnostics.Debug.WriteLine($"ExecuteAddReview: Пользователь {userName} не имеет права оставлять отзыв для абонемента {_currentSubscription.Id}");
					return;
				}

				Review newReview = new Review
				{
					User = userName,       
					Comment = ReviewComment,
					Score = ReviewRating,
					CreatedDate = DateTime.Now,
					SubscriptionId = _currentSubscription.Id
				};
				
				System.Diagnostics.Debug.WriteLine($"ExecuteAddReview: Создан отзыв от {userName} для абонемента ID={_currentSubscription.Id}, рейтинг: {ReviewRating}, комментарий: {ReviewComment}");
				
				int reviewId = _reviewService.Add(newReview);
				
				System.Diagnostics.Debug.WriteLine($"ExecuteAddReview: Отзыв сохранен в БД, получен ID={reviewId}");
				
				if (reviewId > 0)
				{
					newReview.Id = reviewId;
					
					HasUserReviewed = true;
					
					System.Diagnostics.Debug.WriteLine($"ExecuteAddReview: Перезагрузка отзывов для обновления UI");
					
					LoadReviews();
					
					if (_currentSubscription.Reviews == null)
					{
						_currentSubscription.Reviews = new List<Review>();
					}
					_currentSubscription.Reviews.Add(newReview);
					
					double newRating = _currentSubscription.CalculateRating();
					Rating = newRating;       
					
					var subscriptionInCollection = _subscriptions.FirstOrDefault(s => s.Id == _currentSubscription.Id);
					if (subscriptionInCollection != null)
					{
						subscriptionInCollection.Rating = Rating;
						if (subscriptionInCollection.Reviews == null)
						{
							subscriptionInCollection.Reviews = new List<Review>();
						}
						subscriptionInCollection.Reviews.Add(newReview);
					}
					
					ReviewAdded?.Invoke(this, newReview);
					
					_mainWindow.UpdateUIWithSubscriptions(_subscriptions.ToList());
					
					ReviewComment = string.Empty;
					ReviewRating = 0;
					
					MessageBox.Show((string)Application.Current.Resources["ReviewAdded"], 
						(string)Application.Current.Resources["SuccessTitle"], 
						MessageBoxButton.OK, MessageBoxImage.Information);
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"ExecuteAddReview: Ошибка при добавлении отзыва, получен ID={reviewId}");
					MessageBox.Show((string)Application.Current.Resources["ErrorAddingReview"], 
						(string)Application.Current.Resources["ErrorTitle"], 
						MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"ExecuteAddReview: Исключение при добавлении отзыва: {ex.Message}");
				MessageBox.Show($"{(string)Application.Current.Resources["ErrorAddingReview"]}: {ex.Message}", 
					(string)Application.Current.Resources["ErrorTitle"], 
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				IsLoading = false;
			}
		}

		private void ExecuteChooseImage(object parameter)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Файлы JPG и PNG (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";
			
			if (openFileDialog.ShowDialog() == true)
			{
				string selectedPath = openFileDialog.FileName;
				
				string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
				string imagesDirectory = Path.Combine(projectDirectory, "Images");
				
				if (selectedPath.StartsWith(imagesDirectory, StringComparison.OrdinalIgnoreCase))
				{
					string relativePath = "Images/" + Path.GetFileName(selectedPath);
					ImagePath = relativePath;
					System.Diagnostics.Debug.WriteLine($"Выбрано изображение из папки проекта. Сохраняем относительный путь: {relativePath}");
				}
				else
				{
					ImagePath = selectedPath;
					System.Diagnostics.Debug.WriteLine($"Выбрано изображение вне папки проекта. Сохраняем абсолютный путь: {selectedPath}");
				}
			}
		}

		private void LoadDetails()
		{
			try
			{
				System.Diagnostics.Debug.WriteLine($"LoadDetails: загрузка абонемента с ID {_currentSubscription.Id}");
				
				var freshSubscription = _subscriptionService.GetById(_currentSubscription.Id);
				if (freshSubscription != null)
				{
					bool hadReviews = _currentSubscription.Reviews?.Count > 0;
					bool hasNewReviews = freshSubscription.Reviews?.Count > 0;
					
					System.Diagnostics.Debug.WriteLine($"LoadDetails: абонемент загружен успешно, отзывов было: {_currentSubscription.Reviews?.Count ?? 0}, " +
													$"отзывов получено: {freshSubscription.Reviews?.Count ?? 0}");
					
					if (hadReviews && !hasNewReviews)
					{
						System.Diagnostics.Debug.WriteLine("LoadDetails: сохраняем текущие отзывы, так как новые не загружены");
						var currentReviews = _currentSubscription.Reviews;
						_currentSubscription = freshSubscription;
						_currentSubscription.Reviews = currentReviews;
					}
					else
					{
						_currentSubscription = freshSubscription;
					}
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("LoadDetails: не удалось загрузить абонемент, получен null");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке данных абонемента: {ex.Message}");
			}
			
			SubscrName = _currentSubscription.Name;
			ImagePath = _currentSubscription.ImagePath;
			Price = _currentSubscription.Price.ToString("0.00", System.Globalization.CultureInfo.CurrentCulture);
			
			if (!IsEditMode)
			{
				Dictionary<string, string> reverseTypeTranslations = GetReverseTypeTranslations();
				Dictionary<string, string> reverseDurationTranslations = GetReverseDurationTranslations();
				
				Type = GetLocalizedValue(_currentSubscription.SubscriptionType, reverseTypeTranslations);
				Duration = GetLocalizedValue(_currentSubscription.Duration, reverseDurationTranslations);
			}
			else
			{
				Type = (string)Application.Current.Resources[GetTypeResourceKey(_currentSubscription.SubscriptionType)];
				Duration = (string)Application.Current.Resources[GetDurationResourceKey(_currentSubscription.Duration)];
			}
			
			Description = _currentSubscription.Description;
			
			if (_currentSubscription.Reviews == null || _currentSubscription.Reviews.Count == 0)
			{
				System.Diagnostics.Debug.WriteLine("LoadDetails: отзывы отсутствуют, загружаем отдельно");
				LoadReviews();
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"LoadDetails: отзывы уже присутствуют ({_currentSubscription.Reviews.Count} шт.), обновляем UI");
				Reviews = new ObservableCollection<Review>(_currentSubscription.Reviews);
				
				CheckIfUserHasReviewed();
				
				RecalculateRating();
				
				OnPropertyChanged(nameof(Reviews));
			}
		}
	
		private void LoadReviews()
		{
			try
			{
				System.Diagnostics.Debug.WriteLine($"LoadReviews: загрузка отзывов для абонемента {_currentSubscription.Name} (ID: {_currentSubscription.Id})");
				
				var reviews = _reviewService.GetBySubscription(_currentSubscription.Id);
				
				if (reviews == null)
				{
					System.Diagnostics.Debug.WriteLine("LoadReviews: сервис вернул null вместо списка отзывов");
					reviews = new List<Review>();
				}
				
				System.Diagnostics.Debug.WriteLine($"LoadReviews: загружено {reviews.Count} отзывов");
				
				foreach (var review in reviews)
				{
					System.Diagnostics.Debug.WriteLine($"LoadReviews: отзыв ID={review.Id}, пользователь={review.User}, оценка={review.Score}, subscriptionId={review.SubscriptionId}");
				}
				
				if (_justDeletedReview)
				{
					var deletedReviewStillExists = reviews.Any(r => r.Id == _lastDeletedReviewId);
					if (deletedReviewStillExists)
					{
						System.Diagnostics.Debug.WriteLine($"LoadReviews: ВНИМАНИЕ! Удаленный отзыв ID={_lastDeletedReviewId} все еще присутствует в БД");
						reviews = reviews.Where(r => r.Id != _lastDeletedReviewId).ToList();
					}
				}
				
				if (_justDeletedReview && Reviews != null && Reviews.Count > 0)
				{
					System.Diagnostics.Debug.WriteLine("LoadReviews: обнаружено недавнее удаление, сохраняем текущую коллекцию");
					
					_currentSubscription.Reviews = new List<Review>(Reviews);
				}
				else
				{
					Reviews = new ObservableCollection<Review>(reviews);
					
					_currentSubscription.Reviews = reviews;
				}
				
				CheckIfUserHasReviewed();
				
				RecalculateRating();
				
				OnPropertyChanged(nameof(Reviews));
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке отзывов: {ex.Message}");
				MessageBox.Show($"{(string)Application.Current.Resources["ErrorLoadingReviews"]}: {ex.Message}", 
					(string)Application.Current.Resources["ErrorTitle"], 
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		
		private void CheckIfUserHasReviewed()
		{
			try
			{
				if (_mainWindow != null && _mainWindow._user != null)
				{
					int userId = _mainWindow._user.Id;
					string userName = _mainWindow._user.Login;
					
					HasUserReviewed = _reviewService.HasUserReviewedSubscription(userName, _currentSubscription.Id);
					System.Diagnostics.Debug.WriteLine($"CheckIfUserHasReviewed: пользователь {userName} {(HasUserReviewed ? "уже оставлял" : "еще не оставлял")} отзыв");
					
					_canReviewSubscription = _reviewService.CanUserReviewSubscription(userId, _currentSubscription.Id);
					System.Diagnostics.Debug.WriteLine($"CheckIfUserHasReviewed: пользователь {userName} {(_canReviewSubscription ? "может" : "не может")} оставлять отзыв (приобретал ли абонемент)");
					
					OnPropertyChanged(nameof(WriteReviewVisible));
				}
				else
				{
					System.Diagnostics.Debug.WriteLine("CheckIfUserHasReviewed: не удалось получить данные пользователя");
					HasUserReviewed = false;
					_canReviewSubscription = false;
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка при проверке наличия отзыва пользователя: {ex.Message}");
				HasUserReviewed = false;
				_canReviewSubscription = false;
			}
		}

		private void RecalculateRating()
		{
			try
			{
				double oldRating = _currentSubscription.Rating;
				
				if (_currentSubscription != null && _currentSubscription.Reviews != null && _currentSubscription.Reviews.Count > 0)
				{
					double newRating = _currentSubscription.CalculateRating();
					Rating = newRating;      
					System.Diagnostics.Debug.WriteLine($"RecalculateRating: новый рейтинг = {Rating} из {_currentSubscription.Reviews.Count} отзывов");
				}
				else
				{
					Rating = 0;      
					System.Diagnostics.Debug.WriteLine("RecalculateRating: установлен нулевой рейтинг (нет отзывов)");
				}

				if (Math.Abs(oldRating - Rating) > 0.01)
				{
					System.Diagnostics.Debug.WriteLine($"Рейтинг изменился с {oldRating} на {Rating}");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка в RecalculateRating: {ex.Message}");
			}
		}
		
		private void CheckCanSubscribe()
		{
			var currentUser = _userService.GetCurrentUser();
			CanSubscribe = currentUser != null && currentUser.Role == UserRole.Client && !currentUser.IsBlocked;
			
			if (CanSubscribe && currentUser != null && _currentSubscription != null)
			{
				CanSubscribe = !_userSubscriptionRepository.HasActiveSubscription(currentUser.Id, _currentSubscription.Id);
			}
		}
		private void ExecuteSubscribe(object parameter)
		{
			try
			{
				var currentUser = _userService.GetCurrentUser();
				if (currentUser == null)
				{
					MessageBox.Show("Для записи на абонемент необходимо авторизоваться.", 
								  "Ошибка авторизации", 
								  MessageBoxButton.OK, 
								  MessageBoxImage.Warning);
					return;
				}

				if (_userSubscriptionRepository.HasActiveSubscription(currentUser.Id, _currentSubscription.Id))
				{
					MessageBox.Show("У вас уже есть активный абонемент данного типа.", 
								 "Информация", 
								 MessageBoxButton.OK, 
								 MessageBoxImage.Information);
					return;
				}

				var subscribeDialog = new View.SubscribeDialog(_currentSubscription);
				var dialogResult = subscribeDialog.ShowDialog();
				
				if (dialogResult == true && subscribeDialog.Result != null)
				{
					var userSubscription = subscribeDialog.Result;
					
					_canReviewSubscription = true;
					OnPropertyChanged(nameof(SubscribeToReviewVisible));
					OnPropertyChanged(nameof(WriteReviewVisible));
					
					System.Diagnostics.Debug.WriteLine($"ExecuteSubscribe: обновлен статус возможности оставления отзыва после покупки абонемента");
					
					CheckCanSubscribe();
					
					MessageBox.Show($"Вы успешно записались на абонемент \"{_currentSubscription.Name}\"!\n" +
								  $"Срок действия: с {userSubscription.PurchaseDate:dd.MM.yyyy} по {userSubscription.ExpiryDate:dd.MM.yyyy}\n\n" +
								  $"Теперь вы можете оставить отзыв об этом абонементе.", 
								  "Успех", 
								  MessageBoxButton.OK, 
								  MessageBoxImage.Information);
					
					if (_mainWindow != null)
					{
						_mainWindow.RefreshUserSubscriptions();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при записи на абонемент: {ex.Message}", 
							 "Ошибка", 
							 MessageBoxButton.OK, 
							 MessageBoxImage.Error);
			}
		}
		
		public void UpdateSubscriptionDetails(Subscription updatedSubscription, List<Subscription> allSubscriptions)
		{
			if (updatedSubscription == null) return;
			
			_currentSubscription = updatedSubscription;
			
			_subscriptions = new ObservableCollection<Subscription>(allSubscriptions);
			
			LoadDetails();
			
			LoadReviews();
			
			CheckIfUserHasReviewed();
			CheckCanSubscribe();
		}
		
		#endregion
	}
}

