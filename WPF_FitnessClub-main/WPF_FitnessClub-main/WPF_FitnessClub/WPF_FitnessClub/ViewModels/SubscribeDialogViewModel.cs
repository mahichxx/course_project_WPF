using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.ViewModels
{
    public class SubscribeDialogViewModel : ViewModelBase
    {
        private string _fullName;
        private string _email;
        private string _subscriptionName;
        private decimal _subscriptionPrice;
        private string _subscriptionDuration;
        private int _subscriptionId;
        private bool _hasUserData;

        public SubscribeDialogViewModel(Subscription subscription, User currentUser = null)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            SubscriptionName = subscription.Name;
            SubscriptionPrice = subscription.Price;
            SubscriptionDuration = subscription.Duration;
            SubscriptionId = subscription.Id;

            if (currentUser != null)
            {
                FullName = currentUser.FullName;
                Email = currentUser.Email;
                HasUserData = true;
            }
            else
            {
                HasUserData = false;
            }
        }

        public string FullName
        {
            get => _fullName;
            set
            {
                if (_fullName != value)
                {
                    _fullName = value;
                    OnPropertyChanged(nameof(FullName));
                    OnPropertyChanged(nameof(IsDataValid));
                }
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged(nameof(Email));
                    OnPropertyChanged(nameof(IsDataValid));
                }
            }
        }

        public string SubscriptionName
        {
            get => _subscriptionName;
            set
            {
                if (_subscriptionName != value)
                {
                    _subscriptionName = value;
                    OnPropertyChanged(nameof(SubscriptionName));
                }
            }
        }

        public decimal SubscriptionPrice
        {
            get => _subscriptionPrice;
            set
            {
                if (_subscriptionPrice != value)
                {
                    _subscriptionPrice = value;
                    OnPropertyChanged(nameof(SubscriptionPrice));
                }
            }
        }

        public string SubscriptionDuration
        {
            get => _subscriptionDuration;
            set
            {
                if (_subscriptionDuration != value)
                {
                    _subscriptionDuration = value;
                    OnPropertyChanged(nameof(SubscriptionPrice));
                }
            }
        }

        public int SubscriptionId
        {
            get => _subscriptionId;
            set
            {
                if (_subscriptionId != value)
                {
                    _subscriptionId = value;
                    OnPropertyChanged(nameof(SubscriptionId));
                }
            }
        }

        public bool HasUserData
        {
            get => _hasUserData;
            set
            {
                if (_hasUserData != value)
                {
                    _hasUserData = value;
                    OnPropertyChanged(nameof(HasUserData));
                }
            }
        }

        public bool IsDataValid => !string.IsNullOrWhiteSpace(FullName) && 
                                   !string.IsNullOrWhiteSpace(Email) && 
                                   IsValidEmail(Email);

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
} 