using System;
using System.Globalization;
using System.Windows.Data;

namespace WPF_FitnessClub.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public string TrueValue { get; set; } = "Да";

        public string FalseValue { get; set; } = "Нет";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCompleted)
            {
                return isCompleted ? "Выполнено" : "Не выполнено";
            }
            return "Не выполнено";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 