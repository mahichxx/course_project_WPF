using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.ViewModels
{
    public class UserSubscriptionViewModel : ViewModelBase
    {
        private readonly UserSubscription _userSubscription;

        public UserSubscriptionViewModel(UserSubscription userSubscription)
        {
            _userSubscription = userSubscription ?? throw new ArgumentNullException(nameof(userSubscription));
            
            ThemeManager.Instance.ThemeChanged += ThemeManager_ThemeChanged;
        }
        
        private void ThemeManager_ThemeChanged(object sender, ThemeManager.AppTheme e)
        {
            OnPropertyChanged(nameof(StatusColor));
        }
        
        public void Dispose()
        {
            ThemeManager.Instance.ThemeChanged -= ThemeManager_ThemeChanged;
        }

        public int Id => _userSubscription.Id;
        public DateTime PurchaseDate => _userSubscription.PurchaseDate;
        public DateTime ExpiryDate => _userSubscription.ExpiryDate;
        public Subscription Subscription => _userSubscription.Subscription;
        public bool IsCanceled => _userSubscription.IsCanceled;

        public bool IsExpired => ExpiryDate < DateTime.Now;
        public string StatusText
        {
            get
            {
                if (IsCanceled) 
                    return (string)System.Windows.Application.Current.Resources["StatusCancelled"];
                if (IsExpired) 
                    return (string)System.Windows.Application.Current.Resources["StatusExpired"];
                if (PurchaseDate > DateTime.Now) 
                    return (string)System.Windows.Application.Current.Resources["StatusPending"];
                
                return (string)System.Windows.Application.Current.Resources["StatusActive"];
            }
        }

        public Brush StatusColor
        {
            get
            {
                bool isDarkTheme = ThemeManager.Instance.CurrentTheme == ThemeManager.AppTheme.Dark;
                
                if (IsCanceled)
                {
                    return isDarkTheme 
                        ? new SolidColorBrush(Color.FromRgb(229, 115, 115))     
                        : new SolidColorBrush(Color.FromRgb(183, 28, 28));      
                }
                
                if (IsExpired)
                {
                    return isDarkTheme 
                        ? new SolidColorBrush(Color.FromRgb(180, 180, 180))     
                        : Brushes.Gray;
                }
                
                if (PurchaseDate > DateTime.Now)
                {
                    return isDarkTheme 
                        ? new SolidColorBrush(Color.FromRgb(255, 167, 38))     
                        : Brushes.DarkOrange;
                }
                
                return isDarkTheme 
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))       
                    : Brushes.Green;
            }
        }

    }
} 