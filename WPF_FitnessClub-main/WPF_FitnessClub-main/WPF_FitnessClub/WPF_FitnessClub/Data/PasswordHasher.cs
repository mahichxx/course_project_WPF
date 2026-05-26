using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WPF_FitnessClub.Data
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static bool VerifyPassword(string inputPassword, string hashedPassword)
        {
            // Сначала хешируем то, что ввел пользователь, и сравниваем с тем, что в базе
            string hashOfInput = HashPassword(inputPassword);
            return string.Equals(hashOfInput, hashedPassword, StringComparison.OrdinalIgnoreCase);
        }
    }
}
