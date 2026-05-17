using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WPF_FitnessClub.Converters
{
    public class OrVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = false;

            if (value is Visibility visibility1)
            {
                isVisible = visibility1 == Visibility.Visible;
            }

            if (!isVisible && parameter is Visibility visibility2)
            {
                isVisible = visibility2 == Visibility.Visible;
            }

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 