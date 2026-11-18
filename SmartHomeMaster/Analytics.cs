using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartHomeMaster
{
    public static class Analytics
    {
        // ===== Модуль 3: замыкание =====
        // Локальная переменная threshold захватывается лямбдой и живёт, пока жив делегат.
        public static Func<double, bool> CreateThresholdChecker(double threshold)
        {
            return power => power > threshold;
        }

        // Встроенные делегаты
        public static readonly Predicate<Device> IsCritical = d => d.Power > 2000 || !d.IsActive && d.Power > 1000;
        public static readonly Func<Device, double> CalcPower = d => d.CalculatePowerConsumption();
        public static readonly Action<Device> Activate = d => d.SetActive(true);

        // ===== Модуль 4: LINQ =====

        // Проекция + Join -> DTO
        public static List<DeviceReportDto> BuildReport(List<Device> devices, List<Room> rooms) =>
            (from d in devices
             join r in rooms on d.RoomId equals r.Id
             select new DeviceReportDto
             {
                 DeviceId = d.Id,
                 Model = d.Model,
                 RoomTitle = r.Title,
                 Floor = r.Floor,
                 Power = d.Power
             }).ToList();

        // GroupJoin: комната -> число устройств
        public static List<(string Room, int Count)> RoomsWithCounts(List<Device> devices, List<Room> rooms) =>
            rooms.GroupJoin(devices, r => r.Id, d => d.RoomId, (r, ds) => (r.Title, ds.Count())).ToList();

        // Фильтрация + составная сортировка + пагинация
        public static List<DeviceReportDto> GetPage(List<DeviceReportDto> report, double minPower, int page, int pageSize) =>
            report.Where(d => d.Power >= minPower)
                  .OrderBy(d => d.Floor)
                  .ThenByDescending(d => d.Power)
                  .Skip((page - 1) * pageSize)
                  .Take(pageSize)
                  .ToList();

        public static int CountFiltered(List<DeviceReportDto> report, double minPower) =>
            report.Count(d => d.Power >= minPower);

        // Группировка + агрегаты + Aggregate
        public static List<GroupStat> GroupStats(List<DeviceReportDto> report) =>
            report.GroupBy(d => d.RoomTitle)
                  .Select(g => new GroupStat
                  {
                      Room = g.Key,
                      Count = g.Count(),
                      Total = g.Sum(x => x.Power),
                      Average = Math.Round(g.Average(x => x.Power), 1),
                      Models = g.Select(x => x.Model).Aggregate((a, b) => a + ", " + b)
                  })
                  .OrderBy(s => s.Room)
                  .ToList();

        // Операции над множествами
        public static List<string> ModelsOfRoom(List<Device> devices, Room room) =>
            devices.Where(d => d.RoomId == room.Id).Select(d => d.Model).ToList();

        public static IEnumerable<string> Exclusive(List<string> a, List<string> b) => a.Except(b);
        public static IEnumerable<string> Common(List<string> a, List<string> b) => a.Intersect(b);
        public static IEnumerable<string> All(List<string> a, List<string> b) => a.Union(b);

        // Кванторы
        public static string Quantifiers(List<Device> devices)
        {
            bool any = devices.Any(d => d.Power > 2000);
            bool all = devices.All(d => d.Power > 0);
            var first = devices.FirstOrDefault(d => d.Power > 10000);
            return $"Any(Power>2000)={any}; All(Power>0)={all}; " +
                   $"FirstOrDefault(Power>10000)={(first == null ? "null (безопасно)" : first.Model)}";
        }

        // Отложенное выполнение vs ToList()
        public static string DeferredDemo(List<Device> devices)
        {
            var source = new List<Device>(devices);

            IEnumerable<Device> lazy = source.Where(d => d.Power > 1000);   // запрос ещё НЕ выполнен
            List<Device> snapshot = source.Where(d => d.Power > 1000).ToList(); // выполнен сразу

            source.Add(new SmartActuator(999, "New-Heater", 5000, 1, "Actuator"));

            return $"После добавления устройства: отложенный запрос = {lazy.Count()} шт., " +
                   $"ToList() = {snapshot.Count} шт.";
        }
    }
}
