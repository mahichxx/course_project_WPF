using System;
using System.Collections.Generic;
using System.Linq;
using WPF_FitnessClub.Data.Repositories;
using WPF_FitnessClub.Data.Services.Interfaces;
using WPF_FitnessClub.Models;

namespace WPF_FitnessClub.Data.Services
{
    /// <summary>
    /// Сервис для работы с пользователями
    /// </summary>
    public class UserService : DataService
    {
        private static User _currentUser;


        public static void SetCurrentUser(User user)
        {
            _currentUser = user;
        }

        public User GetCurrentUser()
        {
            return _currentUser;
        }


        public List<User> GetAll()
        {
            return _unitOfWork.UserRepository.GetAll();
        }

        public User GetById(int id)
        {
            return _unitOfWork.UserRepository.GetById(id);
        }

        public User GetByLogin(string login)
        {
            try
            {
                var user = _unitOfWork.UserRepository.GetByLogin(login);
                
                if (user != null && !Enum.IsDefined(typeof(UserRole), user.Role))
                {
                    user.Role = UserRole.Client;
                }
                
                return user;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в UserService.GetByLogin: {ex.Message}");
                return null;
            }
        }

        public User GetByEmail(string email)
        {
            return _unitOfWork.UserRepository.GetByEmail(email);
        }

        public int Add(User user)
        {
            try
            {
                // Создаем пользователя
                _unitOfWork.UserRepository.Create(user);
                _unitOfWork.Save();
                
                // Отсоединяем сущность, чтобы избежать проблем при дальнейших операциях
                _unitOfWork.Context.Entry(user).State = System.Data.Entity.EntityState.Detached;
                
                // Очищаем контекст для следующих операций
                _unitOfWork.Context.ChangeTracker.Entries()
                    .Where(e => e.State != System.Data.Entity.EntityState.Detached)
                    .ToList()
                    .ForEach(e => e.State = System.Data.Entity.EntityState.Detached);
                
                return user.Id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при добавлении пользователя: {ex.Message}");
                return 0;
            }
        }

        public bool Update(User user)
        {
            try
            {
                // Очищаем контекст перед операцией обновления
                _unitOfWork.Context.ChangeTracker.Entries()
                    .Where(e => e.State != System.Data.Entity.EntityState.Detached)
                    .ToList()
                    .ForEach(e => e.State = System.Data.Entity.EntityState.Detached);
                
                // Получаем текущую версию пользователя из базы данных
                var existingUser = _unitOfWork.UserRepository.GetById(user.Id);
                if (existingUser == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Пользователь с ID {user.Id} не найден для обновления");
                    return false;
                }

                // Обновляем свойства
                existingUser.FullName = user.FullName;
                existingUser.Email = user.Email;
                existingUser.Login = user.Login;
                existingUser.Password = user.Password;
                existingUser.Role = user.Role;
                existingUser.IsBlocked = user.IsBlocked; // Явно обновляем свойство IsBlocked

                // Обновляем пользователя в репозитории
                _unitOfWork.UserRepository.Update(existingUser);
                
                try
                {
                    _unitOfWork.Save();
                    System.Diagnostics.Debug.WriteLine($"Пользователь с ID {user.Id} успешно обновлен");
                    return true;
                }
                catch (Exception saveEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении обновлений пользователя с ID {user.Id}: {saveEx.Message}");
                    
                    // Попробуем еще раз после повторной очистки контекста
                    _unitOfWork.Context.ChangeTracker.Entries()
                        .Where(e => e.State != System.Data.Entity.EntityState.Detached)
                        .ToList()
                        .ForEach(e => e.State = System.Data.Entity.EntityState.Detached);
                    
                    // Получаем пользователя заново
                    existingUser = _unitOfWork.UserRepository.GetById(user.Id);
                    if (existingUser == null)
                        return false;
                        
                    // Обновляем свойства
                    existingUser.FullName = user.FullName;
                    existingUser.Email = user.Email;
                    existingUser.Login = user.Login;
                    existingUser.Password = user.Password;
                    existingUser.Role = user.Role;
                    existingUser.IsBlocked = user.IsBlocked;
                    
                    // Обновляем пользователя в репозитории
                    _unitOfWork.UserRepository.Update(existingUser);
                    _unitOfWork.Save();
                    
                    System.Diagnostics.Debug.WriteLine($"Пользователь с ID {user.Id} успешно обновлен после повторной попытки");
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в UserService.Update: {ex.Message}");
                return false;
            }
        }
        public bool Delete(int id)
        {
            try
            {
                // Очищаем текущий контекст, чтобы избежать проблем с отслеживанием сущностей
                _unitOfWork.Context.ChangeTracker.Entries()
                    .Where(e => e.State != System.Data.Entity.EntityState.Detached)
                    .ToList()
                    .ForEach(e => e.State = System.Data.Entity.EntityState.Detached);
                
                // Получаем пользователя из базы данных заново (чистая версия)
                var user = _unitOfWork.UserRepository.GetById(id);
                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Пользователь с ID {id} не найден для удаления");
                    return false;
                }

                // Получаем все связанные данные напрямую из базы
                
                // Удаляем связи с CoachClient, где пользователь является тренером
                var coachClients = _unitOfWork.Context.Set<CoachClient>().Where(cc => cc.CoachId == id).ToList();
                foreach (var cc in coachClients)
                {
                    _unitOfWork.Context.Set<CoachClient>().Remove(cc);
                }
                
                // Удаляем связи с CoachClient, где пользователь является клиентом
                var clientCoaches = _unitOfWork.Context.Set<CoachClient>().Where(cc => cc.ClientId == id).ToList();
                foreach (var cc in clientCoaches)
                {
                    _unitOfWork.Context.Set<CoachClient>().Remove(cc);
                }
                
                // Удаляем связи с UserSubscription
                var userSubscriptions = _unitOfWork.Context.Set<UserSubscription>().Where(us => us.UserId == id).ToList();
                foreach (var us in userSubscriptions)
                {
                    _unitOfWork.Context.Set<UserSubscription>().Remove(us);
                }
                
                // Удаляем связи с WorkoutPlan, где пользователь является клиентом
                var clientWorkoutPlans = _unitOfWork.Context.Set<WorkoutPlan>().Where(wp => wp.ClientId == id).ToList();
                foreach (var wp in clientWorkoutPlans)
                {
                    _unitOfWork.Context.Set<WorkoutPlan>().Remove(wp);
                }
                
                // Удаляем связи с WorkoutPlan, где пользователь является тренером
                var coachWorkoutPlans = _unitOfWork.Context.Set<WorkoutPlan>().Where(wp => wp.CoachId == id).ToList();
                foreach (var wp in coachWorkoutPlans)
                {
                    _unitOfWork.Context.Set<WorkoutPlan>().Remove(wp);
                }
                
                // Удаляем связи с NutritionPlan, где пользователь является клиентом
                var clientNutritionPlans = _unitOfWork.Context.Set<NutritionPlan>().Where(np => np.ClientId == id).ToList();
                foreach (var np in clientNutritionPlans)
                {
                    _unitOfWork.Context.Set<NutritionPlan>().Remove(np);
                }
                
                // Удаляем связи с NutritionPlan, где пользователь является тренером
                var coachNutritionPlans = _unitOfWork.Context.Set<NutritionPlan>().Where(np => np.CoachId == id).ToList();
                foreach (var np in coachNutritionPlans)
                {
                    _unitOfWork.Context.Set<NutritionPlan>().Remove(np);
                }
                
                string userLogin = user.Login;
                var reviews = _unitOfWork.Context.Set<Review>().Where(r => r.User == userLogin).ToList();
                foreach (var review in reviews)
                {
                    _unitOfWork.Context.Set<Review>().Remove(review);
                }
                
                try
                {
                    // Сначала сохраняем изменения после удаления всех связей
                    _unitOfWork.Save();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка при удалении связей пользователя с ID {id}: {ex.Message}");
                    // Пробуем продолжить, даже если возникла ошибка при удалении связей
                }
                
                // Вновь очищаем контекст перед удалением пользователя
                _unitOfWork.Context.ChangeTracker.Entries()
                    .Where(e => e.State != System.Data.Entity.EntityState.Detached)
                    .ToList()
                    .ForEach(e => e.State = System.Data.Entity.EntityState.Detached);
                
                // Получаем пользователя заново после очистки контекста
                user = _unitOfWork.UserRepository.GetById(id);
                if (user == null)
                {
                    // Если пользователь уже удален, считаем операцию успешной
                    return true;
                }
                
                // Теперь удаляем самого пользователя
                _unitOfWork.UserRepository.Delete(user);
                _unitOfWork.Save();
                
                System.Diagnostics.Debug.WriteLine($"Пользователь с ID {id} успешно удален");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при удалении пользователя с ID {id}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Проверить уникальность логина
        /// </summary>
        public bool IsLoginUnique(string login)
        {
            return _unitOfWork.UserRepository.IsLoginUnique(login);
        }

        /// <summary>
        /// Проверить уникальность email
        /// </summary>
        public bool IsEmailUnique(string email)
        {
            return _unitOfWork.UserRepository.IsEmailUnique(email);
        }

        /// <summary>
        /// Получить пользователей по роли
        /// </summary>
        public List<User> GetByRole(UserRole role)
        {
            return _unitOfWork.UserRepository.GetByRole(role);
        }
        
        /// <summary>
        /// Получить всех клиентов
        /// </summary>
        public List<User> GetAllClients()
        {
            return GetByRole(UserRole.Client);
        }
        
        /// <summary>
        /// Получить всех клиентов (новый метод)
        /// </summary>
        public List<User> GetClients()
        {
            return GetByRole(UserRole.Client);
        }
        
        /// <summary>
        /// Получить клиентов конкретного тренера
        /// </summary>
        public List<User> GetCoachClients(int coachId)
        {
            try
            {
                // Сначала получаем все связи между тренером и клиентами
                var coachClients = _unitOfWork.Context.Set<CoachClient>()
                    .Where(cc => cc.CoachId == coachId)
                    .ToList();
                
                // Если связей нет, возвращаем пустой список
                if (coachClients.Count == 0)
                {
                    return new List<User>();
                }
                
                // Затем получаем все ID клиентов данного тренера
                var clientIds = coachClients.Select(cc => cc.ClientId).ToList();
                
                // Наконец, получаем всех пользователей с этими ID
                var clients = _unitOfWork.UserRepository.GetAll()
                    .Where(u => clientIds.Contains(u.Id))
                    .ToList();
                
                return clients;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении клиентов тренера: {ex.Message}");
                return new List<User>();
            }
        }
    }
} 