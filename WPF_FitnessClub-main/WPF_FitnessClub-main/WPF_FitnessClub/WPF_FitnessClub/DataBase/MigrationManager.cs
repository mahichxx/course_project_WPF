using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Reflection;
using System.Windows;
using WPF_FitnessClub.Data;
using WPF_FitnessClub.Models;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace WPF_FitnessClub.DataBase
{
    public static class MigrationManager
    {
        public static void ApplyMigrations(AppDbContext context)
        {
            try
            {
                bool allTablesExist = true;
                allTablesExist &= CheckIfTableExists(context, "Users");
                allTablesExist &= CheckIfTableExists(context, "Subscriptions");
                allTablesExist &= CheckIfTableExists(context, "Reviews");
                allTablesExist &= CheckIfTableExists(context, "CoachClients");
                allTablesExist &= CheckIfTableExists(context, "UserSubscriptions");
                
                if (CheckIfTableExists(context, "Users") && !allTablesExist)
                {
                    throw new Exception("База данных повреждена: обнаружены не все необходимые таблицы");
                }
                
                ApplyTableMigration(context, "WorkoutPlans", @"CREATE TABLE [dbo].[WorkoutPlans] (
                    [Id] INT IDENTITY(1,1) NOT NULL,
                    [ClientId] INT NOT NULL,
                    [CoachId] INT NOT NULL,
                    [Title] NVARCHAR(100) NOT NULL,
                    [Description] NVARCHAR(500) NULL,
                    [CreatedDate] DATETIME NOT NULL,
                    [UpdatedDate] DATETIME NOT NULL,
                    [StartDate] DATETIME NOT NULL,
                    [EndDate] DATETIME NOT NULL,
                    [IsCompleted] BIT NOT NULL DEFAULT(0),
                    CONSTRAINT [PK_WorkoutPlans] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_WorkoutPlans_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Users] ([Id]),
                    CONSTRAINT [FK_WorkoutPlans_Users_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [dbo].[Users] ([Id])
                )", "IsCompleted");

                ApplyTableMigration(context, "NutritionPlans", @"CREATE TABLE [dbo].[NutritionPlans] (
                    [Id] INT IDENTITY(1,1) NOT NULL,
                    [ClientId] INT NOT NULL,
                    [CoachId] INT NOT NULL,
                    [Title] NVARCHAR(100) NOT NULL,
                    [Description] NVARCHAR(500) NULL,
                    [CreatedDate] DATETIME NOT NULL,
                    [UpdatedDate] DATETIME NOT NULL,
                    [StartDate] DATETIME NOT NULL,
                    [EndDate] DATETIME NOT NULL,
                    [IsCompleted] BIT NOT NULL DEFAULT(0),
                    CONSTRAINT [PK_NutritionPlans] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_NutritionPlans_Users_ClientId] FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Users] ([Id]),
                    CONSTRAINT [FK_NutritionPlans_Users_CoachId] FOREIGN KEY ([CoachId]) REFERENCES [dbo].[Users] ([Id])
                )", "IsCompleted");

                ApplyTableMigration(context, "TrainingPlans", @"CREATE TABLE [dbo].[TrainingPlans] (
                    [Id] INT IDENTITY(1,1) NOT NULL,
                    [UserId] INT NOT NULL,
                    [TrainerId] INT NOT NULL,
                    [Name] NVARCHAR(100) NOT NULL,
                    [StartDate] DATETIME NOT NULL,
                    [EndDate] DATETIME NOT NULL,
                    [Content] NVARCHAR(MAX) NOT NULL,
                    [IsCompleted] BIT NOT NULL DEFAULT(0),
                    CONSTRAINT [PK_TrainingPlans] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_TrainingPlans_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id]),
                    CONSTRAINT [FK_TrainingPlans_Users_TrainerId] FOREIGN KEY ([TrainerId]) REFERENCES [dbo].[Users] ([Id])
                )", "IsCompleted");
                
                System.Diagnostics.Debug.WriteLine("Миграции успешно применены");
            }
            catch (Exception ex)
            {
                File.AppendAllText("migration_error.log", $"{DateTime.Now}: {ex.Message}\n{ex.StackTrace}\n\n");
                MessageBox.Show($"Ошибка при применении миграций: {ex.Message}", 
                    "Ошибка миграции", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                throw;
            }
        }
        
        private static void ApplyTableMigration(AppDbContext context, string tableName, string createTableSQL, string requiredColumn)
        {
            try
            {
                if (!CheckIfTableExists(context, tableName))
                {
                    context.Database.ExecuteSqlCommand(createTableSQL);
                    System.Diagnostics.Debug.WriteLine($"Таблица {tableName} создана");
                }
                else if (!CheckIfColumnExists(context, tableName, requiredColumn))
                {
                    context.Database.ExecuteSqlCommand(
                        $"ALTER TABLE [dbo].[{tableName}] ADD [{requiredColumn}] BIT NOT NULL DEFAULT(0)");
                    
                    System.Diagnostics.Debug.WriteLine($"Колонка {requiredColumn} добавлена в таблицу {tableName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Таблица {tableName} уже существует и содержит все необходимые колонки");
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("migration_error.log", $"{DateTime.Now}: Ошибка при миграции таблицы {tableName}: {ex.Message}\n{ex.StackTrace}\n\n");
                
                if (ex.Message.Contains("There is already an object named"))
                {
                    System.Diagnostics.Debug.WriteLine($"Таблица {tableName} уже существует (игнорируем ошибку)");
                    return;       
                }
                
                throw;    
            }
        }
        
        private static bool CheckIfTableExists(AppDbContext context, string tableName)
        {
            var result = context.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName",
                new SqlParameter("@tableName", tableName)).FirstOrDefault();
            return result > 0;
        }

        private static bool CheckIfColumnExists(AppDbContext context, string tableName, string columnName)
        {
            var result = context.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName",
                new SqlParameter("@tableName", tableName),
                new SqlParameter("@columnName", columnName)).FirstOrDefault();
            return result > 0;
        }
    }
    
    public sealed class Configuration : DbMigrationsConfiguration<AppDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;       
        }

        protected override void Seed(AppDbContext context)
        {
            base.Seed(context);
        }
    }
} 