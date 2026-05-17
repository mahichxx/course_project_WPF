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
			{ "OneMonth", "1 месяц" },
			{ "One Month", "1 месяц" },
			{ "1 Month", "1 месяц" },
			{ "ThreeMonths", "3 месяца" },
			{ "Three Months", "3 месяца" },
			{ "3 Months", "3 месяца" },
			{ "SixMonths", "6 месяцев" },
			{ "Six Months", "6 месяцев" },
			{ "6 Months", "6 месяцев" },
			{ "OneYear", "1 год" },
			{ "One Year", "1 год" },
			{ "1 Year", "1 год" },
			{ "TwelveMonths", "12 месяцев" },
			{ "Twelve Months", "12 месяцев" },
			{ "12 Months", "12 месяцев" },
			{ "1 месяц", "1 месяц" },
			{ "3 месяца", "3 месяца" },
			{ "6 месяцев", "6 месяцев" },
			{ "1 год", "1 год" },
			{ "12 месяцев", "12 месяцев" }
		};

		private string _searchText;
		private string _minCost;
		private string _maxCost;
		private ComboBoxItem _selectedType;
		private ComboBoxItem _selectedDuration;

		private string _manipulatePanelButtonContent = "◀";

		public event Action<Subscription> SubscriptionSelected;

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
				_minCost = value;
				OnPropertyChanged(nameof(MinCost));
				_isFiltersApplied = true;
				ValidateAndCorrectPrices();
				ApplyFilters();
			}
		}

		public string MaxCost
		{
			get => _maxCost;
			set
			{
				_maxCost = value;
				OnPropertyChanged(nameof(MaxCost));
				_isFiltersApplied = true;
				ValidateAndCorrectPrices();
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

		#endregion

		#region Команды

		public ICommand ToggleFilterPanelCommand { get; }
		public ICommand SelectSubscriptionCommand { get; }
		public ICommand RefreshSubscriptionsCommand { get; }

		#endregion

		public SubscriptionsVM(MainWindow mainWindow, List<Subscription> subscriptions)
		{
			_mainWindow = mainWindow;
			_subscriptionService = new SubscriptionService();

			LanguageManager.Instance.LanguageChanged += OnLanguageChanged;

			ToggleFilterPanelCommand = new RelayCommand(ToggleFilterPanel);
			SelectSubscriptionCommand = new RelayCommand<Subscription>(OnSubscriptionSelected);
            RefreshSubscriptionsCommand = new RelayCommand(_ => RefreshSubscriptions());
            
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
				string currentLang = GetCurrentLanguage();
				System.Diagnostics.Debug.WriteLine($"Applying filters... Current language: {currentLang}");
				
				if (_allSubscriptions == null)
				{
					System.Diagnostics.Debug.WriteLine("_allSubscriptions is null, returning empty list");
					FilteredSubscriptions = new ObservableCollection<Subscription>();
					return;
				}

				decimal? minCostValue = null;
				decimal? maxCostValue = null;

				if (!string.IsNullOrEmpty(MinCost) && decimal.TryParse(MinCost, out decimal min))
				{
					minCostValue = min;
				}

				if (!string.IsNullOrEmpty(MaxCost) && decimal.TryParse(MaxCost, out decimal max))
				{
					maxCostValue = max;
				}

				if (minCostValue.HasValue && maxCostValue.HasValue)
				{
					if (minCostValue.Value > maxCostValue.Value)
					{
						var temp = minCostValue;
						minCostValue = maxCostValue;
						maxCostValue = temp;
						
						_minCost = minCostValue.Value.ToString();
						_maxCost = maxCostValue.Value.ToString();
						
						OnPropertyChanged(nameof(MinCost));
						OnPropertyChanged(nameof(MaxCost));
					}
				}

				string typeFilter = SelectedType?.Content?.ToString();
				string durationFilter = SelectedDuration?.Content?.ToString();

				System.Diagnostics.Debug.WriteLine($"Выбранный тип: {typeFilter}, выбранная длительность: {durationFilter}");

				string typeFilterDb = GetDatabaseValue(typeFilter, _typeTranslations);
				string durationFilterDb = GetDatabaseValue(durationFilter, _durationTranslations);

				System.Diagnostics.Debug.WriteLine($"Значение для поиска в БД - тип: {typeFilterDb}, длительность: {durationFilterDb}");

				var filtered = _allSubscriptions.AsQueryable();

				if (!string.IsNullOrEmpty(SearchText))
				{
					System.Diagnostics.Debug.WriteLine($"Применяем фильтр по тексту: '{SearchText}'");
					filtered = filtered.Where(s => 
						s.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
						(s.Description != null && s.Description.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
					);
				}

				if (minCostValue.HasValue)
				{
					System.Diagnostics.Debug.WriteLine($"Применяем фильтр по минимальной цене: {minCostValue.Value}");
					filtered = filtered.Where(s => s.Price >= minCostValue.Value);
				}

				if (maxCostValue.HasValue)
				{
					System.Diagnostics.Debug.WriteLine($"Применяем фильтр по максимальной цене: {maxCostValue.Value}");
					filtered = filtered.Where(s => s.Price <= maxCostValue.Value);
				}

				if (!string.IsNullOrEmpty(typeFilterDb))
				{
					System.Diagnostics.Debug.WriteLine($"Применяем фильтр по типу абонемента: '{typeFilterDb}'");
					
					var allTypes = _allSubscriptions.Select(s => s.SubscriptionType).Distinct().ToList();
					System.Diagnostics.Debug.WriteLine($"Доступные типы абонементов: {string.Join(", ", allTypes)}");
					
					filtered = filtered.Where(s => s.SubscriptionType == typeFilterDb);
				}

				if (!string.IsNullOrEmpty(durationFilterDb))
				{
					System.Diagnostics.Debug.WriteLine($"Применяем фильтр по длительности абонемента: '{durationFilterDb}'");
					
					var allDurations = _allSubscriptions.Select(s => s.Duration).Distinct().ToList();
					System.Diagnostics.Debug.WriteLine($"Доступные длительности абонементов: {string.Join(", ", allDurations)}");
					
					if (IsYearDuration(durationFilterDb))
					{
						filtered = filtered.Where(s => IsYearDuration(s.Duration));
						System.Diagnostics.Debug.WriteLine($"Применен фильтр по годовой длительности");
					}
					else
					{
						filtered = filtered.Where(s => s.Duration == durationFilterDb);
					}
				}

				FilteredSubscriptions = new ObservableCollection<Subscription>(filtered);
				System.Diagnostics.Debug.WriteLine($"Отфильтровано {FilteredSubscriptions.Count} абонементов");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Ошибка при применении фильтров: {ex.Message}");
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
		}
		
		public void ResetFilters()
		{
			_searchText = string.Empty;
			_minCost = string.Empty;
			_maxCost = string.Empty;
			
			_selectedType = null;
			_selectedDuration = null;
			_isFiltersApplied = false;
			
			OnPropertyChanged(nameof(SearchText));
			OnPropertyChanged(nameof(MinCost));
			OnPropertyChanged(nameof(MaxCost));
			OnPropertyChanged(nameof(SelectedType));
			OnPropertyChanged(nameof(SelectedDuration));
			
			FilteredSubscriptions = new ObservableCollection<Subscription>(_allSubscriptions);
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