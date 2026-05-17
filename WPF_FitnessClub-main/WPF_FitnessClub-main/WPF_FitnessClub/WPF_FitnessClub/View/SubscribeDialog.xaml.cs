using System;
using System.Windows;
using WPF_FitnessClub.Models;
using WPF_FitnessClub.Repositories;
using WPF_FitnessClub.Data.Services;
using WPF_FitnessClub.ViewModels;

namespace WPF_FitnessClub.View
{
    public partial class SubscribeDialog : Window
    {
        private SubscribeDialogViewModel _viewModel;
        private UserSubscriptionRepository _repository;
        private UserService _userService;

        public UserSubscription Result { get; private set; }

        public SubscribeDialog(Subscription subscription)
        {
            InitializeComponent();
            _repository = new UserSubscriptionRepository();
            _userService = new UserService();

            User currentUser = _userService.GetCurrentUser();
            _viewModel = new SubscribeDialogViewModel(subscription, currentUser);
            DataContext = _viewModel;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_viewModel.IsDataValid)
                {
                    MessageBox.Show(
                        (string)Application.Current.FindResource("ValidationError"), 
                        (string)Application.Current.FindResource("ValidationErrorTitle"), 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                    return;
                }

                User currentUser = _userService.GetCurrentUser();
                if (currentUser == null)
                {
                    MessageBox.Show(
                        (string)Application.Current.FindResource("AuthorizationRequired"), 
                        (string)Application.Current.FindResource("AuthorizationErrorTitle"), 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                    return;
                }

                if (_repository.HasActiveSubscription(currentUser.Id, _viewModel.SubscriptionId))
                {
                    MessageBox.Show(
                        (string)Application.Current.FindResource("ActiveSubscriptionExists"), 
                        (string)Application.Current.FindResource("InfoTitle"), 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Information);
                    return;
                }

                DateTime purchaseDate = DateTime.Now;
                DateTime expiryDate = CalculateEndDate(purchaseDate, _viewModel.SubscriptionDuration);

                var userSubscription = new UserSubscription
                {
                    UserId = currentUser.Id,
                    SubscriptionId = _viewModel.SubscriptionId,
                    PurchaseDate = purchaseDate,
                    ExpiryDate = expiryDate
                };

                Result = _repository.Add(userSubscription);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format((string)Application.Current.FindResource("SubscriptionErrorTitle"), ex.Message), 
                    (string)Application.Current.FindResource("ErrorTitle"), 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private DateTime CalculateEndDate(DateTime startDate, string duration)
        {
            try
            {
                string[] parts = duration.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    return startDate.AddMonths(1);

                int value = int.Parse(parts[0]);
                string unit = parts[1].ToLower();

                if (unit.Contains("день") || unit.Contains("дн"))
                    return startDate.AddDays(value);
                else if (unit.Contains("неде") || unit.Contains("недел"))
                    return startDate.AddDays(value * 7);
                else if (unit.Contains("месяц") || unit.Contains("мес"))
                    return startDate.AddMonths(value);
                else if (unit.Contains("год") || unit.Contains("лет"))
                    return startDate.AddYears(value);
                else
                    return startDate.AddMonths(1);
            }
            catch
            {
                return startDate.AddMonths(1);
            }
        }
    }
} 