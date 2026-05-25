using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using WPF_FitnessClub.Models;
using WPF_FitnessClub.ViewModels;

namespace WPF_FitnessClub.View
{
    public partial class SubscriptionsView : UserControl
    {
        public SubscriptionsVM _viewModel;

        public event Action<Subscription> SubscriptionSelected;

        public SubscriptionsView(MainWindow mainWindow, List<Subscription> subscriptions)
        {
            InitializeComponent();
            
            _viewModel = new SubscriptionsVM(mainWindow, subscriptions);
            _viewModel.SubscriptionSelected += OnSubscriptionSelected;
            
            DataContext = _viewModel;
        }

        private void OnSubscriptionSelected(Subscription subscription)
        {
            SubscriptionSelected?.Invoke(subscription);
        }

        public void UpdateSubscriptions(List<Subscription> subscriptions)
        {
            _viewModel.UpdateSubscriptions(subscriptions);
        }
     
        public void UpdateSubscriptions(List<Subscription> subscriptions, bool resetFilters)
        {
            if (resetFilters)
            {
                _viewModel.ResetFilters();
            }
            
            _viewModel.UpdateSubscriptions(subscriptions);
        }

        private void Subscription_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Subscription subscription)
            {
                _viewModel.SelectSubscriptionCommand.Execute(subscription);
            }
        }

        // 1. Запрет ввода любых символов кроме цифр, точки и запятой + ограничение 2 знаков
        private void PriceTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            // Разрешаем только цифры, точку и запятую
            Regex regex = new Regex(@"^[0-9]|[.,]$");
            if (!regex.IsMatch(e.Text))
            {
                e.Handled = true;
                return;
            }

            string currentText = textBox.Text;

            // Обработка разделителей
            if (e.Text == "." || e.Text == ",")
            {
                // Запрет второго разделителя
                if (currentText.Contains(".") || currentText.Contains(","))
                {
                    e.Handled = true;
                    return;
                }

                // Если вводят разделитель первым — преобразуем в "0."
                if (string.IsNullOrEmpty(currentText))
                {
                    textBox.Text = "0";
                    textBox.CaretIndex = 1;
                }
            }

            // Ограничение: максимум 2 цифры после точки
            int delimiterIndex = currentText.IndexOf('.') != -1 ? currentText.IndexOf('.') : currentText.IndexOf(',');
            if (delimiterIndex != -1 && textBox.CaretIndex > delimiterIndex)
            {
                string decimals = currentText.Substring(delimiterIndex + 1);
                if (decimals.Length >= 2)
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        // 2. Запрет пробела
        private void PriceTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        // 3. Автоматическая замена запятой на точку
        private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            if (textBox.Text.Contains(","))
            {
                int cursor = textBox.CaretIndex;
                textBox.Text = textBox.Text.Replace(",", ".");
                textBox.CaretIndex = cursor;
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null)
            {
                element.Focus();
            }
        }
        private void PriceTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Вызываем финальную проверку диапазона, когда пользователь ушел из поля
            _viewModel.ValidatePriceRange();
        }
    }
} 