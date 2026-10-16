using Dapper;
using Microsoft.Data.Sqlite;

namespace KoofrFolderNotifier
{
    internal sealed class Database
    {
        private readonly string _connectionString;

        public Database(string connectionString)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
            _connectionString = connectionString;
        }

        public SqliteConnection CreateConnection()
        {
            var connection = new SqliteConnection(_connectionString);

            try
            {
                connection.Open();
                connection.Execute("PRAGMA foreign_keys = ON;");
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }
    }
}
