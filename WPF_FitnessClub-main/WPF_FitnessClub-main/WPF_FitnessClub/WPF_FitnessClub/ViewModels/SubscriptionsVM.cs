using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPF_FitnessClub.Models;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;
using static WPF_FitnessClub.Commands;
using WPF_FitnessClub.Data;
using WPF_FitnessClub.Data.Services;


namespace WPF_FitnessClub.ViewModels
{
	public class SubscriptionsVM : ViewModelBase
	{
		private MainWindow _mainWindow;
		private List<Subscription> _allSubscriptions;
		private ObservableCollection<Subscription> _filteredSubscriptions;
		private Visibility _filterPanelVisibility = Visibility.Visible;
		private SubscriptionService _subscriptionService;
		private bool _isFiltersApplied = false;
		private bool _isLoading = false;

		private readonly Dictionary<string, string> _typeTranslations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "Unlimited", "Безлимит" },
			{ "Standard", "Обычный" },
			{ "Безлимит", "Безлимит" },
			{ "Обычный", "Обычный" },
		};

        private readonly Dictionary<string, string> _durationTranslations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			// Связываем то, что в выпадающем списке, с тем, что лежит в базе
			{ "1 занятие", "Visit1" },
			{ "4 занятия", "Visit4" },
			{ "8 занятий", "Visit8" },
			{ "16 занятий", "Visit16" },
			{ "32 занятия", "Visit32" },
    
			// Оставляем старые варианты на случай, если в базе еще есть "месяцы"
			{ "1 месяц", "Visit1" },
			{ "3 месяца", "Visit8" },
			{ "12 месяцев", "Visit32" }
		};

        private string _searchText;
		private string _minCost;
		private string _maxCost;
		private ComboBoxItem _selectedType;
		private ComboBoxItem _selectedDuration;

		private string _manipulatePanelButtonContent = "◀";

		public event Action<Subscription> SubscriptionSelected;

        private string _selectedTypeKey;

        #region Свойства

        public ObservableCollection<Subscription> FilteredSubscriptions
		{
			get => _filteredSubscriptions;
			set
			{
				_filteredSubscriptions = value;
				OnPropertyChanged(nameof(FilteredSubscriptions));
			}
		}

		public Visibility FilterPanelVisibility
		{
			get => _filterPanelVisibility;
			set
			{
				_filterPanelVisibility = value;
				OnPropertyChanged(nameof(FilterPanelVisibility));
			}
		}

		public string SearchText
		{
			get => _searchText;
			set
			{
				_searchText = value;
				OnPropertyChanged(nameof(SearchText));
				_isFiltersApplied = true;
				ApplyFilters();
			}
		}

        public string MinCost
        {
            get => _minCost;
            set
            {
                _minCost = value?.Replace(',', '.');
                OnPropertyChanged(nameof(MinCost));
                ApplyFilters();
            }
        }

        public string MaxCost
        {
            get => _maxCost;
            set
            {
                _maxCost = value?.Replace(',', '.');
                OnPropertyChanged(nameof(MaxCost));
                ApplyFilters();
            }
        }

        public ComboBoxItem SelectedType
		{
			get => _selectedType;
			set
			{
				_selectedType = value;
				OnPropertyChanged(nameof(SelectedType));
				_isFiltersApplied = true;
				ApplyFilters();
			}
		}

		public ComboBoxItem SelectedDuration
		{
			get => _selectedDuration;
			set
			{
				_selectedDuration = value;
				OnPropertyChanged(nameof(SelectedDuration));
				_isFiltersApplied = true;
				ApplyFilters();
			}
		}

		public string ManipulatePanelButtonContent
		{
			get => _manipulatePanelButtonContent;
			set
			{
				_manipulatePanelButtonContent = value;
				OnPropertyChanged(nameof(ManipulatePanelButtonContent));
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

        private string _selectedDurationKey;
        public string SelectedDurationKey
        {
            get => _selectedDurationKey;
            set { _selectedDurationKey = value; OnPropertyChanged("SelectedDurationKey"); ApplyFilters(); }
        }
        #endregion

        #region Команды

        public ICommand ToggleFilterPanelCommand { get; }
		public ICommand SelectSubscriptionCommand { get; }
		public ICommand RefreshSubscriptionsCommand { get; }
        public ICommand ResetFiltersCommand { get; private set; }
        #endregion

        public SubscriptionsVM(MainWindow mainWindow, List<Subscription> subscriptions)
		{
			_mainWindow = mainWindow;
			_subscriptionService = new SubscriptionService();

			LanguageManager.Instance.LanguageChanged += OnLanguageChanged;

			ToggleFilterPanelCommand = new RelayCommand(ToggleFilterPanel);
			SelectSubscriptionCommand = new RelayCommand<Subscription>(OnSubscriptionSelected);
            RefreshSubscriptionsCommand = new RelayCommand(_ => RefreshSubscriptions());
            ResetFiltersCommand = new RelayCommand(p => ResetFilters());

            _searchText = string.Empty;
            _minCost = string.Empty;
            _maxCost = string.Empty;
            _selectedType = null;      
            _selectedDuration = null;      
            
			System.Diagnostics.Debug.WriteLine($"Текущий язык интерфейса: {GetCurrentLanguage()}");
            
			RefreshSubscriptions();
		}
        
		private void LoadAllSubscriptions(List<Subscription> subscriptions)
		{
			System.Diagnostics.Debug.WriteLine($"LoadAllSubscriptions вызван с {subscriptions?.Count ?? 0} абонементами");
			
			if (subscriptions == null)
			{
				System.Diagnostics.Debug.WriteLine("Список абонементов равен null, создаем пустой список");
				_allSubscriptions = new List<Subscription>();
			}
			else
			{
				_allSubscriptions = subscriptions;
				
				foreach (var subscription in _allSubscriptions)
				{
					if (subscription.Reviews != null && subscription.Reviews.Count > 0)
					{
						subscription.Rating = subscription.CalculateRating();
						System.Diagnostics.Debug.WriteLine($"Рассчитан рейтинг для абонемента {subscription.Name}: {subscription.Rating}");
					}
					else
					{
						subscription.Rating = 0;
						System.Diagnostics.Debug.WriteLine($"Нулевой рейтинг для абонемента {subscription.Name} (нет отзывов)");
					}
				}
			}
			
			if (!_isFiltersApplied)
			{
				System.Diagnostics.Debug.WriteLine("Фильтры не применены, показываем все абонементы");
				FilteredSubscriptions = new ObservableCollection<Subscription>(_allSubscriptions);
				System.Diagnostics.Debug.WriteLine($"FilteredSubscriptions содержит {FilteredSubscriptions.Count} элементов");
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("Применяем фильтры к обновленному списку");
				ApplyFilters();
			}
		}


		private void RefreshSubscriptions()
		{
			try
			{
                IsLoading = true;
                
				var subscriptions = _subscriptionService.GetAll();
				LoadAllSubscriptions(subscriptions);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка при обновлении списка абонементов: {ex.Message}", 
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
                IsLoading = false;
			}
		}

		private bool IsYearDuration(string duration)
		{
			if (string.IsNullOrEmpty(duration))
				return false;
				
			string normalizedDuration = duration.ToLower().Trim();
			
			return normalizedDuration == "1 год" || 
				   normalizedDuration == "12 месяцев" ||
				   normalizedDuration == "годовой" ||
				   normalizedDuration == "1year" ||
				   normalizedDuration == "12months" ||
				   normalizedDuration == "one year" ||
				   normalizedDuration == "twelve months" ||
				   normalizedDuration.Contains("год") && normalizedDuration.Contains("1") ||
				   normalizedDuration.Contains("months") && normalizedDuration.Contains("12") ||
				   normalizedDuration.Contains("месяц") && normalizedDuration.Contains("12");
		}

        private void ApplyFilters()
        {
            try
            {
                if (_allSubscriptions == null)
                {
                    System.Diagnostics.Debug.WriteLine("ApplyFilters: Список всех абонементов пуст (_allSubscriptions == null).");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("ApplyFilters: Начало фильтрации...");

                // Создаем запрос на основе всех абонементов
                var filtered = _allSubscriptions.AsQueryable();

                // 1. Поиск по тексту (названию)
                if (!string.IsNullOrEmpty(SearchText))
                {
                    string searchLower = SearchText.ToLower();
                    filtered = filtered.Where(s => s.Name != null && s.Name.ToLower().Contains(searchLower));
                    System.Diagnostics.Debug.WriteLine($"ApplyFilters: Применен поиск по тексту: '{SearchText}'");
                }

                // 2. Фильтрация по минимальной цене
                // Используем InvariantCulture, чтобы TryParse всегда понимал точку как разделитель дробной части
                if (decimal.TryParse(MinCost, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal min))
                {
                    filtered = filtered.Where(s => s.Price >= min);
                    System.Diagnostics.Debug.WriteLine($"ApplyFilters: Применен фильтр 'Цена от': {min}");
                }

                // 3. Фильтрация по максимальной цене
                if (decimal.TryParse(MaxCost, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal max))
                {
                    filtered = filtered.Where(s => s.Price <= max);
                    System.Diagnostics.Debug.WriteLine($"ApplyFilters: Применен фильтр 'Цена до': {max}");
                }

                // 4. Фильтрация по ТИПУ абонемента
                // Проверяем, что ключ не пустой и не равен значению по умолчанию "All"
                if (!string.IsNullOrEmpty(SelectedTypeKey) && SelectedTypeKey != "All")
                {
                    filtered = filtered.Where(s => s.SubscriptionType == SelectedTypeKey);
                    System.Diagnostics.Debug.WriteLine($"ApplyFilters: Применен фильтр по типу: '{SelectedTypeKey}'");
                }

                // 5. Фильтрация по ДЛИТЕЛЬНОСТИ
                if (!string.IsNullOrEmpty(SelectedDurationKey) && SelectedDurationKey != "All")
                {
                    filtered = filtered.Where(s => s.Duration == SelectedDurationKey);
                    System.Diagnostics.Debug.WriteLine($"ApplyFilters: Применен фильтр по длительности: '{SelectedDurationKey}'");
                }

                // Выполняем запрос и обновляем коллекцию для отображения в интерфейсе
                var result = filtered.ToList();
                FilteredSubscriptions = new ObservableCollection<Subscription>(result);

                System.Diagnostics.Debug.WriteLine($"ApplyFilters: Фильтрация успешно завершена. Отображено элементов: {FilteredSubscriptions.Count}");
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем работу приложения
                System.Diagnostics.Debug.WriteLine("ApplyFilters: Ошибка при выполнении фильтрации: " + ex.Message);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("ApplyFilters: Внутренняя ошибка: " + ex.InnerException.Message);
                }
            }
        }

        public void ValidatePriceRange()
        {
            if (decimal.TryParse(MinCost, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal minVal) &&
                decimal.TryParse(MaxCost, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal maxVal))
            {
                if (minVal > maxVal)
                {
                    string errorMsg = Application.Current.FindResource("InvalidPriceRange") as string;
                    string errorTitle = Application.Current.FindResource("ValidationError") as string;

                    MessageBox.Show(errorMsg, errorTitle, MessageBoxButton.OK, MessageBoxImage.Warning);

                    // Меняем местами
                    string temp = _minCost;
                    _minCost = _maxCost;
                    _maxCost = temp;

                    OnPropertyChanged(nameof(MinCost));
                    OnPropertyChanged(nameof(MaxCost));

                    ApplyFilters(); // Перефильтруем после перестановки
                }
            }
        }
        private string GetCurrentLanguage()
		{
			try
			{
				string languageCode = LanguageManager.Instance.CurrentLanguage;
				System.Diagnostics.Debug.WriteLine($"Получен язык из LanguageManager: {languageCode}");
				
				if (languageCode.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
					return "ru";
				else if (languageCode.StartsWith("en", StringComparison.OrdinalIgnoreCase))
					return "en";
				
				string uiCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
				if (!string.IsNullOrEmpty(uiCulture))
				{
					System.Diagnostics.Debug.WriteLine($"Определен язык по культуре потока: {uiCulture}");
					if (uiCulture == "ru" || uiCulture == "en")
						return uiCulture;
				}

				var resources = Application.Current.Resources;
				
				foreach (var dict in resources.MergedDictionaries)
				{
					if (dict.Source != null)
					{
						string source = dict.Source.OriginalString;
						System.Diagnostics.Debug.WriteLine($"Найден словарь ресурсов: {source}");
						
						if (source.Contains("Dictionary_ru"))
						{
							if (dict.Contains("Language") && dict["Language"].ToString().Contains("ru"))
							{
								System.Diagnostics.Debug.WriteLine("Найден активный русский словарь");
								return "ru";
							}
							
							System.Diagnostics.Debug.WriteLine("Определен русский язык по имени файла словаря");
							return "ru";
						}
						else if (source.Contains("Dictionary_en"))
						{
							if (dict.Contains("Language") && dict["Language"].ToString().Contains("en"))
							{
								System.Diagnostics.Debug.WriteLine("Найден активный английский словарь");
								return "en";
							}
							
							System.Diagnostics.Debug.WriteLine("Определен английский язык по имени файла словаря");
							return "en";
						}
					}
				}
				
				System.Globalization.CultureInfo currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
				if (currentCulture.Name.StartsWith("ru"))
				{
					System.Diagnostics.Debug.WriteLine("Определен русский язык по культуре потока");
					return "ru";
				}
				
				System.Diagnostics.Debug.WriteLine("Не удалось определить язык, возвращаем английский по умолчанию");
				return "en";
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка при определении языка интерфейса: {ex.Message}");
				return "en";    
			}
		}


		private string GetDatabaseValue(string uiValue, Dictionary<string, string> translations)
		{
			if (string.IsNullOrEmpty(uiValue) || 
				uiValue.Equals("Все", StringComparison.OrdinalIgnoreCase) || 
				uiValue.Equals("All", StringComparison.OrdinalIgnoreCase))
			{
				return null;      
			}

			string currentLang = GetCurrentLanguage();
			System.Diagnostics.Debug.WriteLine($"Текущий язык: {currentLang}, исходное значение: {uiValue}");

			if (currentLang == "en")
			{
				if (uiValue.Equals("Unlimited", StringComparison.OrdinalIgnoreCase))
					return "Безлимит";
				if (uiValue.Equals("Standard", StringComparison.OrdinalIgnoreCase))
					return "Обычный";
					
				if (uiValue.Contains("Month") || uiValue.Contains("месяц"))
				{
					if (uiValue.Contains("1") || uiValue.Contains("One"))
						return "1 месяц";
					if (uiValue.Contains("3") || uiValue.Contains("Three"))
						return "3 месяца";
					if (uiValue.Contains("6") || uiValue.Contains("Six"))
						return "6 месяцев";
					if (uiValue.Contains("12") || uiValue.Contains("Twelve"))
						return "12 месяцев";
				}
				if (uiValue.Contains("Year") || uiValue.Contains("год"))
					return "1 год";
			}

			if (translations.TryGetValue(uiValue, out string dbValue))
			{
				System.Diagnostics.Debug.WriteLine($"Найдено прямое соответствие: {uiValue} -> {dbValue}");
				return dbValue;
			}
			
			string trimmedValue = uiValue.Trim();
			if (translations.TryGetValue(trimmedValue, out dbValue))
			{
				System.Diagnostics.Debug.WriteLine($"Найдено соответствие после удаления пробелов: {trimmedValue} -> {dbValue}");
				return dbValue;
			}
			
			string normalizedValue = trimmedValue.ToLower().Replace(" ", "");
			foreach (var pair in translations)
			{
				string normalizedKey = pair.Key.ToLower().Replace(" ", "");
				if (normalizedKey == normalizedValue || 
					normalizedValue.Contains(normalizedKey) || 
					normalizedKey.Contains(normalizedValue))
				{
					System.Diagnostics.Debug.WriteLine($"Найдено соответствие после нормализации: {normalizedValue} -> {pair.Value}");
					return pair.Value;
				}
			}
			
			if (translations == _typeTranslations)
			{
				if (uiValue.Contains("без") || uiValue.Contains("лимит") || 
					uiValue.Contains("unlim") || uiValue.Contains("limit"))
					return "Безлимитный";
					
				if (uiValue.Contains("стандарт") || uiValue.Contains("обычн") || 
					uiValue.Contains("standard") || uiValue.Contains("regular"))
					return "Обычный";
			}
			else if (translations == _durationTranslations)
			{
				if (uiValue.Contains("1") || uiValue.Contains("один") || uiValue.Contains("one"))
				{
					if (uiValue.Contains("мес") || uiValue.Contains("mon"))
						return "1 месяц";
					if (uiValue.Contains("год") || uiValue.Contains("year") || uiValue.Contains("лет"))
						return "1 год";
				}
				
				if (uiValue.Contains("3") || uiValue.Contains("три") || uiValue.Contains("three"))
					return "3 месяца";
					
				if (uiValue.Contains("6") || uiValue.Contains("шесть") || uiValue.Contains("six"))
					return "6 месяцев";
					
				if (uiValue.Contains("12") || uiValue.Contains("двенадцать") || uiValue.Contains("twelve"))
					return "12 месяцев";
					
				if (uiValue.Contains("год") || uiValue.Contains("year") || 
				   uiValue.Contains("годов") || uiValue.Contains("annual"))
					return "1 год";
			}

			System.Diagnostics.Debug.WriteLine($"Значение '{uiValue}' не найдено в словаре соответствий");
			return uiValue;
		}

		private void ToggleFilterPanel(object parameter)
		{
			if (FilterPanelVisibility == Visibility.Visible)
			{
				FilterPanelVisibility = Visibility.Collapsed;
				ManipulatePanelButtonContent = "▶";
			}
			else
			{
				FilterPanelVisibility = Visibility.Visible;
				ManipulatePanelButtonContent = "◀";
			}
		}


		private void OnSubscriptionSelected(Subscription subscription)
		{
			SubscriptionSelected?.Invoke(subscription);
		}

	
		public void UpdateSubscriptions(List<Subscription> subscriptions)
		{
			try
			{
				IsLoading = true;
				System.Diagnostics.Debug.WriteLine($"SubscriptionsVM.UpdateSubscriptions: Обновление списка с {subscriptions?.Count ?? 0} абонементами");
				
				int oldCount = _allSubscriptions?.Count ?? 0;
				
				_allSubscriptions = new List<Subscription>(subscriptions ?? new List<Subscription>());
				
				int newCount = _allSubscriptions.Count;
				if (newCount < oldCount)
				{
					System.Diagnostics.Debug.WriteLine($"SubscriptionsVM.UpdateSubscriptions: Обнаружено удаление элементов (было {oldCount}, стало {newCount})");
					_isFiltersApplied = false;
					ResetFilters();
				}
				
				if (_isFiltersApplied)
				{
					System.Diagnostics.Debug.WriteLine($"SubscriptionsVM.UpdateSubscriptions: Применяю фильтры");
					ApplyFilters();
				}
				else
				{
					System.Diagnostics.Debug.WriteLine($"SubscriptionsVM.UpdateSubscriptions: Отображаю все абонементы без фильтрации");
					FilteredSubscriptions = new ObservableCollection<Subscription>(_allSubscriptions);
				}
				
				OnPropertyChanged(nameof(FilteredSubscriptions));
				System.Diagnostics.Debug.WriteLine($"SubscriptionsVM.UpdateSubscriptions: Отображается {FilteredSubscriptions.Count} элементов");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"SubscriptionsVM.UpdateSubscriptions: Ошибка: {ex.Message}");
				MessageBox.Show($"Ошибка обновления абонементов: {ex.Message}", 
					"Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			finally
			{
				IsLoading = false;
			}
            FilteredSubscriptions = new ObservableCollection<Subscription>(_allSubscriptions);
            OnPropertyChanged("FilteredSubscriptions");
        }
		
		public void ResetFilters()
		{
			_searchText = string.Empty;
			_minCost = string.Empty;
			_maxCost = string.Empty;

            SelectedTypeKey = "All";
            SelectedDurationKey = "All";
            _isFiltersApplied = false;
			
			OnPropertyChanged(nameof(SearchText));
			OnPropertyChanged(nameof(MinCost));
			OnPropertyChanged(nameof(MaxCost));
			OnPropertyChanged(nameof(SelectedType));
			OnPropertyChanged(nameof(SelectedDuration));
			
			FilteredSubscriptions = new ObservableCollection<Subscription>(_allSubscriptions);

            OnPropertyChanged(nameof(FilteredSubscriptions));
        }
		
		private void ValidateAndCorrectPrices()
		{
			if (string.IsNullOrEmpty(_minCost) || string.IsNullOrEmpty(_maxCost))
				return;

			if (decimal.TryParse(_minCost, out decimal min) && decimal.TryParse(_maxCost, out decimal max))
			{
				if (max < min)
				{
					string tempMin = _minCost;
					string tempMax = _maxCost;

					_minCost = tempMax;
					_maxCost = tempMin;

					OnPropertyChanged(nameof(MinCost));
					OnPropertyChanged(nameof(MaxCost));
					
					MessageBox.Show(
						"Значения минимальной и максимальной цены были автоматически поменяны местами, так как максимальная цена была меньше минимальной.",
						"Автоматическая коррекция",
						MessageBoxButton.OK,
						MessageBoxImage.Information);
				}
			}
		}

        public string SelectedTypeKey
        {
            get => _selectedTypeKey;
            set { _selectedTypeKey = value; OnPropertyChanged("SelectedTypeKey"); ApplyFilters(); }
        }
        
		private void OnLanguageChanged(object sender, string languageCode)
		{
			System.Diagnostics.Debug.WriteLine($"Язык изменен на: {languageCode}");
			
			if (_isFiltersApplied)
			{
				ApplyFilters();
			}
		}

	
	}
}