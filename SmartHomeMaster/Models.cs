using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartHomeMaster
{
    // ===== Модуль 2: интерфейсы =====
    public interface IExportable
    {
        string GetExportData();
    }

    public interface IRepository<T> where T : class
    {
        T GetById(int id);
        List<T> GetAll();
        void Add(T entity);
    }

    // ===== Модуль 3: пользовательские аргументы события =====
    public class DeviceEventArgs : EventArgs
    {
        public string Message { get; }
        public DateTime Timestamp { get; }

        public DeviceEventArgs(string message, DateTime timestamp)
        {
            Message = message;
            Timestamp = timestamp;
        }
    }

    // Вложенный объект калибровки/паспорта (нужен для Deep Copy)
    public class CalibrationInfo : ICloneable
    {
        public string PassportNumber { get; set; }
        public DateTime CalibratedOn { get; set; }

        public object Clone() => new CalibrationInfo
        {
            PassportNumber = PassportNumber,
            CalibratedOn = CalibratedOn
        };

        public override string ToString() => $"{PassportNumber} ({CalibratedOn:d})";
    }

    public class Room
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Floor { get; set; }
        public override string ToString() => $"{Title} (этаж {Floor})";
    }

    public class TelemetryLog
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public double Reading { get; set; }
        public string Timestamp { get; set; }
    }

    public class DeviceReportDto
    {
        public int DeviceId { get; set; }
        public string Model { get; set; }
        public string RoomTitle { get; set; }
        public int Floor { get; set; }
        public double Power { get; set; }
    }

    public class GroupStat
    {
        public string Room { get; set; }
        public int Count { get; set; }
        public double Total { get; set; }
        public double Average { get; set; }
        public string Models { get; set; }
    }

    // ===== Модуль 1: базовый абстрактный класс =====
    public abstract class Device : IExportable, IComparable<Device>, ICloneable
    {
        public int Id { get; private set; }
        public string Model { get; private set; }
        public double Power { get; private set; }

        public int RoomId { get; set; }
        public string Type { get; set; }
        public CalibrationInfo Calibration { get; set; }

        protected bool _isActive;
        public bool IsActive => _isActive;

        // Событие (Модуль 3)
        public event EventHandler<DeviceEventArgs> StatusChanged;

        protected Device(int id, string model, double power, int roomId, string type)
        {
            Id = id;
            Model = model;
            Power = power;
            RoomId = roomId;
            Type = type;
            Calibration = new CalibrationInfo
            {
                PassportNumber = $"PSP-{id:D4}",
                CalibratedOn = DateTime.Today
            };
        }

        public virtual string GetStatus() => _isActive ? "Включено" : "Выключено";

        public abstract double CalculatePowerConsumption();

        // Метод, который будет скрыт через new в наследнике (shadowing)
        public string Describe() => $"Устройство {Model}";

        public void SetActive(bool active)
        {
            _isActive = active;
            // Безопасный вызов события
            StatusChanged?.Invoke(this, new DeviceEventArgs(
                $"{Model}: {(active ? "ВКЛ" : "ВЫКЛ")}", DateTime.Now));
        }

        // ----- Переопределение System.Object -----
        public override string ToString() =>
            $"[{GetType().Name}] #{Id} {Model}, {Power:F1} Вт, {GetStatus()}";

        public override bool Equals(object obj) =>
            obj is Device d && d.GetType() == GetType() && d.Id == Id && d.Model == Model;

        public override int GetHashCode() => HashCode.Combine(Id, Model);

        // ----- Явная реализация интерфейса -----
        string IExportable.GetExportData() =>
            $"{Id};{Model};{Power:F1};{Type};{GetStatus()}";

        // ----- IComparable<Device>: сортировка по мощности -----
        public int CompareTo(Device other)
        {
            if (other is null) return 1;
            return Power.CompareTo(other.Power);
        }

        // ----- ICloneable: ГЛУБОКОЕ копирование -----
        public object Clone()
        {
            var copy = (Device)MemberwiseClone();          // копия полей-значений
            copy.Calibration = (CalibrationInfo)Calibration.Clone(); // вложенный объект дублируется
            copy.StatusChanged = null;                      // подписчики оригинала не переносим
            return copy;
        }
    }

    public class SmartSensor : Device
    {
        public double Accuracy { get; set; }

        public SmartSensor(int id, string model, double power, int roomId, string type, double accuracy = 0.95)
            : base(id, model, power, roomId, type)
        {
            Accuracy = accuracy;
        }

        public override double CalculatePowerConsumption() => Power * 0.1;

        public override string GetStatus() => base.GetStatus() + $" (точность {Accuracy:P0})";

        // Скрытие члена (shadowing)
        public new string Describe() => $"Датчик {Model}, точность {Accuracy:P0}";

        // Асинхронное считывание показаний (Модуль 5)
        public async Task<double> ReadAsync()
        {
            await Task.Delay(300 + Random.Shared.Next(0, 700));
            return Math.Round(15 + Random.Shared.NextDouble() * 15, 2);
        }
    }

    public class SmartActuator : Device
    {
        public double LoadCapacity { get; set; }

        public SmartActuator(int id, string model, double power, int roomId, string type, double loadCapacity = 0)
            : base(id, model, power, roomId, type)
        {
            LoadCapacity = loadCapacity > 0 ? loadCapacity : power * 1.5;
        }

        public override double CalculatePowerConsumption() => Power * (_isActive ? 1.0 : 0.05);

        public override string GetStatus() => base.GetStatus() + $" (нагрузка до {LoadCapacity:F0})";

        public new string Describe() => $"Исполнитель {Model}, нагрузка {LoadCapacity:F0}";
    }

    public static class DeviceFactory
    {
        public static Device Create(int id, string model, double power, int roomId, string type) =>
            type == "Sensor"
                ? new SmartSensor(id, model, power, roomId, type)
                : new SmartActuator(id, model, power, roomId, type);
    }

    // ValueTask для кэшированных вызовов (Модуль 5)
    public class SensorCache
    {
        private readonly Dictionary<int, string> _cache = new Dictionary<int, string>();

        public ValueTask<string> GetAsync(SmartSensor sensor)
        {
            if (_cache.TryGetValue(sensor.Id, out var cached))
                return new ValueTask<string>(cached + " [из кэша]");   // без выделения Task

            return new ValueTask<string>(LoadAsync(sensor));
        }

        private async Task<string> LoadAsync(SmartSensor sensor)
        {
            double v = await sensor.ReadAsync();
            string s = $"{sensor.Model}: {v:F2}";
            _cache[sensor.Id] = s;
            return s + " [новое]";
        }
    }
}
