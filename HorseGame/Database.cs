using System;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace QQBotCSharp.HorseGame
{
    public class Database
    {
        private readonly string _connectionString = "Data Source=horsegame.db";

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
        /// 检查玩家今天是否已签到
        /// </summary>
        public async Task<bool> IsSignedInTodayAsync(long groupUin, long userUin)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT last_sign_in_date FROM players WHERE group_uin = @groupUin AND user_uin = @userUin;";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@groupUin", groupUin);
                    command.Parameters.AddWithValue("@userUin", userUin);

                    var result = await command.ExecuteScalarAsync();
                    if (result == null) return false;

                    var lastSignInDate = DateTime.Parse(result.ToString());
                    return lastSignInDate.Date == DateTime.Today;
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
        /// 增加玩家积分
        /// </summary>
        public async Task AddPointsAsync(long groupUin, long userUin, int amount)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"
                    INSERT OR IGNORE INTO players (group_uin, user_uin, points)
                    VALUES (@groupUin, @userUin, 0);
                    UPDATE players
                    SET points = points + @amount
                    WHERE group_uin = @groupUin AND user_uin = @userUin;";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@groupUin", groupUin);
                    command.Parameters.AddWithValue("@userUin", userUin);
                    command.Parameters.AddWithValue("@amount", amount);

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
    }
}