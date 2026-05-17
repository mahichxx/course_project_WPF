using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub
{
	[Table("Reviews")]
	public class Review : IEquatable<Review>, INotifyPropertyChanged
	{
		private int id;
		private string user;
		private int score;
		private string comment;
		private DateTime createdDate;
		private int subscriptionId;
		private Subscription subscription;

		public Review()
		{
			createdDate = DateTime.Now;
		}

		public Review(string user, int score, string comment)
		{
			this.user = user;
			this.score = score;
			this.comment = comment;
			this.createdDate = DateTime.Now;
		}

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
		[StringLength(100)]
		[Column("UserName")]
		public string User
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
		[Range(1, 5)]
		public int Score
		{
			get => score;
			set
			{
				if (score != value)
				{
					score = value;
					OnPropertyChanged();
				}
			}
		}

		[StringLength(1000)]
		public string Comment
		{
			get => comment;
			set
			{
				if (comment != value)
				{
					comment = value;
					OnPropertyChanged();
				}
			}
		}

		[NotMapped]      
		public DateTime CreatedDate
		{
			get => createdDate;
			set
			{
				if (createdDate != value)
				{
					createdDate = value;
					OnPropertyChanged();
				}
			}
		}

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

		public event PropertyChangedEventHandler PropertyChanged;
		public void OnPropertyChanged([CallerMemberName] string prop = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}
		
		public bool Equals(Review other)
		{
			if (other == null)
				return false;

			return this.User == other.User &&
				   this.Score == other.Score &&
				   this.Comment == other.Comment;
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
				return false;

			return Equals((Review)obj);
		}

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + (User != null ? User.GetHashCode() : 0);
			hash = hash * 23 + Score.GetHashCode();
			hash = hash * 23 + (Comment != null ? Comment.GetHashCode() : 0);
			return hash;
		}
	}
}
