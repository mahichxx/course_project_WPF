using System;
using System.Windows;
using System.ComponentModel;

namespace WPF_FitnessClub
{
    public class ThemeManager : INotifyPropertyChanged
    {
        private static ThemeManager _instance;
        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ThemeManager();
                return _instance;
            }
        }

        public enum AppTheme
        {
            Light, //Ораньжевы
            Dark //Зеленый
        }

        private AppTheme _currentTheme = AppTheme.Light;
        public AppTheme CurrentTheme
        {
            get { return _currentTheme; }
            private set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    ThemeChanged?.Invoke(this, _currentTheme);
                    OnPropertyChanged(nameof(CurrentThemeString));
                }
            }
        }

        public string CurrentThemeString
        {
            get { return _currentTheme.ToString(); }
        }

        public event EventHandler<AppTheme> ThemeChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ThemeManager()
        {
            ChangeTheme(AppTheme.Light); // По умолчанию  ораньжевый
        }

        public void ChangeTheme(AppTheme theme)
        {
            var resources = Application.Current.Resources;

            switch (theme)
            {
                case AppTheme.Light: // === ОРАНЖЕВАЯ ТЕМА ===
                    resources["PrimaryRed"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF7A50");
                    resources["BackgroundColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#efeded");
                    resources["WindowBackgroundColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#efeded");
                    resources["TextColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#252727");
                    resources["SecondaryBackgroundColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#fdf3f0");
                    resources["MediumGray"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#808080");
                    resources["LightGray"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D1D9D6");
                    break;

                case AppTheme.Dark: // === ЗЕЛЕНАЯ ТЕМА ===
                    resources["PrimaryRed"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#83AF3B");
                    resources["BackgroundColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F5F8");
                    resources["WindowBackgroundColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F5F8");
                    resources["TextColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#000000");
                    resources["SecondaryBackgroundColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF");
                    resources["MediumGray"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#808080");
                    resources["LightGray"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E5");
                    break;
            }

            // Обновляем кисти, чтобы изменения вступили в силу
            UpdateBrushes();

            CurrentTheme = theme;
        }

        private void UpdateBrushes()
        {
            var resources = Application.Current.Resources;
            // Список ключей цветов и соответствующих им кистей
            string[] colorKeys = { "PrimaryRed", "BackgroundColor", "WindowBackgroundColor", "TextColor", "SecondaryBackgroundColor", "MediumGray", "LightGray" };
            string[] brushKeys = { "PrimaryRedBrush", "BackgroundBrush", "WindowBackgroundBrush", "TextBrush", "SecondaryBackgroundBrush", "MediumGrayBrush", "LightGrayBrush" };

            for (int i = 0; i < colorKeys.Length; i++)
            {
                if (resources.Contains(colorKeys[i]))
                {
                    System.Windows.Media.Color color = (System.Windows.Media.Color)resources[colorKeys[i]];
                    resources[brushKeys[i]] = new System.Windows.Media.SolidColorBrush(color);
                }
            }

            // Создаем Темно-оранжевый/Зеленый для эффекта наведения (на 30 тонов темнее основного)
            System.Windows.Media.Color primary = (System.Windows.Media.Color)resources["PrimaryRed"];
            resources["DarkRedBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(
                (byte)Math.Max(0, primary.R - 30),
                (byte)Math.Max(0, primary.G - 30),
                (byte)Math.Max(0, primary.B - 30)));
        }
        public void SetTheme(AppTheme theme)
        {
            ChangeTheme(theme);
        }
    }
} 