using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System;

namespace WPF_FitnessClub.Models
{
	[Table("Subscriptions")]
	public class Subscription : INotifyPropertyChanged
	{
		private int id;
		private string name;
		private decimal price;
		private string description;
		private string imagePath;
		private string duration;
		private string subscriptionType;
		private List<Review> reviews;
		private double rating;
		private int? userId;
		private object tag;

		#region Свойства
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id
		{
			get => id;
			set
			{
				if (id != value)
				{
					id = value;
					OnPropertyChanged();
				}
			}
		}

		[Required]
		[StringLength(150)]
		public string Name
		{
			get => name;
			set
			{
				if (name != value)
				{
					name = value;
					OnPropertyChanged();
				}
			}
		}

		[Required]
		public decimal Price
		{
			get => price;
			set
			{
				if (price != value)
				{
					price = value;
					OnPropertyChanged();
				}
			}
		}

		[StringLength(1000)]
		public string Description
		{
			get => description;
			set
			{
				if (description != value)
				{
					description = value;
					OnPropertyChanged();
				}
			}
		}

		[StringLength(255)]
        public string ImagePath
        {
            get => imagePath;
            set
            {
                if (imagePath != value)
                {
                    imagePath = value;
                    OnPropertyChanged();
                }
            }
        }

        [NotMapped]
        public string LocalizedType
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SubscriptionType)) return "---";
                object translated = Application.Current.TryFindResource(SubscriptionType);
                return translated != null ? translated.ToString() : SubscriptionType;
            }
        }

        [StringLength(50)]
        public string Duration
        {
            get => duration; 
            set
            {
                duration = value;
                OnPropertyChanged();
                OnPropertyChanged("LocalizedDuration"); // Уведомляем переводчик
            }
        }

        [StringLength(50)]
        public string SubscriptionType
        {
            get => subscriptionType;
            set
            {
                subscriptionType = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LocalizedType)); //  для перерисовки текста
            }
        }

        public virtual List<Review> Reviews
		{
			get => reviews;
			set
			{
				if (reviews != value)
				{
					reviews = value;
					OnPropertyChanged();
				}
			}
		}

        [NotMapped]
        public double Rating
        {
            get => CalculateRating();
            set { 
				OnPropertyChanged("Rating");
                OnPropertyChanged(nameof(Rating));

            }
        }

        [NotMapped]
        public string LocalizedDuration
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Duration)) return "---";
                object translated = Application.Current.TryFindResource(Duration);
                return translated != null ? translated.ToString() : Duration;
            }
        }


        [NotMapped]      
		public int? UserId
		{
			get => userId;
			set
			{
				if (userId != value)
				{
					userId = value ?? 0;
					OnPropertyChanged();
				}
			}
		}
		#endregion

		public Subscription()
		{
			this.name = "";
			this.price = 0m;
			this.description = "";
			this.imagePath = "";
			this.duration = "";
			this.subscriptionType = "";
			this.reviews = new List<Review>();
			this.rating = 0.0;
			this.userId = null;
		}

		public Subscription(string name, decimal price, string description, string path, string duration, string subscriptionType, List<Review> reviews)
		{
			this.name = name;
			this.price = price;
			this.description = description;
			this.imagePath = path;
			this.duration = duration;
			this.subscriptionType = subscriptionType;
			this.reviews = new List<Review>();
		}

		public Subscription(string name, decimal price, string description, string imagePath, string duration, string subscriptionType)
		{
			this.name = name;
			this.price = price;
			this.description = description;
			this.imagePath = imagePath;
			this.duration = duration;
			this.subscriptionType = subscriptionType;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged([CallerMemberName] string prop = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}

		public double CalculateRating()
		{
			try
			{
				if (Reviews == null || Reviews.Count == 0)
				{
					Debug.WriteLine($"CalculateRating для {Name}: нет отзывов");
					return 0.0;
				}

				var validReviews = Reviews.Where(r => r != null && r.Score > 0).ToList();
				if (validReviews.Count == 0)
				{
					Debug.WriteLine($"CalculateRating для {Name}: нет отзывов с положительной оценкой");
					return 0.0;
				}

				double _rating = validReviews.Average(r => r.Score);
				Debug.WriteLine($"CalculateRating для {Name}: рейтинг {_rating:F1} из {validReviews.Count} отзывов");
				return double.Parse(_rating.ToString("F1"));
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Ошибка при расчете рейтинга для {Name}: {ex.Message}");
				return 0.0;
			}
		}

		public void ClearUserAssociation()
		{
			this.userId = null;
			OnPropertyChanged(nameof(UserId));
		}
	}
}
