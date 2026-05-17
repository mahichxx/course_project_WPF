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
				List<string> validationErrors = new List<string>();
				
				string namePattern = @"^[a-zA-Zа-яА-Я\s]+$";
				if (string.IsNullOrEmpty(Name?.Trim()))
				{
					validationErrors.Add((string)Application.Current.Resources["NameRequired"]);
				}
				else if (!Regex.IsMatch(Name, namePattern))
				{
					validationErrors.Add((string)Application.Current.Resources["InvalidName"]);
				}

				decimal priceValue = 0;
				if (string.IsNullOrEmpty(Price?.Trim()))
				{
					validationErrors.Add((string)Application.Current.Resources["PriceRequired"]);
				}
				else if (!decimal.TryParse(Price, out priceValue) || priceValue < 0)
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

				if (string.IsNullOrEmpty(Duration?.Trim()))
				{
					validationErrors.Add((string)Application.Current.Resources["EmptyDuration"]);
				}
				
				if (string.IsNullOrEmpty(SubscriptionType?.Trim()))
				{
					validationErrors.Add((string)Application.Current.Resources["EmptySubscriptionType"]);
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
					return; 
				}

				NewSubscription = new Subscription
				{
					Name = Name.Trim(),
					Price = priceValue,
					Description = Description.Trim(),
					ImagePath = ImagePath,
					Duration = Duration,
					SubscriptionType = SubscriptionType,
					Reviews = new List<Review>()
				};

				int subscriptionId = _subscriptionService.Add(NewSubscription);
				
				if (subscriptionId > 0)
				{
					NewSubscription.Id = subscriptionId;
					
					MessageBox.Show(
						(string)Application.Current.Resources["SubscriptionAddedSuccess"],
						(string)Application.Current.Resources["SuccessTitle"],
						MessageBoxButton.OK,
						MessageBoxImage.Information);

					CloseRequested?.Invoke(true, NewSubscription);
				}
				else
				{
					MessageBox.Show(
						(string)Application.Current.Resources["ErrorSavingSubscription"],
						(string)Application.Current.Resources["ErrorTitle"],
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					$"{(string)Application.Current.Resources["ErrorSavingSubscription"]}: {ex.Message}",
					(string)Application.Current.Resources["ErrorTitle"],
					MessageBoxButton.OK,
					MessageBoxImage.Error);
			}
		}

		private void ExecuteCancelCommand(object obj)
		{
			CloseRequested?.Invoke(false, null);
		}

		private void ExecuteSelectImageCommand(object obj)
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
				}
				else
				{
					ImagePath = selectedPath;
				}
			}
		}

	}
}
