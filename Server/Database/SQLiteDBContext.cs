using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
