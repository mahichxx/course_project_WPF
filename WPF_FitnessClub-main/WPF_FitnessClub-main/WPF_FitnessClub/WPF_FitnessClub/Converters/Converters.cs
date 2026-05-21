using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WPF_FitnessClub.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using System.IO;                 
using System.Windows.Media.Imaging; 
using System.Globalization;     

namespace WPF_FitnessClub.Converters
{
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string fileName = value as string;
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            try
            {
                // Убираем возможные приставки, оставляем только чистое имя файла (например, gym2.jpg)
                string cleanName = Path.GetFileName(fileName);

                // Собираем путь к папке с картинками внутри твоего проекта
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", cleanName);

                if (File.Exists(fullPath))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(fullPath);
                    image.CacheOption = BitmapCacheOption.OnLoad; // Чтобы картинка не "зависала" в памяти
                    image.EndInit();
                    return image;
                }

                // Если файла на диске нет, пробуем запасной путь (через ресурсы сборки)
                return new BitmapImage(new Uri($"pack://application:,,,/Images/{cleanName}", UriKind.Absolute));
            }
            catch
            {
                // Если картинки нет, вернем null (карточка просто будет без фото, но программа не вылетит)
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class RatingToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double rating && parameter != null)
            {
                int ratingToCheck;
                if (int.TryParse(parameter.ToString(), out ratingToCheck))
                {
                    if (rating >= ratingToCheck)
                    {
                        return Visibility.Visible;
                    }
                }
                else
                {
                    if (rating >= 1)
                    {
                        return Visibility.Visible;
                    }
                }
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ZeroRatingToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double rating && parameter != null)
            {
                int ratingToCheck;
                if (int.TryParse(parameter.ToString(), out ratingToCheck))
                {
                    if (rating < ratingToCheck)
                    {
                        return Visibility.Visible;
                    }
                }
                else
                {
                    if (rating < 1)
                    {
                        return Visibility.Visible;
                    }
                }
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class UserRoleConverter : IValueConverter
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

    public class NullToInvertedVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class RatingToStarsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double rating = 0;
            if (value != null) rating = System.Convert.ToDouble(value);

            var stars = new List<Star>();
            for (int i = 1; i <= 5; i++)
            {
                if (rating >= i) stars.Add(new Star { Type = StarType.Full });
                else if (rating >= i - 0.5) stars.Add(new Star { Type = StarType.Half });
                else stars.Add(new Star { Type = StarType.Empty });
            }
            return stars;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Star
    {
        public StarType Type { get; set; } // Свойство должно называться именно Type
    }

    public enum StarType { Full, Half, Empty }
} 