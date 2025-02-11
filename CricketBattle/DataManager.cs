namespace QQBotCSharp.CricketBattle;
using Lagrange.Core.Common.Interface.Api;

using System.Data.SQLite;
using System.IO;
using System;
using Lagrange.Core;
using System.Threading.Tasks;

public class DatabaseManager
{
    private const string DbFileName = "cricket_game.db";
    private const string ConnectionString = $"Data Source={DbFileName};Version=3;";

    public DatabaseManager()
    {
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        if (!File.Exists(DbFileName))
        {
            SQLiteConnection.CreateFile(DbFileName);
            using (var connection = GetConnection())
            {
                connection.Open();
                string sql = @"
                        CREATE TABLE IF NOT EXISTS PlayerPoints (
                            ID  PRIMARY KEY,
                            Uin TEXT,  -- 数据库中仍然使用 TEXT 类型
                            GroupUin TEXT,       -- 数据库中仍然使用 TEXT 类型
                            Points INTEGER NOT NULL DEFAULT 0,
                            CheckInDate TEXT
                        );";
                SQLiteCommand command = new SQLiteCommand(sql, connection);
                command.ExecuteNonQuery();
            }
        }
    }

    private SQLiteConnection GetConnection()
    {
        return new SQLiteConnection(ConnectionString);
    }

    public int GetUserPoints(uint groupUin, uint uin) // 修改参数类型为 uint
    {
        using (var connection = GetConnection())
        {
            connection.Open();
            string sql = "SELECT Points FROM PlayerPoints WHERE Uin = @Uin AND GroupUin=@GroupUin";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Uin", uin.ToString()); // uint 转换为 string 存入数据库
            command.Parameters.AddWithValue("@GroupUin", groupUin.ToString());
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return Convert.ToInt32(reader["Points"]);
                }
                else
                {
                    // 用户不存在，创建新用户并返回默认积分
                    CreateNewUser(uin, 0); // GroupUin 可以暂时为 0
                    return 0;
                }
            }
        }
    }

    public void CreateNewUser(uint uin, uint groupUin) // 修改参数类型为 uint
    {
        using (var connection = GetConnection())
        {
            connection.Open();
            string sql = "INSERT INTO PlayerPoints (Uin, GroupUin, Points, CheckInDate) VALUES (@Uin, @GroupUin, 0, null)";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Uin", uin.ToString()); // uint 转换为 string 存入数据库
            command.Parameters.AddWithValue("@GroupUin", groupUin.ToString()); // uint 转换为 string 存入数据库
            command.ExecuteNonQuery();
        }
    }

    public async Task<List<Tuple<uint, string, int>>> GetAllUsersInGroupAsync(BotContext _context, uint groupUin)
    {
        var groupMembers = await _context.FetchMembers(groupUin, true);
        var uinNames = new Dictionary<uint, string>();
        foreach (var m in groupMembers)
        {
            uinNames[m.Uin] = m.MemberCard ?? m.MemberName;
        }
        List<Tuple<uint, string, int>> userList = new List<Tuple<uint, string, int>>();
        using (var connection = GetConnection())
        {
            connection.Open();
            string sql = @"SELECT Uin, Points FROM PlayerPoints WHERE GroupUin = @GroupUin ORDER BY Points DESC";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@GroupUin", groupUin.ToString());
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    uint uin = uint.Parse(reader["Uin"].ToString());
                    string name = uinNames[uin];
                    int points = Convert.ToInt32(reader["Points"]);
                    userList.Add(new Tuple<uint, string, int>(uin, name, points));
                }
            }
        }
        return userList;
    }


    public void UpdateUserPoints(uint groupUin, uint uin, int pointsToAdd) // 修改参数类型为 uint
    {
        using (var connection = GetConnection())
        {
            connection.Open();
            string sql = "UPDATE PlayerPoints SET Points = Points + @PointsToAdd WHERE Uin = @Uin AND GroupUin=@GroupUin";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@PointsToAdd", pointsToAdd);
            command.Parameters.AddWithValue("@Uin", uin.ToString()); // uint 转换为 string 用于查询
            command.Parameters.AddWithValue("@GroupUin", groupUin.ToString());
            command.ExecuteNonQuery();
        }
    }

    public bool CanCheckInToday(uint groupUin, uint uin) // 修改参数类型为 uint
    {
        using (var connection = GetConnection())
        {
            connection.Open();
            string sql = "SELECT CheckInDate FROM PlayerPoints WHERE Uin = @Uin AND GroupUin=@GroupUin";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Uin", uin.ToString()); // uint 转换为 string 用于查询
            command.Parameters.AddWithValue("@GroupUin", groupUin.ToString());
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    string checkInDate = reader["CheckInDate"] as string;
                    if (string.IsNullOrEmpty(checkInDate)) return true; // 从未签到过
                    DateTime lastCheckInDate;
                    if (DateTime.TryParse(checkInDate, out lastCheckInDate))
                    {
                        // 使用北京时间进行比较
                        TimeZoneInfo chinaZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                        DateTime chinaNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaZone);

                        return lastCheckInDate.Date < chinaNow.Date; // 检查是否是不同一天
                    }
                    return true; // 解析失败，也允许签到
                }
                return true; // 用户不存在，允许签到 (首次签到)
            }
        }
    }

    public void UpdateCheckInDate(uint groupUin, uint uin) // 修改参数类型为 uint
    {
        using (var connection = GetConnection())
        {
            connection.Open();
            // 使用北京时间记录签到日期
            TimeZoneInfo chinaZone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            DateTime chinaNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, chinaZone);
            string todayDate = chinaNow.ToString("yyyy-MM-dd");

            string sql = "UPDATE PlayerPoints SET CheckInDate = @CheckInDate WHERE Uin = @Uin AND GroupUin=@GroupUin";
            SQLiteCommand command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@CheckInDate", todayDate);
            command.Parameters.AddWithValue("@Uin", uin.ToString()); // uint 转换为 string 用于查询
            command.Parameters.AddWithValue("@GroupUin", groupUin.ToString());
            command.ExecuteNonQuery();
        }
    }
}