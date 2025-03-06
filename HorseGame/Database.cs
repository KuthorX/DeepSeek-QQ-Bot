using System;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace QQBotCSharp.HorseGame
{
    public class Database : IDisposable
    {
        private bool _disposed = false;
        private SQLiteConnection? _connection = null;
        private readonly string _connectionString = "Data Source=horsegame.db";

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _connection?.Dispose();
                }
                _disposed = true;
            }
        }

        ~Database()
        {
            Dispose(false);
        }

        public Database()
        {
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // 创建玩家数据表
                var createPlayersTable = @"
                    CREATE TABLE IF NOT EXISTS players (
                        group_uin INTEGER NOT NULL,
                        user_uin INTEGER NOT NULL,
                        points INTEGER DEFAULT 0,
                        last_sign_in_date TEXT,
                        level INTEGER DEFAULT 1,
                        PRIMARY KEY (group_uin, user_uin)
                    );";
                using (var command = new SQLiteCommand(createPlayersTable, connection))
                {
                    command.ExecuteNonQuery();
                }

                // 创建下注记录表
                var createBetsTable = @"
                    CREATE TABLE IF NOT EXISTS bets (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        group_uin INTEGER NOT NULL,
                        user_uin INTEGER NOT NULL,
                        horse_id INTEGER NOT NULL,
                        amount INTEGER NOT NULL,
                        bet_time TEXT DEFAULT (datetime('now', 'localtime'))
                    );";
                using (var command = new SQLiteCommand(createBetsTable, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// 获取玩家积分
        /// </summary>
        public async Task<int> GetPlayerPointsAsync(long groupUin, long userUin)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT points FROM players WHERE group_uin = @groupUin AND user_uin = @userUin;";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@groupUin", groupUin);
                    command.Parameters.AddWithValue("@userUin", userUin);

                    var result = await command.ExecuteScalarAsync();
                    return result == null ? 0 : Convert.ToInt32(result);
                }
            }
        }

        /// <summary>
        /// 获取玩家等级
        /// </summary>
        public async Task<int> GetPlayerLevelAsync(long groupUin, long userUin)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT level FROM players WHERE group_uin = @groupUin AND user_uin = @userUin;";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@groupUin", groupUin);
                    command.Parameters.AddWithValue("@userUin", userUin);

                    var result = await command.ExecuteScalarAsync();
                    return result == null ? 1 : Convert.ToInt32(result);
                }
            }
        }

        /// <summary>
        /// 获取玩家信息
        /// </summary>
        public async Task<(int Points, int Level)> GetPlayerInfoAsync(long groupUin, long userUin)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT points, level FROM players WHERE group_uin = @groupUin AND user_uin = @userUin;";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@groupUin", groupUin);
                    command.Parameters.AddWithValue("@userUin", userUin);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return (reader.GetInt32(0), reader.GetInt32(1));
                        }
                        return (0, 1); // 默认值
                    }
                }
            }
        }

        /// <summary>
        /// 检查玩家今天是否已签到
        /// </summary>
        public async Task<bool> IsSignedInTodayAsync(long groupUin, long userUin)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    SELECT last_sign_in_date FROM players WHERE group_uin = @groupUin AND user_uin = @userUin;";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@groupUin", groupUin);
                    command.Parameters.AddWithValue("@userUin", userUin);

                    var result = await command.ExecuteScalarAsync();
                    if (result == null) return false;
                    
                    var resultStr = result.ToString();
                    if (string.IsNullOrEmpty(resultStr) || resultStr == "{}")
                        return false;
                        
                    if (DateTime.TryParse(resultStr, out var lastSignInDate))
                        return lastSignInDate.Date == DateTime.Today;
                    
                    return false;
                }
            }
        }

        /// <summary>
        /// 更新玩家签到日期并赠送积分
        /// </summary>
        public async Task UpdateSignInAsync(long groupUin, long userUin)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    INSERT OR IGNORE INTO players (group_uin, user_uin, points, last_sign_in_date)
                    VALUES (@groupUin, @userUin, 0, @today);
                    UPDATE players
                    SET last_sign_in_date = @today,
                        points = points + 1000
                    WHERE group_uin = @groupUin AND user_uin = @userUin;";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@groupUin", groupUin);
                    command.Parameters.AddWithValue("@userUin", userUin);
                    command.Parameters.AddWithValue("@today", DateTime.Today.ToString("yyyy-MM-dd"));

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 扣除玩家积分
        /// </summary>
        public async Task<bool> DeductPointsAsync(long groupUin, long userUin, int amount)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    UPDATE players
                    SET points = points - @amount
                    WHERE group_uin = @groupUin AND user_uin = @userUin AND points >= @amount;";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@groupUin", groupUin);
                    command.Parameters.AddWithValue("@userUin", userUin);
                    command.Parameters.AddWithValue("@amount", amount);

                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        /// <summary>
        /// 更新玩家等级
        /// </summary>
        public async Task UpdatePlayerLevelAsync(long groupUin, long userUin, int newLevel)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    UPDATE players
                    SET level = @newLevel
                    WHERE group_uin = @groupUin AND user_uin = @userUin;";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@groupUin", groupUin);
                    command.Parameters.AddWithValue("@userUin", userUin);
                    command.Parameters.AddWithValue("@newLevel", newLevel);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 增加玩家积分，处理积分上限和自动升级
        /// </summary>
        public async Task AddPointsAsync(long groupUin, long userUin, int amount)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var (currentPoints, currentLevel) = await GetPlayerInfoAsync(groupUin, userUin);
                var newPoints = currentPoints + amount;

                // 如果超过积分上限，自动升级
                while (newPoints > Models.Player.MaxPoints)
                {
                    if (newPoints >= Models.Player.LevelUpCost)
                    {
                        newPoints -= Models.Player.LevelUpCost;
                        currentLevel++;
                    }
                    else
                    {
                        break;
                    }
                }

                var query = @"
                    INSERT OR IGNORE INTO players (group_uin, user_uin, points, level)
                    VALUES (@groupUin, @userUin, 0, 1);
                    UPDATE players
                    SET points = @newPoints,
                        level = @newLevel
                    WHERE group_uin = @groupUin AND user_uin = @userUin;";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@groupUin", groupUin);
                    command.Parameters.AddWithValue("@userUin", userUin);
                    command.Parameters.AddWithValue("@newPoints", newPoints);
                    command.Parameters.AddWithValue("@newLevel", currentLevel);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 记录下注信息
        /// </summary>
        public async Task RecordBetAsync(long groupUin, long userUin, int horseId, int amount)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    INSERT INTO bets (group_uin, user_uin, horse_id, amount)
                    VALUES (@groupUin, @userUin, @horseId, @amount);";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@groupUin", groupUin);
                    command.Parameters.AddWithValue("@userUin", userUin);
                    command.Parameters.AddWithValue("@horseId", horseId);
                    command.Parameters.AddWithValue("@amount", amount);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        /// <summary>
        /// 获取本群群友积分排名
        /// </summary>
        public async Task<List<(uint UserUin, int Points)>> GetGroupMemberRankingAsync(uint groupUin)
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            var query = @"
                    SELECT user_uin, points
                    FROM players
                    WHERE group_uin = @groupUin
                    ORDER BY points DESC;";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@groupUin", groupUin);

            var reader = await command.ExecuteReaderAsync();
            var ranking = new List<(uint UserUin, int Points)>();

            while (await reader.ReadAsync())
            {
                var userUin = reader.GetInt32(0);
                var points = reader.GetInt32(1);
                ranking.Add(((uint UserUin, int Points))(userUin, points));
            }

            return ranking;
        }
    }
}