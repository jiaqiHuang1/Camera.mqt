using MySqlConnector;

namespace Camera.Datebase
{
    public class DatebaseService
    {
        private readonly string _connectionString;

        // The constructor accepts the connection string directly
        public DatebaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Getting a database connection
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}
