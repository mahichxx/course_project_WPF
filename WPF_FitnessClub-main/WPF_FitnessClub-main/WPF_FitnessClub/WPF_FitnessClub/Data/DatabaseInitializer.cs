using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using WPF_FitnessClub.Models;
using WPF_FitnessClub.DataBase;

namespace WPF_FitnessClub.Data
{
    public class DatabaseInitializer
    {
        private readonly string _connectionString;
        private readonly string _backupDirectory;
        private List<User> _cachedUsers = new List<User>();
        private List<Subscription> _cachedSubscriptions = new List<Subscription>();
        private List<Review> _cachedReviews = new List<Review>();
        private List<UserSubscription> _cachedUserSubscriptions = new List<UserSubscription>();
        private List<CoachClient> _cachedCoachClients = new List<CoachClient>();
        private List<NutritionPlan> _cachedNutritionPlans = new List<NutritionPlan>();
        private List<WorkoutPlan> _cachedWorkoutPlans = new List<WorkoutPlan>();
        private bool _hasExistingData = false;
        private readonly DatabaseBackupService _backupService;

        public DatabaseInitializer()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["FitnessClubConnectionString"].ConnectionString;
            _backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
            _backupService = new DatabaseBackupService();
            
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
                System.Diagnostics.Debug.WriteLine($"Создана директория для резервных копий: {_backupDirectory}");
            }
        }

        public bool InitializeDatabase()
        {
            try
            {
                var connectionService = new DatabaseConnectionService(_connectionString);
                bool isConnected = connectionService.CheckConnectionWithRetry(3, 1000);

                if (isConnected)
                {
                    System.Diagnostics.Debug.WriteLine("Подключение к базе данных успешно.");
                    
                    using (var context = new AppDbContext())
                    {
                        try
                        {
                            if (!context.Database.Exists())
                            {
                                System.Diagnostics.Debug.WriteLine("База данных не существует, создаем новую.");
                                return CreateNewDatabase();
                            }
                            
                            try
                            {
                                MigrationManager.ApplyMigrations(context);
                                
                                _backupService.BackupDatabaseToJson();
                                
                                return true;
                            }
                            catch (Exception migrationEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Ошибка при применении миграций: {migrationEx.Message}");
                                if (migrationEx.InnerException != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Внутреннее исключение: {migrationEx.InnerException.Message}");
                                }
                                
                                File.AppendAllText("migration_error.log", 
                                    $"{DateTime.Now}: Ошибка при применении миграций: {migrationEx.Message}\n" +
                                    $"Inner: {migrationEx.InnerException?.Message}\n" +
                                    $"Stack: {migrationEx.StackTrace}\n\n");
                                
                                return false;
                            }
                        }
                        catch (Exception dbEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Ошибка при проверке/создании базы данных: {dbEx.Message}");
                            return false;
                        }
                    }
                }
                else
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Не удалось подключиться к базе данных или база не существует. Хотите создать новую базу данных?",
                        "Проблема с базой данных",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        return CreateNewDatabase();
                    }
                    else
                    {
                        MessageBox.Show("Приложение будет закрыто, так как нет доступной базы данных.",
                            "Закрытие приложения",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при инициализации базы данных: {ex.Message}",
                    "Ошибка инициализации БД", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                return false;
            }
        }

       

        private bool CreateNewDatabase()
        {
            try
            {
                Database.SetInitializer<AppDbContext>(null);
                
                using (var context = new AppDbContext())
                {
                    try
                    {
                        context.Database.Create();
                        System.Diagnostics.Debug.WriteLine("База данных создана");
                        
                        MigrationManager.ApplyMigrations(context);
                        
                        MessageBox.Show("База данных была успешно создана. Сейчас данные будут восстановлены.",
                            "БД создана", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка при инициализации базы данных: {dbEx.Message}");
                        if (dbEx.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Внутреннее исключение: {dbEx.InnerException.Message}");
                            if (dbEx.InnerException.InnerException != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Вложенное исключение: {dbEx.InnerException.InnerException.Message}");
                            }
                        }
                        
                        File.AppendAllText("db_error.log", 
                            $"{DateTime.Now}: Ошибка при инициализации базы данных: {dbEx.Message}\n" +
                            $"Inner: {dbEx.InnerException?.Message}\n" +
                            $"Inner Inner: {dbEx.InnerException?.InnerException?.Message}\n" +
                            $"Stack: {dbEx.StackTrace}\n\n");
                        
                        throw;    
                    }
                        
                    bool restoredFromJson = RestoreFromJsonFile(context);
                    
                    if (!restoredFromJson)
                    {
                        if (_hasExistingData)
                        {
                            RestoreData(context);
                        }
                        else
                        {
                            if (context.Users.Count() == 0)
                            {
                                CreateDefaultUsers(context);
                            }

                            if (context.Subscriptions.Count() == 0)
                            {
                                CreateDefaultSubscriptions(context);
                            }

                            if (context.Reviews.Count() == 0)
                            {
                                CreateDefaultReviews(context);
                            }

                            if (context.WorkoutPlans.Count() == 0)
                            {
                                CreateDefaultWorkoutPlans(context);
                            }

                            if (context.NutritionPlans.Count() == 0)
                            {
                                CreateDefaultNutritionPlans(context);
                            }

                            if (context.CoachClients.Count() == 0)
                            {
                                CreateDefaultCoachClients(context);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Данные успешно восстановлены из JSON файла.",
                            "Восстановление данных", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Information);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось создать базу данных: {ex.Message}",
                    "Ошибка создания БД", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                return false;
            }
        }
        
        private bool RestoreFromJsonFile(AppDbContext context)
        {
            try
            {
                var backupData = _backupService.RestoreDatabaseFromJson();
                if (backupData == null)
                {
                    System.Diagnostics.Debug.WriteLine("JSON файл с резервной копией не найден или поврежден.");
                    return false;
                }

                int restoredCount = 0;

                if (backupData.Users.Count > 0)
                {
                    foreach (var user in backupData.Users)
                    {
                        var newUser = new User
                        {
                            Login = user.Login,
                            Password = user.Password,
                            FullName = user.FullName,
                            Role = user.Role,
                            IsBlocked = user.IsBlocked,
                            Email = user.Email
                        };
                        
                        context.Users.Add(newUser);
                    }
                    context.SaveChanges();
                    restoredCount += backupData.Users.Count;
                    System.Diagnostics.Debug.WriteLine($"Восстановлено пользователей: {backupData.Users.Count}");
                }

                if (backupData.Subscriptions.Count > 0)
                {
                    foreach (var subscription in backupData.Subscriptions)
                    {
                        var newSubscription = new Subscription
                        {
                            Name = subscription.Name,
                            Price = subscription.Price,
                            Description = subscription.Description,
                            ImagePath = subscription.ImagePath,
                            Duration = subscription.Duration,
                            SubscriptionType = subscription.SubscriptionType
                        };
                        
                        context.Subscriptions.Add(newSubscription);
                    }
                    context.SaveChanges();
                    restoredCount += backupData.Subscriptions.Count;
                    System.Diagnostics.Debug.WriteLine($"Восстановлено абонементов: {backupData.Subscriptions.Count}");
                }
                
                var userMap = context.Users.ToDictionary(u => u.Login, u => u.Id);
                var subscriptionMap = context.Subscriptions.ToDictionary(s => s.Name, s => s.Id);
                
                if (backupData.Reviews.Count > 0)
                {
                    foreach (var review in backupData.Reviews)
                    {
                        var subscription = backupData.Subscriptions.FirstOrDefault(s => s.Id == review.SubscriptionId);
                        if (subscription != null && subscriptionMap.ContainsKey(subscription.Name))
                        {
                            var newReview = new Review
                            {
                                User = review.User,
                                Comment = review.Comment,
                                Score = review.Score,
                                CreatedDate = review.CreatedDate,
                                SubscriptionId = subscriptionMap[subscription.Name]
                            };
                            
                            context.Reviews.Add(newReview);
                        }
                    }
                    context.SaveChanges();
                    System.Diagnostics.Debug.WriteLine($"Восстановлено отзывов: {context.Reviews.Count()}");
                    restoredCount += context.Reviews.Count();
                }
                
                if (backupData.UserSubscriptions.Count > 0)
                {
                    foreach (var userSub in backupData.UserSubscriptions)
                    {
                        var user = backupData.Users.FirstOrDefault(u => u.Id == userSub.UserId);
                        var subscription = backupData.Subscriptions.FirstOrDefault(s => s.Id == userSub.SubscriptionId);
                        
                        if (user != null && subscription != null && 
                            userMap.ContainsKey(user.Login) && subscriptionMap.ContainsKey(subscription.Name))
                        {
                            var newUserSub = new UserSubscription
                            {
                                UserId = userMap[user.Login],
                                SubscriptionId = subscriptionMap[subscription.Name],
                                PurchaseDate = userSub.PurchaseDate,
                                ExpiryDate = userSub.ExpiryDate
                            };
                            
                            context.UserSubscriptions.Add(newUserSub);
                        }
                    }
                    context.SaveChanges();
                    System.Diagnostics.Debug.WriteLine($"Восстановлено подписок пользователей: {context.UserSubscriptions.Count()}");
                    restoredCount += context.UserSubscriptions.Count();
                }
                
                System.Diagnostics.Debug.WriteLine($"Всего восстановлено записей из JSON: {restoredCount}");
                return restoredCount > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при восстановлении из JSON: {ex.Message}");
                return false;
            }
        }

        private void RestoreData(AppDbContext context)
        {
            try
            {
                int restoredCount = 0;

                if (_cachedUsers.Count > 0)
                {
                    foreach (var user in _cachedUsers)
                    {
                        var newUser = new User
                        {
                            Login = user.Login,
                            Password = user.Password,
                            FullName = user.FullName,
                            Role = user.Role,
                            IsBlocked = user.IsBlocked,
                            Email = user.Email
                        };
                        
                        context.Users.Add(newUser);
                    }
                    context.SaveChanges();
                    restoredCount += _cachedUsers.Count;
                    System.Diagnostics.Debug.WriteLine($"Восстановлено пользователей: {_cachedUsers.Count}");
                }

                if (_cachedSubscriptions.Count > 0)
                {
                    foreach (var subscription in _cachedSubscriptions)
                    {
                        var newSubscription = new Subscription
                        {
                            Name = subscription.Name,
                            Price = subscription.Price,
                            Description = subscription.Description,
                            ImagePath = subscription.ImagePath,
                            Duration = subscription.Duration,
                            SubscriptionType = subscription.SubscriptionType
                        };
                        
                        context.Subscriptions.Add(newSubscription);
                    }
                    context.SaveChanges();
                    restoredCount += _cachedSubscriptions.Count;
                    System.Diagnostics.Debug.WriteLine($"Восстановлено абонементов: {_cachedSubscriptions.Count}");
                }

                var subscriptionMap = context.Subscriptions.ToDictionary(s => s.Name, s => s.Id);
                
                if (_cachedReviews.Count > 0)
                {
                    foreach (var review in _cachedReviews)
                    {
                        var subscription = _cachedSubscriptions.FirstOrDefault(s => s.Id == review.SubscriptionId);
                        if (subscription != null && subscriptionMap.ContainsKey(subscription.Name))
                        {
                            var newReview = new Review
                            {
                                User = review.User,
                                Comment = review.Comment,
                                Score = review.Score,
                                CreatedDate = review.CreatedDate,
                                SubscriptionId = subscriptionMap[subscription.Name]
                            };
                            
                            context.Reviews.Add(newReview);
                        }
                    }
                    context.SaveChanges();
                    restoredCount += context.Reviews.Count();
                    System.Diagnostics.Debug.WriteLine($"Восстановлено отзывов: {context.Reviews.Count()}");
                }

                MessageBox.Show($"Восстановлено {restoredCount} записей из предыдущей базы данных.",
                    "Данные восстановлены",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при восстановлении данных: {ex.Message}");
                MessageBox.Show($"Произошла ошибка при восстановлении данных: {ex.Message}",
                    "Ошибка восстановления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CreateDefaultUsers(AppDbContext context)
        {
            try
            {
                var users = new List<User>
                {
                    new User
                    {
                        Id = 1,
                        FullName = "Администратор Системы",
                        Email = "admin@fitness.ru",
                    Login = "admin",
                        Password = "admin123",
                    Role = UserRole.Admin,
                        IsBlocked = false
                    },
                    new User
                    {
                        Id = 2,
                    FullName = "Иванов Иван Иванович",
                        Email = "ivan@fitness.ru",
                        Login = "coach",
                        Password = "coach123",
                    Role = UserRole.Coach,
                        IsBlocked = false
                    },
                    new User
                    {
                        Id = 3,
                    FullName = "Петров Петр Петрович",
                        Email = "petr@gmail.com",
                        Login = "client",
                        Password = "client123",
                    Role = UserRole.Client,
                        IsBlocked = false
                    },
                    new User
                    {
                        Id = 4,
                        FullName = "Иван Тренеров",
                        Email = "coach1@example.com",
                        Login = "coach1",
                        Password = "password",
                        Role = UserRole.Coach,
                        IsBlocked = false
                    },
                    new User
                    {
                        Id = 5,
                        FullName = "Мария Инструкторова",
                        Email = "coach2@example.com",
                        Login = "coach2",
                        Password = "password",
                        Role = UserRole.Coach,
                        IsBlocked = false
                    },
                    new User
                    {
                        Id = 6,
                        FullName = "Алексей Фитнесов",
                        Email = "coach3@example.com",
                        Login = "coach3",
                        Password = "password",
                        Role = UserRole.Coach,
                        IsBlocked = false
                    },
                    new User
                    {
                        Id = 7,
                        FullName = "Анна Клиентова",
                        Email = "client1@example.com",
                        Login = "client1",
                        Password = "password",
                        Role = UserRole.Client,
                        IsBlocked = false
                    },
                    new User
                    {
                        Id = 8,
                        FullName = "Петр Спортивный",
                        Email = "client2@example.com",
                        Login = "client2",
                        Password = "password",
                        Role = UserRole.Client,
                        IsBlocked = false
                    },
                    new User
                    {
                        Id = 9,
                        FullName = "Елена Здоровая",
                        Email = "client3@example.com",
                        Login = "client3",
                        Password = "password",
                        Role = UserRole.Client,
                        IsBlocked = false
                    },
                    new User
                    {
                        Id = 10,
                        FullName = "Сергей Силин",
                        Email = "client4@example.com",
                        Login = "client4",
                        Password = "password",
                        Role = UserRole.Admin,
                        IsBlocked = false
                    },
                    new User
                    {
                        Id = 11,
                        FullName = "Ольга Фитнесова",
                        Email = "client5@example.com",
                        Login = "client5",
                        Password = "password",
                        Role = UserRole.Coach,
                        IsBlocked = false
                    },
                    new User
                    {
                        Id = 12,
                        FullName = "Дмитрий Бегунов",
                        Email = "client6@example.com",
                        Login = "client6",
                        Password = "password",
                        Role = UserRole.Client,
                        IsBlocked = false
                    },
                    new User
                    {
                        Id = 13,
                        FullName = "Наталья Гибкая",
                        Email = "client7@example.com",
                        Login = "client7",
                        Password = "password",
                        Role = UserRole.Client,
                        IsBlocked = false
                    }
                };
                
                context.Users.AddRange(users);
                context.SaveChanges();
                
                System.Diagnostics.Debug.WriteLine($"Созданы {users.Count} пользователей");
                MessageBox.Show($"Созданы {users.Count} пользователей",
                    "Пользователи созданы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при создании пользователей: {ex.Message}");
            }
        }

        private void CreateDefaultSubscriptions(AppDbContext context)
        {
            try
            {
                var subscriptions = new List<Subscription>
                {
                    new Subscription
                    {
                        Id = 1,
                        Name = "Тренажерный зал",
                        Price = 85.00m,
                        Description = "Абонемент на месяц с доступом к тренажерному залу",
                        ImagePath = "/Images/iron1.jpg",
                        Duration = "1 месяц",
                        SubscriptionType = "Безлимит"
                    },
                    new Subscription
                    {
                        Id = 2,
                        Name = "Тренажерный зал - премиум",
                        Price = 850.00m,
                        Description = "Годовой абонемент с неограниченным доступом ко всем зонам клуба",
                        ImagePath = "/Images/iron1.jpg",
                        Duration = "12 месяцев",
                        SubscriptionType = "Обычный"
                    },
                    new Subscription
                    {
                        Id = 3,
                        Name = "Тренажерный зал - 1 смена",
                        Price = 60.00m,
                        Description = "Абонемент на утренние тренировки с 6:00 до 12:00",
                        ImagePath = "/Images/gym3.jpg",
                        Duration = "1 месяц",
                        SubscriptionType = "Обычный"
                    },
                    new Subscription
                    {
                        Id = 4,
                        Name = "Тренажерный зал – Персональные тренировки",
                        Price = 270.00m,
                        Description = "Абонемент на 10 индивидуальных занятий с тренером",
                        ImagePath = "/Images/gym2.jpg",
                        Duration = "10 занятий",
                        SubscriptionType = "Обычный"
                    },
                    new Subscription
                    {
                        Id = 5,
                        Name = "Тренажерный зал - групповые тренировки",
                        Price = 100.00m,
                        Description = "Абонемент на месяц с доступом к групповым тренировкам (йога, пилатес, зумба)",
                        ImagePath = "/Images/yoga.jpg",
                        Duration = "1 месяц",
                        SubscriptionType = "Безлимит"
                    }
                };
                
                context.Subscriptions.AddRange(subscriptions);
                context.SaveChanges();
                
                System.Diagnostics.Debug.WriteLine($"Созданы {subscriptions.Count} абонементов");
                MessageBox.Show($"Созданы {subscriptions.Count} абонементов",
                    "Абонементы созданы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при создании абонементов: {ex.Message}");
            }
        }

        private void CreateDefaultReviews(AppDbContext context)
        {
            try
            {
                var reviews = new List<Review>
                {
                    new Review
                    {
                        Id = 1,
                        SubscriptionId = 1,
                        User = "Иван",
                        Score = 5,
                        Comment = "Отличный зал, много оборудования!",
                        CreatedDate = new DateTime(2025, 5, 1, 22, 50, 30, 360)
                    },
                    new Review
                    {
                        Id = 3,
                        SubscriptionId = 2,
                        User = "Алексей",
                        Score = 5,
                        Comment = "Лучший клуб в городе!",
                        CreatedDate = new DateTime(2025, 5, 1, 22, 50, 30, 363)
                    },
                    new Review
                    {
                        Id = 5,
                        SubscriptionId = 3,
                        User = "Дмитрий",
                        Score = 4,
                        Comment = "Отличное время для занятий, мало людей.",
                        CreatedDate = new DateTime(2025, 5, 1, 22, 50, 30, 370)
                    },
                    new Review
                    {
                        Id = 6,
                        SubscriptionId = 4,
                        User = "Светлана",
                        Score = 5,
                        Comment = "Тренер профессионал, занятия очень полезные.",
                        CreatedDate = new DateTime(2025, 5, 1, 22, 50, 30, 373)
                    },
                    new Review
                    {
                        Id = 7,
                        SubscriptionId = 5,
                        User = "Елена",
                        Score = 5,
                        Comment = "Люблю групповые тренировки, тренеры супер!",
                        CreatedDate = new DateTime(2025, 5, 1, 22, 50, 30, 377)
                    },
                    new Review
                    {
                        Id = 8,
                        SubscriptionId = 5,
                        User = "Сергей",
                        Score = 4,
                        Comment = "Йога – класс!",
                        CreatedDate = new DateTime(2025, 5, 1, 22, 50, 30, 377)
                    }
                };
                
                context.Reviews.AddRange(reviews);
                context.SaveChanges();
                
                System.Diagnostics.Debug.WriteLine($"Созданы {reviews.Count} отзывов");
                MessageBox.Show($"Созданы {reviews.Count} отзывов",
                    "Отзывы созданы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при создании отзывов: {ex.Message}");
            }
        }

        private void CreateDefaultWorkoutPlans(AppDbContext context)
        {
            try
            {
                var workoutPlans = new List<WorkoutPlan>
                {
                    new WorkoutPlan
                    {
                        Id = 1,
                        ClientId = 3,
                        CoachId = 2,
                        Title = "Начальный уровень подготовки для Петров Петр Петрович",
                        Description = "Составлен тренером Иванов Иван Иванович. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 387),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 387),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 387),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 387)
                    },
                    new WorkoutPlan
                    {
                        Id = 2,
                        ClientId = 7,
                        CoachId = 2,
                        Title = "Функциональный тренинг для Анна Клиентова",
                        Description = "Составлен тренером Иванов Иван Иванович. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 390),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 390),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 390),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 390)
                    },
                    new WorkoutPlan
                    {
                        Id = 4,
                        ClientId = 9,
                        CoachId = 4,
                        Title = "Продвинутая программа для Елена Здоровая",
                        Description = "Составлен тренером Иван Тренеров. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 407),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 407),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 407),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 407)
                    },
                    new WorkoutPlan
                    {
                        Id = 5,
                        ClientId = 10,
                        CoachId = 4,
                        Title = "Силовые тренировки для Сергей Силин",
                        Description = "Составлен тренером Иван Тренеров. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 407),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 407),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 407),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 407)
                    },
                    new WorkoutPlan
                    {
                        Id = 6,
                        ClientId = 11,
                        CoachId = 4,
                        Title = "Кардио программа для Ольга Фитнесова",
                        Description = "Составлен тренером Иван Тренеров. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 410),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 410),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 410),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 410)
                    },
                    new WorkoutPlan
                    {
                        Id = 7,
                        ClientId = 12,
                        CoachId = 5,
                        Title = "Функциональный тренинг для Дмитрий Бегунов",
                        Description = "Составлен тренером Мария Инструкторова. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 413),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 413),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 413),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 413)
                    },
                    new WorkoutPlan
                    {
                        Id = 8,
                        ClientId = 13,
                        CoachId = 5,
                        Title = "Начальный уровень подготовки для Наталья Гибкая",
                        Description = "Составлен тренером Мария Инструкторова. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 413),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 413),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 413),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 413)
                    }
                };
                
                context.WorkoutPlans.AddRange(workoutPlans);
                context.SaveChanges();
                
                System.Diagnostics.Debug.WriteLine($"Созданы {workoutPlans.Count} планов тренировок");
                MessageBox.Show($"Созданы {workoutPlans.Count} планов тренировок",
                    "Планы тренировок созданы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при создании планов тренировок: {ex.Message}");
            }
        }

        private void CreateDefaultNutritionPlans(AppDbContext context)
        {
            try
            {
                var nutritionPlans = new List<NutritionPlan>
                {
                    new NutritionPlan
                    {
                        Id = 1,
                        ClientId = 3,
                        CoachId = 2,
                        Title = "Низкоуглеводная диета для Петров Петр Петрович",
                        Description = "Составлен тренером Иванов Иван Иванович. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 387),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 387),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 387),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 387)
                    },
                    new NutritionPlan
                    {
                        Id = 2,
                        ClientId = 7,
                        CoachId = 2,
                        Title = "Сбалансированный рацион для Анна Клиентова",
                        Description = "Составлен тренером Иванов Иван Иванович. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 390),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 390),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 390),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 390)
                    },
                    new NutritionPlan
                    {
                        Id = 4,
                        ClientId = 9,
                        CoachId = 4,
                        Title = "Белковая диета для Елена Здоровая",
                        Description = "Составлен тренером Иван Тренеров. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 403),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 403),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 403),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 403)
                    },
                    new NutritionPlan
                    {
                        Id = 5,
                        ClientId = 10,
                        CoachId = 4,
                        Title = "План питания для набора массы для Сергей Силин",
                        Description = "Составлен тренером Иван Тренеров. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 407),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 407),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 407),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 407)
                    },
                    new NutritionPlan
                    {
                        Id = 6,
                        ClientId = 11,
                        CoachId = 4,
                        Title = "План питания для похудения для Ольга Фитнесова",
                        Description = "Составлен тренером Иван Тренеров. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 407),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 407),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 407),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 407)
                    },
                    new NutritionPlan
                    {
                        Id = 7,
                        ClientId = 12,
                        CoachId = 5,
                        Title = "Сбалансированный рацион для Дмитрий Бегунов",
                        Description = "Составлен тренером Мария Инструкторова. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 410),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 410),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 410),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 410)
                    },
                    new NutritionPlan
                    {
                        Id = 8,
                        ClientId = 13,
                        CoachId = 5,
                        Title = "Низкоуглеводная диета для Наталья Гибкая",
                        Description = "Составлен тренером Мария Инструкторова. Период: 14.05.2025 - 14.06.2025",
                        CreatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 413),
                        UpdatedDate = new DateTime(2025, 5, 14, 13, 15, 20, 413),
                        StartDate = new DateTime(2025, 5, 14, 13, 15, 20, 413),
                        EndDate = new DateTime(2025, 6, 14, 13, 15, 20, 413)
                    }
                };
                
                context.NutritionPlans.AddRange(nutritionPlans);
                context.SaveChanges();
                
                System.Diagnostics.Debug.WriteLine($"Созданы {nutritionPlans.Count} планов питания");
                MessageBox.Show($"Созданы {nutritionPlans.Count} планов питания",
                    "Планы питания созданы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при создании планов питания: {ex.Message}");
            }
        }

        private void CreateDefaultCoachClients(AppDbContext context)
        {
            try
            {
                var coachClients = new List<CoachClient>
                {
                    new CoachClient
                    {
                        ClientId = 3,
                        CoachId = 2,
                        AssignedDate = new DateTime(2025, 5, 14, 13, 7, 11, 017)
                    },
                    new CoachClient
                    {
                        ClientId = 7,
                        CoachId = 2,
                        AssignedDate = new DateTime(2025, 5, 14, 13, 7, 11, 020)
                    },
                    new CoachClient
                    {
                        ClientId = 8,
                        CoachId = 2,
                        AssignedDate = new DateTime(2025, 5, 16, 14, 3, 17, 563)
                    },
                    new CoachClient
                    {
                        ClientId = 9,
                        CoachId = 4,
                        AssignedDate = new DateTime(2025, 5, 14, 13, 7, 11, 020)
                    },
                    new CoachClient
                    {
                        ClientId = 10,
                        CoachId = 4,
                        AssignedDate = new DateTime(2025, 5, 14, 13, 7, 11, 020)
                    },
                    new CoachClient
                    {
                        ClientId = 11,
                        CoachId = 4,
                        AssignedDate = new DateTime(2025, 5, 14, 13, 7, 11, 020)
                    },
                    new CoachClient
                    {
                        ClientId = 12,
                        CoachId = 5,
                        AssignedDate = new DateTime(2025, 5, 14, 13, 7, 11, 020)
                    },
                    new CoachClient
                    {
                        ClientId = 13,
                        CoachId = 5,
                        AssignedDate = new DateTime(2025, 5, 14, 13, 7, 11, 020)
                    }
                };
                
                context.CoachClients.AddRange(coachClients);
                context.SaveChanges();
                
                System.Diagnostics.Debug.WriteLine($"Созданы {coachClients.Count} связей тренер-клиент");
                MessageBox.Show($"Созданы {coachClients.Count} связей тренер-клиент",
                    "Связи тренер-клиент созданы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при создании связей тренер-клиент: {ex.Message}");
            }
        }
    }
} 