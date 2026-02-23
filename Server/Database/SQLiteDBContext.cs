using Microsoft.Data.Sqlite;

namespace Server.Database
{
    public class SQLiteDbContext
    {
        public string ConnectionString { get; }

        public SQLiteDbContext(string path)
        {
            ConnectionString = $"Data Source={path}";
        }

        public SqliteConnection CreateConnection()
        {
            return new SqliteConnection(ConnectionString);
        }
    }

}
