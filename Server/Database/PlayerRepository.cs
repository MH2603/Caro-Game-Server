using Server.GameLogic;
using Shared.GameLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Database
{
    public class SQLitePlayerRepository : IPlayerRepository, IDisposable
    {
        private readonly SQLiteDbContext _context;
        private CancellationTokenSource _cts;

        public SQLitePlayerRepository(SQLiteDbContext context)
        {
            _context = context;
            _cts = new CancellationTokenSource();

            
        }

        public async Task Init()
        {
            await InitPlayersTableAsync();
        }


        /// <summary>
        /// Initialize the Players table if it does not exist.
        /// </summary>
        public async Task InitPlayersTableAsync()
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync(_cts.Token);

            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                @"CREATE TABLE IF NOT EXISTS Players (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username    TEXT    NOT NULL UNIQUE,
                    Password    TEXT    NOT NULL,
                    CreatedDate TEXT    NOT NULL,
                    LastLoginDate TEXT NOT NULL
                  );";

            await cmd.ExecuteNonQueryAsync(_cts.Token);
        }

        public async Task AddAsync(PlayerData playerData)
        {
            using var conn = _context.CreateConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Players (Username, Password, CreatedDate, LastLoginDate ) VALUES (@Username, @Password, @CreatedDate, @LastLoginDate)";
            cmd.Parameters.AddWithValue("@Username", playerData.UserName);
            cmd.Parameters.AddWithValue("@Password", playerData.Password);
            cmd.Parameters.AddWithValue("@CreatedDate", playerData.CreatedDate.ToUniversalTime().ToString("O"));
            cmd.Parameters.AddWithValue("@LastLoginDate", playerData.LastLoginDate.ToUniversalTime().ToString("O"));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(PlayerData newData)
        {
            using var conn = _context.CreateConnection();
            conn.Open();

            var cmd = conn.CreateCommand();

        }

        public async Task<PlayerData?> GetByUsernameAsync(string username)
        {
            using var conn = _context.CreateConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, Level FROM Players WHERE Username=@u";
            cmd.Parameters.AddWithValue("@u", username);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new PlayerData(
                    //reader.GetString(1)
                );
            }

            return null;
        }

        public async Task<PlayerData[]> GetAll()
        {
            // List dùng để chứa toàn bộ player đọc được từ DB
            List<PlayerData> players = new List<PlayerData>();

            // Tạo và mở connection (using để đảm bảo Dispose)
            using var conn = _context.CreateConnection();
            await conn.OpenAsync(_cts.Token);

            // Tạo command
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Username, Password FROM Players";

            // ExecuteReaderAsync để đọc nhiều dòng
            using var reader = await cmd.ExecuteReaderAsync();

            // Đọc từng row trong result set
            while (await reader.ReadAsync(_cts.Token))
            {
                // Map dữ liệu từ DB → object
                PlayerData player = new PlayerData
                {
                    // GetInt32 / GetString nhanh hơn indexer object
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserName = reader.GetString(reader.GetOrdinal("Username")),
                    Password = reader.GetString(reader.GetOrdinal("Password"))
                };

                // Thêm vào danh sách
                players.Add(player);
            }

            // Trả về mảng (immutable hơn cho caller)
            return players.ToArray();
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }

}
