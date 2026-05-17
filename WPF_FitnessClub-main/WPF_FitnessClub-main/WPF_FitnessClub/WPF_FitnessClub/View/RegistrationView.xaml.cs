using Newtonsoft.Json;
using System;
using System.IO;
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
using System.Windows.Shapes;
using WPF_FitnessClub.Models;
using WPF_FitnessClub.ViewModels;
using System.Text.RegularExpressions;
using System.Globalization;

namespace WPF_FitnessClub.View
{
	public partial class RegistrationView : Window
	{
		private RegistrationVM _viewModel;
		private const int MaxPasswordLength = 30;

		public RegistrationView()
		{
			InitializeComponent();

			_viewModel = new RegistrationVM();
			DataContext = _viewModel;

			PasswordInput.PasswordChanged += PasswordInput_PasswordChanged;
			RegPasswordInput.PasswordChanged += RegPasswordInput_PasswordChanged;
			ConfirmPasswordInput.PasswordChanged += ConfirmPasswordInput_PasswordChanged;

			_viewModel.RequestClose += (s, e) => this.Close();
            
            LanguageManager.Instance.LanguageChanged += LanguageManager_LanguageChanged;
            
            UpdateLanguageButtonsAppearance();
		}
        
        private void LanguageManager_LanguageChanged(object sender, string cultureName)
        {
            UpdateLanguageButtonsAppearance();
        }
        
        private void UpdateLanguageButtonsAppearance()
        {
            string currentCulture = CultureInfo.CurrentUICulture.Name.ToLower();
            
            RussianButton.Background = new SolidColorBrush(Colors.Transparent);
            EnglishButton.Background = new SolidColorBrush(Colors.Transparent);
            
            if (currentCulture.StartsWith("ru"))
            {
                RussianButton.BorderBrush = new SolidColorBrush(Colors.White);
                RussianButton.BorderThickness = new Thickness(2);
                EnglishButton.BorderBrush = new SolidColorBrush(Colors.Transparent);
                EnglishButton.BorderThickness = new Thickness(1);
                
                RussianButton.Tag = "Active";
                EnglishButton.Tag = "Normal";
            }
            else
            {
                EnglishButton.BorderBrush = new SolidColorBrush(Colors.White);
                EnglishButton.BorderThickness = new Thickness(2);
                RussianButton.BorderBrush = new SolidColorBrush(Colors.Transparent);
                RussianButton.BorderThickness = new Thickness(1);
                
                EnglishButton.Tag = "Active";
                RussianButton.Tag = "Normal";
            }
        }

		private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
		{
			if (_viewModel != null)
			{
				_viewModel.Password = PasswordInput.Password;
			}
		}

		private void RegPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
		{
			if (_viewModel != null)
			{
				_viewModel.RegPassword = RegPasswordInput.Password;
			}
		}

		private void ConfirmPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
		{
			if (_viewModel != null)
			{
				_viewModel.ConfirmPassword = ConfirmPasswordInput.Password;
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

		private void RussianButton_Click(object sender, RoutedEventArgs e)
		{
			LanguageManager.Instance.ChangeLanguage("ru-RU");
		}

		private void EnglishButton_Click(object sender, RoutedEventArgs e)
		{
			LanguageManager.Instance.ChangeLanguage("en-US");
		}

		private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (MainTabControl.SelectedIndex == 0)
			{
				MainBorder.Height = 300;
			}
			else if (MainTabControl.SelectedIndex == 1)
			{
				MainBorder.Height = 420;
			}
		}
        
        protected override void OnClosed(EventArgs e)
        {
            LanguageManager.Instance.LanguageChanged -= LanguageManager_LanguageChanged;
            base.OnClosed(e);
        }
	}
}