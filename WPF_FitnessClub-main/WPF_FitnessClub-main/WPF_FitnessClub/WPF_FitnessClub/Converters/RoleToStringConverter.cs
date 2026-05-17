using System;
using System.Globalization;
using System.Windows.Data;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Converters
{
    public class RoleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UserRole role)
            {
                switch (role)
                {
                    case UserRole.Admin:
                        return "Администратор";
                    case UserRole.Client:
                        return "Клиент";
                    case UserRole.Coach:
                        return "Тренер";
                    default:
                        return "Неизвестная роль";
                }
            }

            return "Неизвестная роль";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string roleStr)
            {
                switch (roleStr)
                {
                    case "Администратор":
                        return UserRole.Admin;
                    case "Клиент":
                        return UserRole.Client;
                    case "Тренер":
                        return UserRole.Coach;
                    default:
                        return UserRole.Client;
                }
            }

            return UserRole.Client;
        }
    }
} 