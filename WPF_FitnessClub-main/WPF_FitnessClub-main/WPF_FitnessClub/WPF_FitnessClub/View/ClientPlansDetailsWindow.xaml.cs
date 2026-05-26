using System.Windows;
using System.Windows.Controls;
using WPF_FitnessClub.Models;
using WPF_FitnessClub.ViewModels;

namespace WPF_FitnessClub.View
{
    public partial class ClientPlansDetailsWindow : Window
    {
        private ClientPlansDetailsViewModel _viewModel;

        public ClientPlansDetailsWindow()
        {
            InitializeComponent();
        }

        public ClientPlansDetailsWindow(User client)
        {
            InitializeComponent();
            
            Title = $"Планы клиента: {client.FullName}";
            
            _viewModel = new ClientPlansDetailsViewModel(client);
            
            DataContext = _viewModel;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is ClientPlansDetailsViewModel vm)
            {
                vm.LoadClientPlans();
                MessageBox.Show("Данные успешно обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddClientButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is User client)
            {
                // Вызываем команду добавления из вашей ViewModel
                var viewModel = (ViewModels.CoachClientsViewModel)this.DataContext;
                viewModel.AddClientCommand.Execute(client);
            }
        }
    }
} 