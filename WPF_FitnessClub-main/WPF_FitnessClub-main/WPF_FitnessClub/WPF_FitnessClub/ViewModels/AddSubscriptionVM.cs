using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPF_FitnessClub.Models;
using static WPF_FitnessClub.Commands;
using WPF_FitnessClub.Data;
using WPF_FitnessClub.Data.Services;
using Microsoft.Win32;
using System.IO;

namespace WPF_FitnessClub.ViewModels
{
	public class AddSubscriptionVM : ViewModelBase
	{
		private string _name;
		private string _description;
		private string _price;
		private string _imagePath;
		private string _duration;
		private string _subscriptionType;
		private SubscriptionService _subscriptionService;

		#region Свойства для привязки данных
		public string Name
		{
			get { return _name; }
			set
			{
				_name = value;
				OnPropertyChanged(nameof(Name));
				CommandManager.InvalidateRequerySuggested();
			}
		}

		public string Description
		{
			get { return _description; }
			set
			{
				_description = value;
				OnPropertyChanged(nameof(Description));
			}
		}

		public string Price
		{
			get { return _price; }
			set
			{
				_price = value;
				OnPropertyChanged(nameof(Price));
				CommandManager.InvalidateRequerySuggested();
			}
		}

		public string ImagePath
		{
			get { return _imagePath; }
			set
			{
				_imagePath = value;
				OnPropertyChanged(nameof(ImagePath));
				CommandManager.InvalidateRequerySuggested();
			}
		}

		public string Duration
		{
			get { return _duration; }
			set
			{
				_duration = value;
				OnPropertyChanged(nameof(Duration));
				CommandManager.InvalidateRequerySuggested();
			}
		}

		public string SubscriptionType
		{
			get { return _subscriptionType; }
			set
			{
				_subscriptionType = value;
				OnPropertyChanged(nameof(SubscriptionType));
				CommandManager.InvalidateRequerySuggested();
			}
		}

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        #endregion

        #region Команды
        public ICommand SaveCommand { get; private set; }
		public ICommand CancelCommand { get; private set; }
		public ICommand SelectImageCommand { get; private set; }

		#endregion

		public Subscription NewSubscription { get; private set; }

		public event Action<bool, Subscription> CloseRequested;

		public AddSubscriptionVM()
		{
			_subscriptionService = new SubscriptionService();

			SaveCommand = new RelayCommand(ExecuteSaveCommand, CanExecuteSaveCommand);
			CancelCommand = new RelayCommand(ExecuteCancelCommand);
			SelectImageCommand = new RelayCommand(ExecuteSelectImageCommand);
		}

		public bool CanExecuteSaveCommand(object parameter)
		{
			return true;     
		}

        public void ExecuteSaveCommand(object parameter)
        {
            try
            {
                IsLoading = true;
                List<string> validationErrors = new List<string>();

                // 1. Валидация названия (раскомментировал и поправил)
                if (string.IsNullOrWhiteSpace(Name))
                {
                    validationErrors.Add("Введите название абонемента");
                }

                // 2. Валидация цены
                decimal priceValue = 0;
                string normalizedPrice = Price?.Replace(',', '.');
                if (string.IsNullOrEmpty(Price?.Trim()) || !decimal.TryParse(normalizedPrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out priceValue) || priceValue < 0)
                    validationErrors.Add("Некорректная цена");

                // 3. Валидация описания и картинки
                if (string.IsNullOrEmpty(Description?.Trim())) validationErrors.Add("Введите описание");
                if (string.IsNullOrEmpty(ImagePath?.Trim())) validationErrors.Add("Выберите изображение");

                // 4. ОЧИСТКА ВЫБОРА (Тип и Длительность)
                // Используем универсальный метод получения текста из ComboBoxItem
                string GetValue(string input) => input?.Replace("System.Windows.Controls.ComboBoxItem: ", "").Trim();

                string finalType = GetValue(SubscriptionType);
                string finalDuration = GetValue(Duration);

                if (string.IsNullOrEmpty(finalDuration) || finalDuration == "Все")
                    validationErrors.Add("Выберите длительность");
                if (string.IsNullOrEmpty(finalType) || finalType == "Все")
                    validationErrors.Add("Выберите тип подписки");

                // 5. Вывод ошибок
                if (validationErrors.Count > 0)
                {
                    MessageBox.Show("Форма содержит ошибки:\n- " + string.Join("\n- ", validationErrors),
                                    "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsLoading = false;
                    return;
                }

                // 6. Создание объекта
                NewSubscription = new Subscription
                {
                    Name = Name.Trim(),
                    Price = priceValue,
                    Description = Description.Trim(),
                    ImagePath = ImagePath,
                    Duration = finalDuration,
                    SubscriptionType = finalType,
                    Reviews = new List<Review>()
                };

                // 7. Сохранение в БД
                int subscriptionId = _subscriptionService.Add(NewSubscription);

                if (subscriptionId > 0)
                {
                    NewSubscription.Id = subscriptionId;
                    MessageBox.Show("Абонемент успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    CloseRequested?.Invoke(true, NewSubscription);
                }
                else
                {
                    MessageBox.Show("Ошибка сохранения в базу данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        private void ExecuteCancelCommand(object obj)
		{
			CloseRequested?.Invoke(false, null);
		}

        private void ExecuteSelectImageCommand(object obj)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Картинки (*.jpg;*.png)|*.jpg;*.png";

            if (openFileDialog.ShowDialog() == true)
            {
                // Берем имя файла (например, "yoga.jpg"), а не весь путь к диску C:
                string fileName = System.IO.Path.GetFileName(openFileDialog.FileName);

                // Сохраняем только имя. Конвертер сам подставит папку Images/
                ImagePath = fileName;

                System.Diagnostics.Debug.WriteLine($"Выбрано имя файла: {fileName}");
            }
        }

    }
}
