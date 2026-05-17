using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.NetworkInformation;
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
using static WPF_FitnessClub.Commands;
using WPF_FitnessClub.ViewModels;

namespace WPF_FitnessClub.View
{
	public partial class AddSubscriptionView : UserControl
	{
		private readonly AddSubscriptionVM _viewModel;

		public event Action<bool, Subscription> CloseRequested;

		public AddSubscriptionView()
		{
			InitializeComponent();
			_viewModel = new AddSubscriptionVM();
			_viewModel.CloseRequested += OnViewModelCloseRequested;
			DataContext = _viewModel;
		}

		private void OnViewModelCloseRequested(bool success, Subscription subscription)
		{
			CloseRequested?.Invoke(success, subscription);
		}
		
		private void UserControl_Unloaded(object sender, RoutedEventArgs e)
		{
			if (_viewModel != null)
			{
				_viewModel.CloseRequested -= OnViewModelCloseRequested;
			}
		}
	}
}

