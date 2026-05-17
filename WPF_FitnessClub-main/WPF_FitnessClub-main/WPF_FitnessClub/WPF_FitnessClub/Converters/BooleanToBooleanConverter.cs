using System;
using System.Globalization;
using System.Windows.Data;

namespace WPF_FitnessClub.Converters
{
    public class BooleanToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter is string paramStr && paramStr == "Inverse")
                {
                    return !boolValue;
                }
                return boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter is string paramStr && paramStr == "Inverse")
                {
                    return !boolValue;
                }
                return boolValue;
            }
            return false;
        }
    }
} 