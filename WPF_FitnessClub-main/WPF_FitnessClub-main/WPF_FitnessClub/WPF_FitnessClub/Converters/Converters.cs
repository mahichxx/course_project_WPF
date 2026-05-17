using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WPF_FitnessClub.Models;
using System.Collections.Generic;
using System.Linq;

namespace WPF_FitnessClub.Converters
{
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return "/Images/default-profile.jpg";

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
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
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double rating)
            {
                var stars = new List<Star>();
                
                int fullStars = (int)Math.Floor(rating);
                
                bool hasHalfStar = (rating - fullStars) >= 0.5;
                
                for (int i = 0; i < fullStars; i++)
                {
                    stars.Add(new Star { Type = StarType.Full });
                }
                
                if (hasHalfStar)
                {
                    stars.Add(new Star { Type = StarType.Half });
                }
                
                while (stars.Count < 5)
                {
                    stars.Add(new Star { Type = StarType.Empty });
                }
                
                return stars;
            }
            
            return Enumerable.Repeat(new Star { Type = StarType.Empty }, 5).ToList();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
    
    public class Star
    {
        public StarType Type { get; set; }
    }
    
    public enum StarType
    {
        Empty,    
        Half,     
        Full      
    }
} 