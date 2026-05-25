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

        private string _origName, _origDesc, _origImagePath, _origType, _origDuration;
        private decimal _origPrice;


        private readonly Dictionary<string, string> _typeTranslations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			// { "То, что написано в меню", "То, что лежит в Базе (Ключ)" }
			{ "Безлимит", "Unlimited" },
			{ "Обычный", "Standard" },
			{ "Групповая", "Group" },
			{ "Утренний", "Morning" },
			{ "Вечерний", "Evening" },
			{ "Одиночная", "Single" },
    
			// Оставляем английские варианты для совместимости (если язык переключен)
			{ "Unlimited", "Unlimited" },
			{ "Standard", "Standard" },
			{ "Group", "Group" },
			{ "Morning", "Morning" },
			{ "Evening", "Evening" },
			{ "Single", "Single" }
		};

        private readonly Dictionary<string, string> _durationTranslations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			// { "То, что в меню", "То, что в Базе" }
			{ "1 занятие", "Visit1" },
			{ "4 занятия", "Visit4" },
			{ "8 занятий", "Visit8" },
			{ "16 занятий", "Visit16" },
			{ "32 занятия", "Visit32" },
    
			// Старые варианты из месяцев для страховки
			{ "1 месяц", "Visit1" },
			{ "3 месяца", "Visit8" },
			{ "6 месяцев", "Visit16" },
			{ "12 месяцев", "Visit32" }
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

        public string LocalizedType => _currentSubscription?.LocalizedType;

        public string Duration
        {
            get => _currentSubscription?.Duration;
            set
            {
                if (_currentSubscription != null && _currentSubscription.Duration != value)
                {
                    _currentSubscription.Duration = value;
                    OnPropertyChanged(nameof(Duration));
                    OnPropertyChanged(nameof(LocalizedDuration));
                }
            }
        }

        public string LocalizedDuration => _currentSubscription?.LocalizedDuration;

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                _isEditMode = value;
                OnPropertyChanged(nameof(IsEditMode));
                OnPropertyChanged(nameof(ViewModeVisible));
                OnPropertyChanged(nameof(EditModeVisible));
                OnPropertyChanged(nameof(AdminEditVisible));
                OnPropertyChanged(nameof(CanSubscribeVisible));
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

        // Теперь кнопку "Удалить" на отзыве увидят и Админ, и Тренер
		public Visibility DeleteReviewVisible => (_currentUserRole == UserRole.Admin || _currentUserRole == UserRole.Coach) ? Visibility.Visible : Visibility.Collapsed;
        // Показываем блок "Для оставления отзыва купите..." только если клиент еще НЕ купил
        public Visibility SubscribeToReviewVisible =>
            (_currentUserRole == UserRole.Client && !_canReviewSubscription) ? Visibility.Visible : Visibility.Collapsed;

        // Показываем форму ввода "Ваше мнение очень важно" только если клиент КУПИЛ и еще НЕ писал отзыв
        public Visibility WriteReviewVisible =>(_currentUserRole == UserRole.Client && _canReviewSubscription && !HasUserReviewed) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CanReplyVisible => (_currentUserRole != UserRole.Client) ? Visibility.Visible : Visibility.Collapsed;
        private void ExecuteSendReply(object parameter)
        {
            MessageBox.Show("Для ввода текста ответа нужно создать окно ввода. Пока эта функция просто выводит это сообщение.",
                            "Техническое сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public string SubscriptionType
        {
            get => _currentSubscription?.SubscriptionType;
            set
            {
                if (_currentSubscription != null && _currentSubscription.SubscriptionType != value)
                {
                    _currentSubscription.SubscriptionType = value;
                    OnPropertyChanged(nameof(SubscriptionType));
                    // Пинкаем LocalizedType, чтобы текст на экране обновился
                    OnPropertyChanged(nameof(LocalizedType));
                }
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
		public ICommand SendReplyCommand { get; private set; }
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
            // 1. Создаем бэкап КЛЮЧЕЙ, а не перевода
            _origName = _currentSubscription.Name;
            _origDesc = _currentSubscription.Description;
            _origPrice = _currentSubscription.Price;
            _origImagePath = _currentSubscription.ImagePath;
            _origType = _currentSubscription.SubscriptionType;
            _origDuration = _currentSubscription.Duration;

            // 2. Переносим данные в поля ввода
            LoadDetails();

            // 3. Включаем режим
            IsEditMode = true;
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

                // 1. Синхронизируем данные
                _currentSubscription.Name = SubscrName;
                _currentSubscription.Description = Description;
                _currentSubscription.ImagePath = ImagePath;
                _currentSubscription.SubscriptionType = SubscriptionType;
                _currentSubscription.Duration = Duration;

                if (decimal.TryParse(Price.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal p))
                {
                    _currentSubscription.Price = p;
                }

                // 2. Просто вызываем обновление. 
                // Если в БД что-то пойдет не так (ошибка связи, валидация БД), программа уйдет в блок catch.
                _subscriptionService.Update(_currentSubscription);

                // 3. Обновляем "бэкап" (снимки)
                _origName = _currentSubscription.Name;
                _origDesc = _currentSubscription.Description;
                _origPrice = _currentSubscription.Price;
                _origImagePath = _currentSubscription.ImagePath;
                _origType = _currentSubscription.SubscriptionType;
                _origDuration = _currentSubscription.Duration;

                // 4. Синхронизация с главным окном
                if (_mainWindow != null && _mainWindow.subscriptions != null)
                {
                    var mainSub = _mainWindow.subscriptions.FirstOrDefault(s => s.Id == _currentSubscription.Id);
                    if (mainSub != null)
                    {
                        mainSub.Name = _currentSubscription.Name;
                        mainSub.Price = _currentSubscription.Price;
                        mainSub.ImagePath = _currentSubscription.ImagePath;
                        mainSub.SubscriptionType = _currentSubscription.SubscriptionType;
                        mainSub.Duration = _currentSubscription.Duration;
                        mainSub.Description = _currentSubscription.Description;
                    }
                    _mainWindow.UpdateUIWithSubscriptions(_mainWindow.subscriptions);
                }

                // 5. Сначала выключаем индикаторы и режим, чтобы UI "отпустило"
                IsLoading = false;
                IsEditMode = false;
                LoadDetails(); // Обновляем текст на экране

                // 6. Показываем успех (теперь это будет происходить всегда, если не было ошибки)
                MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                IsLoading = false;
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
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

        // ОТМЕНА (Теперь возвращает всё назад)
        private void ExecuteCancel(object parameter)
        {
            // Возвращаем ключи в модель
            _currentSubscription.Name = _origName;
            _currentSubscription.Description = _origDesc;
            _currentSubscription.Price = _origPrice;
            _currentSubscription.ImagePath = _origImagePath;
            _currentSubscription.SubscriptionType = _origType;
            _currentSubscription.Duration = _origDuration;

            IsEditMode = false;
            LoadDetails(); // Обновляем экран
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
            if (!_canReviewSubscription)
            {
                MessageBox.Show("Ай-ай-ай! Отзывы могут оставлять только клиенты, которые приобрели этот абонемент.",
                                "Доступ ограничен", MessageBoxButton.OK, MessageBoxImage.Warning);
                ReviewComment = string.Empty;
                ReviewRating = 0;
                return;
            }

            try
            {
                IsLoading = true;

                if (string.IsNullOrWhiteSpace(ReviewComment) || ReviewComment.Length < 3)
                {
                    MessageBox.Show("Пожалуйста, введите текст отзыва (минимум 3 символа).", "Ошибка валидации");
                    return;
                }
                if (ReviewRating <= 0)
                {
                    MessageBox.Show("Пожалуйста, поставьте оценку.", "Ошибка");
                    return;
                }

                string userName = _mainWindow._user.Login;

                Review newReview = new Review
                {
                    User = userName,
                    Comment = ReviewComment,
                    Score = ReviewRating,
                    CreatedDate = DateTime.Now,
                    SubscriptionId = _currentSubscription.Id
                };

                int reviewId = _reviewService.Add(newReview);

                if (reviewId > 0)
                {
                    newReview.Id = reviewId;
                    HasUserReviewed = true;

                    // 1. Добавляем отзыв в текущую модель (для этого окна)
                    if (_currentSubscription.Reviews == null) _currentSubscription.Reviews = new List<Review>();
                    _currentSubscription.Reviews.Add(newReview);

                    // Считаем новый рейтинг
                    double newRating = _currentSubscription.CalculateRating();

                    // 2. ОБНОВЛЯЕМ ЗВЕЗДЫ В ТЕКУЩЕМ ОКНЕ
                    _currentSubscription.Rating = newRating;
                    OnPropertyChanged("Rating");
                    LoadReviews(); // Обновит список текстом внизу

                    // 3. ОБНОВЛЯЕМ ЗВЕЗДЫ НА ГЛАВНОМ ЭКРАНЕ (БЕЗ ПЕРЕЗАХОДА)
                    if (_mainWindow != null && _mainWindow.subscriptions != null)
                    {
                        // Находим тот же самый абонемент в списке главного окна
                        var subInMainList = _mainWindow.subscriptions.FirstOrDefault(s => s.Id == _currentSubscription.Id);
                        if (subInMainList != null)
                        {
                            // Обновляем его данные
                            subInMainList.Reviews = _currentSubscription.Reviews;
                            // ВАЖНО: вызываем установку рейтинга, которая "пинает" интерфейс
                            subInMainList.Rating = newRating;
                        }

                        // Просим главное окно обновить привязки (force refresh)
                        _mainWindow.UpdateUIWithSubscriptions(_mainWindow.subscriptions);
                    }

                    ReviewComment = string.Empty;
                    ReviewRating = 0;
                    OnPropertyChanged("WriteReviewVisible"); // Прячем форму ввода

                    MessageBox.Show("Отзыв успешно добавлен!", "Успех");
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
            finally { IsLoading = false; }
        }
        private void ExecuteChooseImage(object parameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Картинки (*.jpg;*.png)|*.jpg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                // Берем ТОЛЬКО имя файла (например "gym2.jpg")
                ImagePath = System.IO.Path.GetFileName(openFileDialog.FileName);
                OnPropertyChanged("ImagePath");
            }
        }

        private void LoadDetails()
        {
            // Синхронизируем простые текстовые поля
            SubscrName = _currentSubscription.Name;
            ImagePath = _currentSubscription.ImagePath;
            Description = _currentSubscription.Description;
            Price = _currentSubscription.Price.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

            // Уведомляем UI о том, что нужно перечитать данные из модели
            OnPropertyChanged(nameof(SubscrName));
            OnPropertyChanged(nameof(Price));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(ImagePath));

            // ВАЖНО: уведомляем о ключах и их локализованных версиях
            OnPropertyChanged(nameof(SubscriptionType));
            OnPropertyChanged(nameof(Duration));
            OnPropertyChanged(nameof(LocalizedType));
            OnPropertyChanged(nameof(LocalizedDuration));
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
                if (currentUser == null) return;

                var subscribeDialog = new View.SubscribeDialog(_currentSubscription);
                if (subscribeDialog.ShowDialog() == true && subscribeDialog.Result != null)
                {
                    var userSub = subscribeDialog.Result;

                    // 1. Устанавливаем флаг. Теперь логика свойств видимости изменилась в памяти.
                    _canReviewSubscription = true;

                    // Логика локализации сообщения (сохраняем твой блок без изменений)
                    string resourceMsg = (string)Application.Current.Resources["SubscriptionSuccessMessage"];
                    string message;
                    if (!string.IsNullOrEmpty(resourceMsg))
                    {
                        message = string.Format(resourceMsg, _currentSubscription.Name,
                                  userSub.PurchaseDate.ToString("dd.MM.yyyy"),
                                  userSub.ExpiryDate.ToString("dd.MM.yyyy"));
                    }
                    else
                    {
                        message = $"Успех! Вы записались на абонемент \"{_currentSubscription.Name}\".\n" +
                                  $"Срок: с {userSub.PurchaseDate:dd.MM.yyyy} по {userSub.ExpiryDate:dd.MM.yyyy}.\n" +
                                  $"Теперь вы можете оставить отзыв!";
                    }

                    MessageBox.Show(message, "Запись подтверждена", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 2. УВЕДОМЛЯЕМ ИНТЕРФЕЙС ОБ ИЗМЕНЕНИЯХ:

                    // Появится блок "Ваше мнение очень важно"
                    OnPropertyChanged("WriteReviewVisible");

                    // ИСЧЕЗНЕТ блок "Для оставления отзыва необходимо приобрести..." (ЭТОГО НЕ ХВАТАЛО)
                    OnPropertyChanged("SubscribeToReviewVisible");

                    // Обновляем состояние кнопки "Записаться" внизу окна
                    CheckCanSubscribe();

                    // Обновляем список в главном окне
                    if (_mainWindow != null) _mainWindow.RefreshUserSubscriptions();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при записи: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
        
    }
        #endregion
    
}
