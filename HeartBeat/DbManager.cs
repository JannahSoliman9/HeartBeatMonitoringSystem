using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
namespace HeartBeat
{
    public class DbManager
    {
        private readonly string _connectionString;

        public DbManager()
        {
            _connectionString = "Server=Temp-Jannah\\HEARTBEAT1;Database=heartbeatUdp;User Id=sa;Password=1234;";
        }
        public void TestConnection()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    Console.WriteLine("Connection successful!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connection failed: " + ex.Message);
            }
        }
        public void LogAppHeartbeat(string appId, DateTime sentTime)
        {
            try
            {
                string query = "INSERT INTO AppsLogs (AppId, SentTime) VALUES (@AppId, @SentTime)";
                ExecuteNonQuery(query, new SqlParameter("@AppId", appId), new SqlParameter("@SentTime", sentTime));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging heartbeat: {ex.Message}");
                // Optionally, log the error to your logger
            }
        }


        public void LogReceivedMessage(string appId, DateTime receivedTime)
        {
            string query = "INSERT INTO WatcherLogs (AppId, ReceivedTime) VALUES (@AppId, @ReceivedTime)";
            ExecuteNonQuery(query, new SqlParameter("@AppId", appId), new SqlParameter("@ReceivedTime", receivedTime));
        }

        public void LogAppRecovery(string appId, DateTime restartTime)
        {
            string query = "INSERT INTO RecoveryLogs (AppId, RestartTime) VALUES (@AppId, @RestartTime)";
            ExecuteNonQuery(query, new SqlParameter("@AppId", appId), new SqlParameter("@RestartTime", restartTime));
        }

        private void ExecuteNonQuery(string query, params SqlParameter[] parameters)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param);
                        }
                        connection.Open();
                        command.ExecuteNonQuery();
                    }

                }

            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Error connecting to database : {ex.Message}");
            }
        }
    }
}
