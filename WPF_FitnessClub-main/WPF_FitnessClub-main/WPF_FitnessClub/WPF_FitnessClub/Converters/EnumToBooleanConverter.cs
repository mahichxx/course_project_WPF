using System;
using System.Globalization;
using System.Windows.Data;

namespace WPF_FitnessClub.Converters
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string parameterString = parameter.ToString();
            
            if (int.TryParse(parameterString, out int parameterValue))
            {
                if (value is int valueInt)
                {
                    return valueInt == parameterValue;
                }
                
                if (value is Enum)
                {
                    return System.Convert.ToInt32(value) == parameterValue;
                }
            }
            
            if (value is Enum && Enum.IsDefined(value.GetType(), value))
            {
                return value.ToString().Equals(parameterString, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter != null)
            {
                if (targetType.IsEnum)
                {
                    if (parameter is string parameterString)
                    {
                        if (int.TryParse(parameterString, out int intValue))
                        {
                            return Enum.ToObject(targetType, intValue);
                        }
                        
                    }
                    
                    if (parameter is int intParam)
                    {
                        return Enum.ToObject(targetType, intParam);
                    }
                }
            }

            return Binding.DoNothing;
        }
    }
} 