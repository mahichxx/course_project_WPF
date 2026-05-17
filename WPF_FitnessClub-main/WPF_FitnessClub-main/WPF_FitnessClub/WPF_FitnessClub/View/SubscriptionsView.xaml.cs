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
        
        private void PriceTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]|[.,]$");
            bool isMatch = regex.IsMatch(e.Text);

            if (!isMatch)
            {
                e.Handled = true;
                return;
            }

            if (e.Text == "." || e.Text == ",")
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null && (textBox.Text.Contains(".") || textBox.Text.Contains(",")))
                {
                    e.Handled = true;
                    return;
                }

                if (textBox != null && string.IsNullOrEmpty(textBox.Text))
                {
                    textBox.Text = "0";
                    textBox.CaretIndex = 1;
                }
            }
        }

        private void PriceTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete || e.Key == Key.Back || e.Key == Key.Left || 
                e.Key == Key.Right || e.Key == Key.Tab)
            {
                return;
            }

            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    string clipboardText = System.Windows.Clipboard.GetText();
                    Regex regex = new Regex(@"^[0-9]*([.,][0-9]*)?$");
                    if (!regex.IsMatch(clipboardText))
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        private void PriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null || string.IsNullOrEmpty(textBox.Text))
                return;

            int caretIndex = textBox.CaretIndex;
            bool textWasChanged = false;

            if (textBox.Text.Contains(","))
            {
                textBox.TextChanged -= PriceTextBox_TextChanged;
                
                textBox.Text = textBox.Text.Replace(",", ".");
                textWasChanged = true;
                
                if (caretIndex > textBox.Text.IndexOf('.'))
                    caretIndex = Math.Min(caretIndex, textBox.Text.Length);
                
                textBox.TextChanged += PriceTextBox_TextChanged;
            }

            try
            {
                if (textBox.Text != "." && textBox.Text != "0.")
                {
                    decimal value = Convert.ToDecimal(textBox.Text);
                    if (value < 0)
                    {
                        if (!textWasChanged)
                            textBox.TextChanged -= PriceTextBox_TextChanged;
                        
                        textBox.Text = "0";
                        
                        if (!textWasChanged)
                            textBox.TextChanged += PriceTextBox_TextChanged;
                        
                        caretIndex = textBox.Text.Length;
                        textWasChanged = true;
                    }
                }
            }
            catch
            {

            }

            if (textWasChanged)
                textBox.CaretIndex = caretIndex;
                
            BindingExpression binding = textBox.GetBindingExpression(TextBox.TextProperty);
            if (binding != null)
                binding.UpdateSource();
        }
    }
} 