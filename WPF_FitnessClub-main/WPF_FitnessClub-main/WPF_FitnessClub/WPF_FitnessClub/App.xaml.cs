using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Resources;
using System.Windows.Media.Imaging;
using WPF_FitnessClub.Models;
using WPF_FitnessClub.Data;
using WPF_FitnessClub.Properties;
using WPF_FitnessClub.View;
using WPF_FitnessClub.DataBase;

namespace WPF_FitnessClub
{
	/// <summary>
	/// Логика взаимодействия для App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			
			try
			{
				// Устанавливаем русскую культуру для UI
				Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ru-RU");
				
				/* Закомментируем автоматическое удаление базы данных
				// Удаляем базу данных, если она существует
				try
				{
					// Получаем строку подключения
					string connectionString = ConfigurationManager.ConnectionStrings["FitnessClubConnectionString"].ConnectionString;
					
					// Сохраняем данные перед удалением базы
					var backupService = new DatabaseBackupService();
					backupService.BackupDatabaseToJson();
					
					// Удаляем базу данных через прямое подключение к SQL Server
					DeleteDatabase(connectionString);
					
					System.Diagnostics.Debug.WriteLine("База данных успешно удалена");
				}
				catch (Exception dbEx)
				{
					System.Diagnostics.Debug.WriteLine($"Ошибка при удалении базы данных: {dbEx.Message}");
					// Продолжаем выполнение, даже если не удалось удалить базу
				}
				*/
				
				// Инициализация базы данных при запуске приложения
				var databaseInitializer = new DatabaseInitializer();
				
				System.Diagnostics.Debug.WriteLine("Запуск инициализации базы данных...");
				if (!databaseInitializer.InitializeDatabase())
				{
					System.Diagnostics.Debug.WriteLine("Инициализация базы данных не удалась");
					
					// Вместо просто закрытия приложения, предлагаем пользователю пересоздать базу данных
					MessageBoxResult result = MessageBox.Show(
						"Ошибка инициализации базы данных. Возможно, схема базы данных устарела. Хотите пересоздать базу данных?",
						"Ошибка инициализации", 
						MessageBoxButton.YesNo, 
						MessageBoxImage.Warning);
						
					if (result == MessageBoxResult.Yes)
					{
						// Если пользователь согласен, удаляем и пересоздаем базу данных
						try
						{
							// Получаем строку подключения
							string connectionString = ConfigurationManager.ConnectionStrings["FitnessClubConnectionString"].ConnectionString;
							
							// Сохраняем данные перед удалением базы
							var backupService = new DatabaseBackupService();
							backupService.BackupDatabaseToJson();
							
							// Удаляем базу данных через прямое подключение к SQL Server
							DeleteDatabase(connectionString);
							
							System.Diagnostics.Debug.WriteLine("База данных успешно удалена, попытка повторной инициализации");
							
							// Пробуем инициализировать БД заново
							if (!databaseInitializer.InitializeDatabase())
							{
								MessageBox.Show("Не удалось инициализировать базу данных даже после ее удаления. Приложение будет закрыто.",
									"Критическая ошибка", 
									MessageBoxButton.OK, 
									MessageBoxImage.Error);
								Shutdown();
								return;
							}
						}
						catch (Exception ex)
						{
							MessageBox.Show($"Произошла ошибка при пересоздании базы данных: {ex.Message}\n{ex.InnerException?.Message}",
								"Критическая ошибка", 
								MessageBoxButton.OK, 
								MessageBoxImage.Error);
							Shutdown();
							return;
						}
					}
					else
					{
						// Если пользователь отказался, закрываем приложение
						MessageBox.Show("Приложение будет закрыто из-за проблем с базой данных.",
							"Ошибка инициализации", 
							MessageBoxButton.OK, 
							MessageBoxImage.Error);
						Shutdown();
						return;
					}
				}

				System.Diagnostics.Debug.WriteLine("База данных успешно инициализирована");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Критическая ошибка при инициализации: {ex.Message}");
				if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Внутреннее исключение: {ex.InnerException.Message}");
                }
                
				MessageBox.Show($"Произошла ошибка при инициализации приложения: {ex.Message}\n{ex.InnerException?.Message}", 
					"Критическая ошибка", 
					MessageBoxButton.OK, 
					MessageBoxImage.Error);
				Shutdown();
				return;
			}

			// Открываем окно авторизации
			RegistrationView registrationView = new RegistrationView();
			registrationView.Show();
		}
		
		private void DeleteDatabase(string connectionString)
		{
			// Извлекаем имя базы данных из строки подключения
			SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
			string databaseName = builder.InitialCatalog;
			
			// Создаем строку подключения к master
			builder.InitialCatalog = "master";
			string masterConnectionString = builder.ConnectionString;
			
			using (SqlConnection connection = new SqlConnection(masterConnectionString))
			{
				connection.Open();
				
				try
				{
					// Проверяем существование базы данных
					SqlCommand checkCommand = new SqlCommand(
						$"SELECT COUNT(*) FROM sys.databases WHERE name = '{databaseName}'", 
						connection);
					int dbCount = (int)checkCommand.ExecuteScalar();
					
					if (dbCount > 0)
					{
						// Закрываем все соединения с базой данных
						SqlCommand killCommand = new SqlCommand(
							$"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", 
							connection);
						killCommand.ExecuteNonQuery();
						
						// Удаляем базу данных
						SqlCommand dropCommand = new SqlCommand(
							$"DROP DATABASE [{databaseName}]", 
							connection);
						dropCommand.ExecuteNonQuery();
						
						System.Diagnostics.Debug.WriteLine($"База данных {databaseName} успешно удалена");
					}
					else
					{
						System.Diagnostics.Debug.WriteLine($"База данных {databaseName} не существует");
					}
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Ошибка при удалении базы данных: {ex.Message}");
					throw;
				}
			}
		}
	}
}