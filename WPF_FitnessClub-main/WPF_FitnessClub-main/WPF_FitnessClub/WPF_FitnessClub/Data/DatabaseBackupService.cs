using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPF_FitnessClub.Models;
using Newtonsoft.Json;
using System.Diagnostics;

namespace WPF_FitnessClub.Data
{
    public class DatabaseBackupService
    {
        private readonly string _backupDirectory;
        private readonly string _backupFilePath;

        public DatabaseBackupService()
        {
            _backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
            _backupFilePath = Path.Combine(_backupDirectory, "database_backup.json");

            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
                Debug.WriteLine($"Создана директория для резервных копий: {_backupDirectory}");
            }
        }

        public bool BackupDatabaseToJson()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var backupData = new DatabaseBackupData
                    {
                        Users = context.Users.ToList(),
                        Subscriptions = context.Subscriptions.ToList(),
                        Reviews = context.Reviews.ToList(),
                        UserSubscriptions = context.UserSubscriptions.ToList(),
                        CoachClients = context.CoachClients.ToList(),
                        WorkoutPlans = context.WorkoutPlans.ToList(),
                        NutritionPlans = context.NutritionPlans.ToList()
                    };

                    var jsonSettings = new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        Formatting = Formatting.Indented
                    };

                    string json = JsonConvert.SerializeObject(backupData, jsonSettings);
                    File.WriteAllText(_backupFilePath, json, Encoding.UTF8);

                    Debug.WriteLine($"База данных успешно экспортирована в {_backupFilePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при экспорте базы данных в JSON: {ex.Message}");
                return false;
            }
        }

        public DatabaseBackupData RestoreDatabaseFromJson()
        {
            try
            {
                if (!File.Exists(_backupFilePath))
                {
                    Debug.WriteLine($"Файл резервной копии не найден: {_backupFilePath}");
                    return null;
                }

                string json = File.ReadAllText(_backupFilePath, Encoding.UTF8);
                var backupData = JsonConvert.DeserializeObject<DatabaseBackupData>(json);

                Debug.WriteLine($"База данных успешно импортирована из {_backupFilePath}");
                return backupData;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при импорте базы данных из JSON: {ex.Message}");
                return null;
            }
        }

        public string GetBackupFilePath()
        {
            return _backupFilePath;
        }

        public bool HasBackupFile()
        {
            bool exists = File.Exists(_backupFilePath);
            Debug.WriteLine($"Проверка наличия файла резервной копии: {_backupFilePath}, результат: {exists}");
            return exists;
        }
    }

    public class DatabaseBackupData
    {
        public List<User> Users { get; set; } = new List<User>();
        public List<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public List<Review> Reviews { get; set; } = new List<Review>();
        public List<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
        public List<CoachClient> CoachClients { get; set; } = new List<CoachClient>();
        public List<WorkoutPlan> WorkoutPlans { get; set; } = new List<WorkoutPlan>();
        public List<NutritionPlan> NutritionPlans { get; set; } = new List<NutritionPlan>();
    }
} 