using MySqlConnector;

namespace Camera.Datebase
{
    public class DatebaseService
    {
        private readonly string _connectionString = "server=192.168.4.1;port=3306;database=traffic_analysis;user=root;password=123456;";

        // The constructor accepts the connection string directly
        public DatebaseService()
        {
            //_connectionString = connectionString;
        }

        // Getting a database connection
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}
