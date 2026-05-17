using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WPF_FitnessClub.Converters
{
    public class PlanStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            
            string status = value.ToString();
            
            switch (status.ToLower())
            {
                case "выполнен":
                    return Application.Current.Resources["PlanStatusCompleted"];
                case "в процессе":
                    return Application.Current.Resources["PlanStatusInProgress"];
                case "активен":
                    return Application.Current.Resources["PlanStatusActive"];
                case "истек":
                    return Application.Current.Resources["PlanStatusExpired"];
                case "отменен":
                    return Application.Current.Resources["PlanStatusCancelled"];
                case "в ожидании":
                    return Application.Current.Resources["PlanStatusPending"];
                default:
                    return status;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 