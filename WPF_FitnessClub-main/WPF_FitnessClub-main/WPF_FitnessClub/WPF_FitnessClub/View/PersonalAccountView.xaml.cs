using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using WPF_FitnessClub.Data.Services.Interfaces;
using WPF_FitnessClub.Data.Services;
using WPF_FitnessClub.Models;
using WPF_FitnessClub.ViewModels;

namespace WPF_FitnessClub.View
{
	public partial class PersonalAccountView : UserControl
	{
		private PersonalAccountVM _viewModel;
		private User _user;
		private const int MaxPasswordLength = 30;

		public PersonalAccountView(User user)
		{
			InitializeComponent();
			
			var workoutPlanService = new WorkoutPlanService();
			var nutritionPlanService = new NutritionPlanService();
			
			_viewModel = new PersonalAccountVM(user, workoutPlanService, nutritionPlanService);
			_viewModel.LanguageChanged += OnLanguageChanged;
			DataContext = _viewModel;
			
			SecurityCurrentPasswordInput.TextChanged += CurrentPasswordBox_PasswordChanged;
			SecurityNewPasswordInput.PasswordChanged += NewPasswordBox_PasswordChanged;
			SecurityConfirmPasswordInput.PasswordChanged += ConfirmPasswordBox_PasswordChanged;

			_user = user;

			ThemeManager.Instance.PropertyChanged += ThemeManager_PropertyChanged;
			
			this.Unloaded += PersonalAccountView_Unloaded;

			string currentLanguage = LanguageManager.Instance.CurrentLanguage;
			if (currentLanguage.StartsWith("ru"))
			{
				LanguageSelectionComboBox.SelectedIndex = 0;  
			}
			else
			{
				LanguageSelectionComboBox.SelectedIndex = 1;  
			}

			if (ThemeManager.Instance.CurrentTheme == ThemeManager.AppTheme.Light)
			{
				ThemeSelectionComboBox.SelectedIndex = 0;   
			}
			else
			{
				ThemeSelectionComboBox.SelectedIndex = 1;   
			}
		}

		private void EmailTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			if (sender is TextBox textBox)
			{
				string fullText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

				if (fullText.Contains("@"))
				{
					int atIndex = fullText.IndexOf('@');
					
					if (textBox.CaretIndex > atIndex)
					{
						if (!Regex.IsMatch(e.Text, @"^[a-zA-Z.\-]$"))
						{
							e.Handled = true;   
							return;
						}
						
						string domainPart = fullText.Substring(atIndex + 1) + e.Text;
						
						if (domainPart.Contains(".."))
						{
							e.Handled = true;
							return;
						}
					}
					else
					{
						if (!Regex.IsMatch(e.Text, @"^[a-zA-Z0-9._\-]$"))
						{
							e.Handled = true;   
							return;
						}
						
						string localPart = fullText.Substring(0, atIndex);
						if (localPart.Contains(".."))
						{
							e.Handled = true;
							return;
						}
					}
				}
				else
				{
					if (!Regex.IsMatch(e.Text, @"^[a-zA-Z0-9._\-@]$"))
					{
						e.Handled = true;   
						return;
					}
					
					if (fullText.Contains(".."))
					{
						e.Handled = true;
						return;
					}
				}
			}
		}

		private void EmailTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			if (_viewModel != null && sender is TextBox textBox)
			{
				_viewModel.ValidateEmail(textBox.Text);
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

		private void OnLanguageChanged(object sender, string language)
		{
		}
		

		private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			if (_viewModel != null)
				_viewModel.CurrentPassword = SecurityCurrentPasswordInput.Text;
		}

		private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			if (_viewModel != null)
				_viewModel.NewPassword = SecurityNewPasswordInput.Password;
		}

		private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			if (_viewModel != null)
				_viewModel.ConfirmPassword = SecurityConfirmPasswordInput.Password;
		}

		private void LanguageSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (LanguageSelectionComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
			{
				string languageCode = selectedItem.Tag.ToString();
				LanguageManager.Instance.ChangeLanguage(languageCode);
				
				Thread.CurrentThread.CurrentCulture = new CultureInfo(languageCode);
				CultureInfo customCulture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
				customCulture.NumberFormat.CurrencySymbol = "Br";
				customCulture.NumberFormat.CurrencyDecimalSeparator = ",";
				customCulture.NumberFormat.CurrencyGroupSeparator = " ";
				Thread.CurrentThread.CurrentCulture = customCulture;
			}
		}

		private void ThemeSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ThemeSelectionComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
			{
				string themeCode = selectedItem.Tag.ToString();
				ThemeManager.Instance.ChangeTheme(themeCode == "Light" ? ThemeManager.AppTheme.Light : ThemeManager.AppTheme.Dark);
			}
		}

		private void ClearPasswordFields_Click(object sender, RoutedEventArgs e)
		{
			SecurityCurrentPasswordInput.Clear();
			SecurityNewPasswordInput.Clear();
			SecurityConfirmPasswordInput.Clear();
		}

		private void ThemeManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "CurrentThemeString")
			{
				if (ThemeManager.Instance.CurrentTheme == ThemeManager.AppTheme.Light)
				{
					ThemeSelectionComboBox.SelectedIndex = 0;   
				}
				else
				{
					ThemeSelectionComboBox.SelectedIndex = 1;   
				}
			}
		}
		
		private void PersonalAccountView_Unloaded(object sender, RoutedEventArgs e)
		{
			Unload();
		}

		public void Unload()
		{
			ThemeManager.Instance.PropertyChanged -= ThemeManager_PropertyChanged;
			_viewModel.LanguageChanged -= OnLanguageChanged;
		}
	}
}
