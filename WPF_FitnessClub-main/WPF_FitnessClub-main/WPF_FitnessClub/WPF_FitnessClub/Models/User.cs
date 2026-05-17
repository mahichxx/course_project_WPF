using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WPF_FitnessClub.Models
{
	[Table("Users")]
	public class User : BaseEntity
	{
		private string fullName;
		private string email;
		private string login;
		private string password;
		private UserRole role;
		private bool isBlocked;

		public User()
		{
		}

		public User(string fullName, string email, string login, string password, UserRole role)
		{
			FullName = fullName;
			Email = email;
			Login = login;
			Password = password;
			Role = role;
			IsBlocked = false;
		}

		[Required]
		[StringLength(100)]
		public string FullName
		{
			get => fullName;
			set
			{
				if (fullName != value)
				{
					fullName = value;
					OnPropertyChanged();
				}
			}
		}

		[Required]
		[StringLength(100)]
		[EmailAddress]
		public string Email
		{
			get => email;
			set
			{
				if (email != value)
				{
					email = value;
					OnPropertyChanged();
				}
			}
		}

		[Required]
		[StringLength(50)]
		[Index(IsUnique = true)]
		public string Login
		{
			get => login;
			set
			{
				if (login != value)
				{
					login = value;
					OnPropertyChanged();
				}
			}
		}

		[Required]
		[StringLength(100)]
		public string Password
		{
			get => password;
			set
			{
				if (password != value)
				{
					password = value;
					OnPropertyChanged();
				}
			}
		}

		[Column("Role", TypeName = "int")]
		[Required]
		public UserRole Role
		{
			get => role;
			set
			{
				if (role != value)
				{
					if (!Enum.IsDefined(typeof(UserRole), value))
					{
						role = UserRole.Client;     
					}
					else
					{
						role = value;
					}
					OnPropertyChanged();
				}
			}
		}

		[Required]
		public bool IsBlocked
		{
			get => isBlocked;
			set
			{
				if (isBlocked != value)
				{
					isBlocked = value;
					OnPropertyChanged();
				}
			}
		}

		[NotMapped]
		public string RoleAsString
		{
			get => Role.ToString();
			set
			{
				if (Enum.TryParse(value, out UserRole parsedRole))
				{
					Role = parsedRole;
				}
				else if (int.TryParse(value, out int roleValue) && Enum.IsDefined(typeof(UserRole), roleValue))
				{
					Role = (UserRole)roleValue;
				}
			}
		}

		public override string ToString()
		{
			return $"User[ID={Id}, Login={Login}, Name={FullName}, Role={Role}]";
		}
	}

	public enum UserRole
	{
		Client = 1,
		Coach = 2,
		Admin = 3
	}
}
