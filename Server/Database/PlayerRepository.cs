using Dapper;
using Dapper.Contrib;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Server.GameLogic;
using Shared.GameLogic;

namespace Server.Database
{


    public class SQLitePlayerRepository : IPlayerRepository, IDisposable
    {
        private const string PlayerTable = "Players";

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
        /// Adds WinMatch and LostMatch columns to existing Players table if they don't exist.
        /// </summary>
        private async Task AddWinMatchLostMatchColumnsIfNeededAsync(SqliteConnection conn)
        {
            var columns = await conn.QueryAsync<string>(
                "SELECT name FROM pragma_table_info('Players') WHERE name IN ('WinMatch', 'LostMatch')");
            var existing = columns.ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!existing.Contains("WinMatch"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "ALTER TABLE Players ADD COLUMN WinMatch INTEGER NOT NULL DEFAULT 0";
                await cmd.ExecuteNonQueryAsync(_cts.Token);
            }
            if (!existing.Contains("LostMatch"))
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "ALTER TABLE Players ADD COLUMN LostMatch INTEGER NOT NULL DEFAULT 0";
                await cmd.ExecuteNonQueryAsync(_cts.Token);
            }
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
                    LastLoginDate TEXT NOT NULL,
                    WinMatch    INTEGER NOT NULL DEFAULT 0,
                    LostMatch   INTEGER NOT NULL DEFAULT 0
                  );";

            await cmd.ExecuteNonQueryAsync(_cts.Token);

            await AddWinMatchLostMatchColumnsIfNeededAsync(conn);
        }

        public async Task AddAsync(PlayerData playerData)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync();

            await conn.InsertAsync<PlayerData>(playerData);
        }

        public async Task UpdateAsync(PlayerData newData)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync(_cts.Token);

            await conn.UpdateAsync(newData);    
        }

        public async Task<PlayerData?> GetByUsernameAsync(string username)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync(_cts.Token);

            var sql = $"SELECT * FROM {PlayerTable} WHERE Username = @Username";

            var data = await conn.QueryFirstOrDefaultAsync<PlayerData>(sql, new { Username = username });

            return data;
        }

       
        public async Task<PlayerData[]> GetAllAsync()
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync(_cts.Token);

            var datas =  await conn.QueryAsync<PlayerData>($"SELECT * FROM {PlayerTable}");

            return datas.ToArray();

        }

        /// <summary>
        /// Updates WinMatch and LostMatch stats for a player.
        /// </summary>
        public async Task UpdateMatchStatsAsync(int playerId, int winMatch, int lostMatch)
        {
            using var conn = _context.CreateConnection();
            await conn.OpenAsync(_cts.Token);

            var sql = $"UPDATE {PlayerTable} SET WinMatch = @WinMatch, LostMatch = @LostMatch WHERE Id = @Id";
            await conn.ExecuteAsync(sql, new { Id = playerId, WinMatch = winMatch, LostMatch = lostMatch });
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }

}
