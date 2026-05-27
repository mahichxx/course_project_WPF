using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        private string phone;
        private double weight;
        private double height;
        private int age;
        private string gender; 

        public User() { }

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
            set { fullName = value; OnPropertyChanged(); }
        }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email
        {
            get => email;
            set { email = value; OnPropertyChanged(); }
        }

        [Required]
        [StringLength(50)]
        public string Login
        {
            get => login;
            set { login = value; OnPropertyChanged(); }
        }

        [Required]
        [StringLength(100)]
        public string Password
        {
            get => password;
            set { password = value; OnPropertyChanged(); }
        }

        [Column("Role", TypeName = "int")]
        public UserRole Role
        {
            get => role;
            set { role = value; OnPropertyChanged(); }
        }

        public bool IsBlocked
        {
            get => isBlocked;
            set { isBlocked = value; OnPropertyChanged(); }
        }

        public string Phone { get => phone; set { phone = value; OnPropertyChanged(); } }
        public double Weight { get => weight; set { weight = value; OnPropertyChanged(); } }
        public double Height { get => height; set { height = value; OnPropertyChanged(); } }
        public int Age { get => age; set { age = value; OnPropertyChanged(); } }
        public string Gender { get => gender; set { gender = value; OnPropertyChanged(); } }

        public override string ToString() => $"User[{Login}]";
    }

    public enum UserRole { Client = 1, Coach = 2, Admin = 3 }
}