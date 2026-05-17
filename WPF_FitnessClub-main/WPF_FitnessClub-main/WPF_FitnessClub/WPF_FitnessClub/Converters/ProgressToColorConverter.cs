using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WPF_FitnessClub.Converters
{
    public class ProgressToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                progress = Math.Round(progress, 2);
                
                progress = Math.Max(0, Math.Min(1, progress));
                
                if (progress < 0.5)
                {
                    byte r = 255;
                    byte g = (byte)(255 * (progress * 2));  
                    byte b = 0;
                    return new SolidColorBrush(Color.FromRgb(r, g, b));
                }
                else
                {
                    byte r = (byte)(255 * (2 - progress * 2));  
                    byte g = 255;
                    byte b = 0;
                    return new SolidColorBrush(Color.FromRgb(r, g, b));
                }
            }
            
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
} 