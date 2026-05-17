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
            Light,
            Dark
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
            ChangeTheme(AppTheme.Light);
        }

        public void ChangeTheme(AppTheme theme)
        {
            var resources = Application.Current.Resources;

            switch (theme)
            {
                case AppTheme.Light:
                    resources["BackgroundColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("White");
                    resources["WindowBackgroundColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("White");
                    resources["TextColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333");
                    resources["SecondaryBackgroundColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F5F5F5");
                    
                    resources["BackgroundBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)resources["BackgroundColor"]);
                    resources["WindowBackgroundBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)resources["WindowBackgroundColor"]);
                    resources["TextBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)resources["TextColor"]);
                    resources["SecondaryBackgroundBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)resources["SecondaryBackgroundColor"]);
                    break;

                case AppTheme.Dark:
                    resources["BackgroundColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E");
                    resources["WindowBackgroundColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333");
                    resources["TextColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#DEDDD9");
                    resources["SecondaryBackgroundColor"] = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D2D");
                    
     
                    resources["BackgroundBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)resources["BackgroundColor"]);
                    resources["WindowBackgroundBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)resources["WindowBackgroundColor"]);
                    resources["TextBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)resources["TextColor"]);
                    resources["SecondaryBackgroundBrush"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)resources["SecondaryBackgroundColor"]);
                    break;
            }

			CurrentTheme = theme;
        }

        public void SetTheme(AppTheme theme)
        {
            ChangeTheme(theme);
        }
    }
} 