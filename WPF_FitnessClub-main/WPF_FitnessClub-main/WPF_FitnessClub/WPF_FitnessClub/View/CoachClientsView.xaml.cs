using System;
using System.Windows;
using System.Windows.Controls;
using WPF_FitnessClub.Models;
using WPF_FitnessClub.ViewModels;

namespace WPF_FitnessClub.View
{
    public partial class CoachClientsView : Window
    {
        private CoachClientsViewModel _viewModel;

        public CoachClientsView()
        {
            InitializeComponent();
        }

        public CoachClientsView(User coach)
        {
            InitializeComponent();
            _viewModel = new CoachClientsViewModel(coach);
            DataContext = _viewModel;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel?.LoadClients();
                
                if (_viewModel?.SelectedTabIndex == 1)
                {
                    _viewModel.LoadAvailableClients();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format((string)Application.Current.FindResource("ErrorRefreshingLists"), ex.Message),
                    (string)Application.Current.FindResource("ErrorTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ShowClientPlansButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is User client)
            {
                try
                {
                    var clientPlansWindow = new ClientPlansDetailsWindow(client);
                    clientPlansWindow.Show();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(
                        string.Format((string)Application.Current.FindResource("ErrorOpeningClientPlans"), ex.Message),
                        (string)Application.Current.FindResource("ErrorTitle"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void AddClientButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
            {
                MessageBox.Show(
                    (string)Application.Current.FindResource("ViewModelNotInitialized"),
                    (string)Application.Current.FindResource("ErrorTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (sender is Button button)
            {
                if (button.CommandParameter is User client)
                {
                    try 
                    {
                        _viewModel.AddClientToCoach(client);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            string.Format((string)Application.Current.FindResource("ErrorAddingClient"), ex.Message),
                            (string)Application.Current.FindResource("ErrorTitle"),
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else 
                {
                    MessageBox.Show(
                        (string)Application.Current.FindResource("ErrorGettingClientData"),
                        (string)Application.Current.FindResource("ErrorTitle"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RemoveClientButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
            {
                MessageBox.Show(
                    (string)Application.Current.FindResource("ViewModelNotInitialized"),
                    (string)Application.Current.FindResource("ErrorTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            if (sender is Button button && button.CommandParameter is User client)
            {
                var result = MessageBox.Show(
                    string.Format((string)Application.Current.FindResource("ConfirmRemoveClient"), client.FullName), 
                    (string)Application.Current.FindResource("ConfirmTitle"), 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _viewModel.RemoveClientFromCoach(client);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            string.Format((string)Application.Current.FindResource("ErrorRemovingClient"), ex.Message),
                            (string)Application.Current.FindResource("ErrorTitle"),
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show(
                    (string)Application.Current.FindResource("FailedToGetClientData"),
                    (string)Application.Current.FindResource("ErrorTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 