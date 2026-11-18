using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace SmartHomeMaster
{
    public static class Db
    {
        public static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "smart_home.db");
        private static string ConnectionString => $"Data Source={FilePath}";

        public static SqliteConnection Open()
        {
            var c = new SqliteConnection(ConnectionString);
            c.Open();
            using var pragma = c.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
            return c;
        }

        private static void Exec(SqliteConnection c, string sql)
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        public static void Init()
        {
            using var c = Open();

            Exec(c, @"CREATE TABLE IF NOT EXISTS Rooms(
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL UNIQUE,
                        Floor INTEGER NOT NULL);");

            Exec(c, @"CREATE TABLE IF NOT EXISTS Devices(
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        RoomId INTEGER NOT NULL REFERENCES Rooms(Id),
                        Model TEXT NOT NULL,
                        Power REAL NOT NULL,
                        Type TEXT NOT NULL);");

            Exec(c, @"CREATE TABLE IF NOT EXISTS TelemetryLogs(
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        DeviceId INTEGER NOT NULL REFERENCES Devices(Id),
                        Reading REAL NOT NULL,
                        Timestamp TEXT NOT NULL);");

            using var count = c.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM Rooms;";
            if (Convert.ToInt32(count.ExecuteScalar()) > 0) return;

            // Начальные данные
            Exec(c, @"INSERT INTO Rooms(Title, Floor) VALUES
                        ('Гостиная',1),('Кухня',1),('Спальня',2),('Кабинет',2),('Гараж',0);");

            Exec(c, @"INSERT INTO Devices(RoomId, Model, Power, Type) VALUES
                        (1,'Thermo-1',5,'Sensor'),
                        (1,'Lamp-X',60,'Actuator'),
                        (1,'TV-4K',150,'Actuator'),
                        (2,'Thermo-1',5,'Sensor'),
                        (2,'Oven-Pro',2400,'Actuator'),
                        (2,'Lamp-X',60,'Actuator'),
                        (3,'Humid-2',8,'Sensor'),
                        (3,'Lamp-X',40,'Actuator'),
                        (4,'Motion-3',3,'Sensor'),
                        (4,'AC-Max',1800,'Actuator'),
                        (5,'Door-Lock',12,'Actuator'),
                        (5,'Motion-3',3,'Sensor');");

            Exec(c, @"INSERT INTO TelemetryLogs(DeviceId, Reading, Timestamp) VALUES
                        (1,21.5,'2026-11-18 08:00:00'),
                        (4,22.1,'2026-11-18 08:05:00'),
                        (7,45.0,'2026-11-18 08:10:00');");
        }
    }

    public class RoomRepository : IRepository<Room>
    {
        public Room GetById(int id)
        {
            using var c = Db.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, Floor FROM Rooms WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? new Room { Id = r.GetInt32(0), Title = r.GetString(1), Floor = r.GetInt32(2) } : null;
        }

        public List<Room> GetAll()
        {
            var list = new List<Room>();
            using var c = Db.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id, Title, Floor FROM Rooms ORDER BY Id;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new Room { Id = r.GetInt32(0), Title = r.GetString(1), Floor = r.GetInt32(2) });
            return list;
        }

        public void Add(Room room)
        {
            using var c = Db.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO Rooms(Title, Floor) VALUES($t, $f);";
            cmd.Parameters.AddWithValue("$t", room.Title);
            cmd.Parameters.AddWithValue("$f", room.Floor);
            cmd.ExecuteNonQuery();
        }

        public Task<List<Room>> GetAllAsync() => Task.Run(GetAll);
    }

    public class DeviceRepository : IRepository<Device>
    {
        private static Device Map(SqliteDataReader r) =>
            DeviceFactory.Create(
                r.GetInt32(0),          // Id
                r.GetString(2),         // Model
                r.GetDouble(3),         // Power
                r.GetInt32(1),          // RoomId
                r.GetString(4));        // Type

        public Device GetById(int id)
        {
            using var c = Db.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id, RoomId, Model, Power, Type FROM Devices WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? Map(r) : null;
        }

        public List<Device> GetAll()
        {
            var list = new List<Device>();
            using var c = Db.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id, RoomId, Model, Power, Type FROM Devices ORDER BY Id;";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(Map(r));
            return list;
        }

        public void Add(Device d)
        {
            using var c = Db.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO Devices(RoomId, Model, Power, Type) VALUES($r, $m, $p, $t);";
            cmd.Parameters.AddWithValue("$r", d.RoomId);
            cmd.Parameters.AddWithValue("$m", d.Model);
            cmd.Parameters.AddWithValue("$p", d.Power);
            cmd.Parameters.AddWithValue("$t", d.Type);
            cmd.ExecuteNonQuery();
        }

        // Асинхронное чтение — UI-поток не блокируется
        public Task<List<Device>> GetAllAsync() => Task.Run(GetAll);
    }

    public class TelemetryRepository : IRepository<TelemetryLog>
    {
        public TelemetryLog GetById(int id)
        {
            using var c = Db.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id, DeviceId, Reading, Timestamp FROM TelemetryLogs WHERE Id = $id;";
            cmd.Parameters.AddWithValue("$id", id);
            using var r = cmd.ExecuteReader();
            return r.Read() ? Map(r) : null;
        }

        public List<TelemetryLog> GetAll()
        {
            var list = new List<TelemetryLog>();
            using var c = Db.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT Id, DeviceId, Reading, Timestamp FROM TelemetryLogs ORDER BY Id;";
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(Map(r));
            return list;
        }

        public void Add(TelemetryLog log)
        {
            using var c = Db.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO TelemetryLogs(DeviceId, Reading, Timestamp) VALUES($d, $r, $t);";
            cmd.Parameters.AddWithValue("$d", log.DeviceId);
            cmd.Parameters.AddWithValue("$r", log.Reading);
            cmd.Parameters.AddWithValue("$t", log.Timestamp);
            cmd.ExecuteNonQuery();
        }

        private static TelemetryLog Map(SqliteDataReader r) => new TelemetryLog
        {
            Id = r.GetInt32(0),
            DeviceId = r.GetInt32(1),
            Reading = r.GetDouble(2),
            Timestamp = r.GetString(3)
        };
    }
}
