using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace WPF_FitnessClub.Models
{
    [Table("UserSubscriptions")]
    public class UserSubscription : BaseEntity
    {
        private int userId;
        private User user;
        private int subscriptionId;
        private Subscription subscription;
        private DateTime purchaseDate;
        private DateTime expiryDate;
        private bool isCanceled;

        public UserSubscription()
        {
            PurchaseDate = DateTime.Now;
            IsCanceled = false;
        }

        [Required]
        public int UserId
        {
            get => userId;
            set
            {
                if (userId != value)
                {
                    userId = value;
                    OnPropertyChanged();
                }
            }
        }

        [ForeignKey("UserId")]
        public virtual User User
        {
            get => user;
            set
            {
                if (user != value)
                {
                    user = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required]
        public int SubscriptionId
        {
            get => subscriptionId;
            set
            {
                if (subscriptionId != value)
                {
                    subscriptionId = value;
                    OnPropertyChanged();
                }
            }
        }

        [ForeignKey("SubscriptionId")]
        public virtual Subscription Subscription
        {
            get => subscription;
            set
            {
                if (subscription != value)
                {
                    subscription = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required]
        public DateTime PurchaseDate
        {
            get => purchaseDate;
            set
            {
                if (purchaseDate != value)
                {
                    purchaseDate = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required]
        public DateTime ExpiryDate
        {
            get => expiryDate;
            set
            {
                if (expiryDate != value)
                {
                    expiryDate = value;
                    OnPropertyChanged();
                    if (value <= DateTime.Now && Math.Abs((value - DateTime.Now).TotalMinutes) < 5)
                    {
                        IsCanceled = true;
                    }
                }
            }
        }
        
        [NotMapped]
        public bool IsCanceled
        {
            get => isCanceled;
            set
            {
                if (isCanceled != value)
                {
                    isCanceled = value;
                    OnPropertyChanged();
                }
            }
        }

        [NotMapped]
        public bool IsActive => ExpiryDate >= DateTime.Now;

        public override string ToString()
        {
            return $"UserSubscription[ID={Id}, UserId={UserId}, SubscriptionId={SubscriptionId}, Active={IsActive}]";
        }
    }
} 