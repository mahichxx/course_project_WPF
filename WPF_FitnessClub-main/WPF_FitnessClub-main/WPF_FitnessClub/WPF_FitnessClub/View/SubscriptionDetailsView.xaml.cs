using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using WPF_FitnessClub.Models;
using WPF_FitnessClub.ViewModels;
using static System.Globalization.CultureInfo;
using System.Linq;
using System.Text.RegularExpressions;

namespace WPF_FitnessClub.View
{
	public partial class SubscriptionDetailsView : Window
	{
		private SubscriptionDetailsVM _viewModel;

		public SubscriptionDetailsView(MainWindow mainWindow, List<Subscription> subscriptions, Subscription subscription, UserRole role)
		{
			InitializeComponent();

			_viewModel = new SubscriptionDetailsVM(mainWindow, subscriptions, subscription, role);
			
			this.Title = string.Format((string)Application.Current.Resources["SubscriptionDetailsFormat"], subscription.Name);
			
			_viewModel.RequestClose += ViewModel_RequestClose;
			_viewModel.ReviewAdded += ViewModel_ReviewAdded;
			_viewModel.ReviewDeleted += ViewModel_ReviewDeleted;
			_viewModel.SubscriptionDeleted += ViewModel_SubscriptionDeleted;

			DataContext = _viewModel;
			
			ThemeManager.Instance.ThemeChanged += OnThemeChanged;
			
			LoadReviews();
		}

		private void ViewModel_RequestClose(object sender, EventArgs e)
		{
			this.Close();
		}

		private void ViewModel_ReviewAdded(object sender, Review e)
		{
			LoadReviews();
		}

		private void ViewModel_ReviewDeleted(object sender, Review e)
		{
			LoadReviews();
		}
		
		private void ViewModel_SubscriptionDeleted(object sender, Subscription e)
		{
			this.Close();
		}

		private void LoadReviews()
		{
			ReviewWrapPanel.Children.Clear();
			
			foreach (var review in _viewModel.Reviews)
			{
				Border reviewBorder = new Border
				{
					BorderBrush = new SolidColorBrush(Colors.Gray),
					BorderThickness = new Thickness(1),
					CornerRadius = new CornerRadius(5),
					Padding = new Thickness(10),
					Margin = new Thickness(5),
					MinWidth = 200
				};

				StackPanel reviewPanel = new StackPanel();

				TextBlock userNameBlock = new TextBlock
				{
					Text = review.User,
					FontWeight = FontWeights.Bold,
					Margin = new Thickness(0, 0, 0, 5),
					Foreground = (SolidColorBrush)Application.Current.Resources["TextBrush"]
				};
				reviewPanel.Children.Add(userNameBlock);

				StackPanel ratingPanel = new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(0, 0, 0, 5)
				};

				TextBlock ratingText = new TextBlock
				{
					Text = review.Score.ToString(),
					Margin = new Thickness(0, 0, 5, 0),
					Foreground = (SolidColorBrush)Application.Current.Resources["TextBrush"]
				};
				ratingPanel.Children.Add(ratingText);

				for (int i = 1; i <= 5; i++)
				{
					TextBlock star = new TextBlock
					{
						FontSize = 20,
						Margin = new Thickness(0),
						Padding = new Thickness(0)
					};

					if (i <= review.Score)
					{
						star.Text = "★";
						star.Foreground = new SolidColorBrush(Colors.Gold);
					}
					else
					{
						star.Text = "☆";
						star.Foreground = new SolidColorBrush(Colors.Gray);
					}

					ratingPanel.Children.Add(star);
				}

				reviewPanel.Children.Add(ratingPanel);

				TextBlock commentBlock = new TextBlock
				{
					Text = review.Comment,
					TextWrapping = TextWrapping.Wrap,
					Foreground = (SolidColorBrush)Application.Current.Resources["TextBrush"]
				};
				reviewPanel.Children.Add(commentBlock);

				if (_viewModel.DeleteReviewVisible == Visibility.Visible)
				{
					Button deleteButton = new Button
					{
						Content = (string)Application.Current.Resources["DeleteReviewButton"],
						Margin = new Thickness(0, 10, 0, 0),
						Tag = review.Id,
						CommandParameter = review.Id,
						Style = (Style)Application.Current.Resources["DefaultButtonStyle"]
					};
					deleteButton.Command = _viewModel.DeleteReviewCommand;
					reviewPanel.Children.Add(deleteButton);
				}

				reviewBorder.Child = reviewPanel;
				ReviewWrapPanel.Children.Add(reviewBorder);
			}
		}

		private void Radio_Checked(object sender, RoutedEventArgs e)
		{
			if (sender is RadioButton radioButton && int.TryParse(radioButton.Content.ToString(), out int rating))
			{
				_viewModel.ReviewRating = rating;
			}
		}

		private void PriceTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
		}

		private void OnThemeChanged(object sender, ThemeManager.AppTheme e)
		{
			LoadReviews();
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			ThemeManager.Instance.ThemeChanged -= OnThemeChanged;
		}

		public void UpdateSubscriptions(List<Subscription> updatedSubscriptions)
		{
			int currentId = _viewModel.CurrentSubscriptionId;
			
			Subscription updatedSubscription = updatedSubscriptions.FirstOrDefault(s => s.Id == currentId);
			
			if (updatedSubscription != null)
			{
				_viewModel.UpdateSubscriptionDetails(updatedSubscription, updatedSubscriptions);
				
				LoadReviews();
			}
		}
	}
} 