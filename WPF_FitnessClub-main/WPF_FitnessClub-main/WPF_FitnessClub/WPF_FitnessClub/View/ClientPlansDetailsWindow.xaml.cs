using System.Windows;
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
            if (DataContext is ClientPlansDetailsViewModel viewModel)
            {
                viewModel.LoadClientPlans();
            }
        }
    }
} 