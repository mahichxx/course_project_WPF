using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Threading;

namespace WPF_FitnessClub.Data
{
    public class DatabaseConnectionService
    {
        private readonly string _connectionString;

        public DatabaseConnectionService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool CheckConnection()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch (SqlException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool IsDatabaseExists()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    return context.Database.Exists();
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool CheckConnectionWithRetry(int retryCount = 3, int retryIntervalMilliseconds = 1000)
        {
            for (int i = 0; i < retryCount; i++)
            {
                if (CheckConnection())
                {
                    return true;
                }

                Thread.Sleep(retryIntervalMilliseconds);
            }

            return false;
        }
    }
} 