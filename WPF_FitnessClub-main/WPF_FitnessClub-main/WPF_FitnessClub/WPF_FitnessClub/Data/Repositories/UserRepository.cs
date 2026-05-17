using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Data.Repositories
{
    public class UserRepository : BaseRepository<User>
    {
        public UserRepository(AppDbContext context) : base(context)
        {
        }

        public override List<User> GetAll()
        {
            try
            {
                var users = _dbSet.AsNoTracking().ToList();
                foreach (var user in users)
                {
                    if (!Enum.IsDefined(typeof(UserRole), user.Role))
                    {
                        user.Role = UserRole.Client;
                    }
                }
                return users;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении всех пользователей: {ex.Message}");
                return new List<User>();
            }
        }

        public override User GetById(int id)
        {
            try
            {
                var user = _dbSet.AsNoTracking().FirstOrDefault(u => u.Id == id);
                if (user != null && !Enum.IsDefined(typeof(UserRole), user.Role))
                {
                    user.Role = UserRole.Client;
                }
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении пользователя по ID: {ex.Message}");
                return null;
            }
        }

        public User GetByLogin(string login)
        {
            try
            {
                var user = _dbSet.AsNoTracking().FirstOrDefault(u => u.Login == login);
                if (user != null && !Enum.IsDefined(typeof(UserRole), user.Role))
                {
                    user.Role = UserRole.Client;
                }
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении пользователя по логину: {ex.Message}");
                return null;
            }
        }

        public User GetByEmail(string email)
        {
            try
            {
                var user = _dbSet.AsNoTracking().FirstOrDefault(u => u.Email == email);
                if (user != null && !Enum.IsDefined(typeof(UserRole), user.Role))
                {
                    user.Role = UserRole.Client;
                }
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении пользователя по email: {ex.Message}");
                return null;
            }
        }

        public List<User> GetByRole(UserRole role)
        {
            try
            {
                var users = _dbSet.AsNoTracking().Where(u => u.Role == role).ToList();
                foreach (var user in users)
                {
                    if (!Enum.IsDefined(typeof(UserRole), user.Role))
                    {
                        user.Role = role;     
                    }
                }
                return users;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении пользователей по роли: {ex.Message}");
                return new List<User>();
            }
        }

        public bool IsLoginUnique(string login)
        {
            return !_dbSet.Any(u => u.Login == login);
        }

        public bool IsEmailUnique(string email)
        {
            return !_dbSet.Any(u => u.Email == email);
        }
    }
} 