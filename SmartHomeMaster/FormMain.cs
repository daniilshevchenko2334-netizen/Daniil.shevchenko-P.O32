using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace SmartHomeMaster
{
    public class FormMain : Form
    {
        private readonly DeviceRepository _devRepo = new DeviceRepository();
        private readonly RoomRepository _roomRepo = new RoomRepository();
        private readonly TelemetryRepository _telRepo = new TelemetryRepository();
        private readonly SensorCache _cache = new SensorCache();

        private List<Device> _devices = new List<Device>();
        private List<Room> _rooms = new List<Room>();

        private readonly TabControl _tabs = new TabControl { Dock = DockStyle.Fill };

        // Вкладка 1
        private DataGridView _gridDevices;
        private TextBox _txtModel, _txtPower;
        private ComboBox _cmbType, _cmbRoom;

        // Вкладка 2
        private ListBox _log;
        private NumericUpDown _numThreshold;

        // Вкладка 3
        private DataGridView _gridLinq;
        private TextBox _txtLinq;

        // Вкладка 4
        private ComboBox _cmbA, _cmbB;
        private ListBox _lstSets;
        private DataGridView _gridPage;
        private Label _lblPage;
        private NumericUpDown _numMinPower;
        private int _page = 1;
        private const int PageSize = 3;

        public FormMain()
        {
            Text = "SmartHome Master Console";
            Width = 1000;
            Height = 650;
            StartPosition = FormStartPosition.CenterScreen;

            Db.Init();

            _tabs.TabPages.Add(BuildRegistryTab());
            _tabs.TabPages.Add(BuildAsyncTab());
            _tabs.TabPages.Add(BuildLinqTab());
            _tabs.TabPages.Add(BuildSetsTab());
            Controls.Add(_tabs);

            Load += async (s, e) => await ReloadAsync();
        }

        // ============ Загрузка данных (async, UI не блокируется) ============
        private async System.Threading.Tasks.Task ReloadAsync()
        {
            _devices = await _devRepo.GetAllAsync();
            _rooms = await _roomRepo.GetAllAsync();

            // Подписка на события каждого устройства
            foreach (var d in _devices)
                d.StatusChanged += OnDeviceStatusChanged;

            _cmbRoom.DataSource = _rooms.ToList();
            _cmbA.DataSource = _rooms.ToList();
            _cmbB.DataSource = _rooms.ToList();
            if (_cmbB.Items.Count > 1) _cmbB.SelectedIndex = 1;

            RefreshDevicesGrid();
            RefreshPage();
        }

        private void OnDeviceStatusChanged(object sender, DeviceEventArgs e)
        {
            _log.Items.Insert(0, $"[{e.Timestamp:HH:mm:ss}] {e.Message}");
            RefreshDevicesGrid();
        }

        private void RefreshDevicesGrid()
        {
            _gridDevices.DataSource = _devices.Select(d => new
            {
                d.Id,
                d.Model,
                d.Type,
                d.Power,
                Status = d.GetStatus(),
                Passport = d.Calibration.ToString()
            }).ToList();
        }

        private static FlowLayoutPanel Row(params Control[] items)
        {
            var p = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(4) };
            p.Controls.AddRange(items);
            return p;
        }

        private static Label Lbl(string text) =>
            new Label { Text = text, AutoSize = true, Margin = new Padding(6, 8, 2, 0) };

        // ============ Вкладка 1: Реестр приборов (ООП & Generics) ============
        private TabPage BuildRegistryTab()
        {
            var tab = new TabPage("Реестр приборов (ООП & Generics)");

            _gridDevices = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            _txtModel = new TextBox { Width = 120 };
            _txtPower = new TextBox { Width = 70 };
            _cmbType = new ComboBox { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbType.Items.AddRange(new object[] { "Sensor", "Actuator" });
            _cmbType.SelectedIndex = 0;
            _cmbRoom = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };

            var btnAdd = new Button { Text = "Добавить", AutoSize = true };
            btnAdd.Click += async (s, e) => await AddDeviceAsync();

            var btnSort = new Button { Text = "Сортировать (IComparable)", AutoSize = true };
            btnSort.Click += (s, e) =>
            {
                _devices.Sort();    // использует Device.CompareTo -> по мощности
                RefreshDevicesGrid();
            };

            var btnClone = new Button { Text = "Deep Copy (ICloneable)", AutoSize = true };
            btnClone.Click += BtnClone_Click;

            var btnExport = new Button { Text = "Экспорт (IExportable)", AutoSize = true };
            btnExport.Click += (s, e) =>
            {
                var d = SelectedDevice();
                if (d == null) return;
                // Явная реализация: доступна только через приведение к интерфейсу
                MessageBox.Show(((IExportable)d).GetExportData(), "GetExportData()");
            };

            tab.Controls.Add(_gridDevices);
            tab.Controls.Add(Row(Lbl("Модель:"), _txtModel, Lbl("Мощность:"), _txtPower,
                                 Lbl("Тип:"), _cmbType, Lbl("Комната:"), _cmbRoom, btnAdd));
            tab.Controls.Add(Row(btnSort, btnClone, btnExport));
            // Row добавлены последними -> лежат сверху благодаря Dock=Top
            return tab;
        }

        private Device SelectedDevice()
        {
            if (_gridDevices.CurrentRow == null)
            {
                MessageBox.Show("Выберите устройство в таблице.");
                return null;
            }
            int id = (int)_gridDevices.CurrentRow.Cells["Id"].Value;
            return _devices.FirstOrDefault(d => d.Id == id);
        }

        private async System.Threading.Tasks.Task AddDeviceAsync()
        {
            string model = _txtModel.Text.Trim();
            if (string.IsNullOrEmpty(model))
            {
                MessageBox.Show("Введите модель.");
                return;
            }
            if (!double.TryParse(_txtPower.Text.Replace(',', '.'), NumberStyles.Float,
                                 CultureInfo.InvariantCulture, out double power) || power <= 0)
            {
                MessageBox.Show("Мощность должна быть положительным числом.");
                return;
            }
            if (!(_cmbRoom.SelectedItem is Room room)) return;

            var dev = DeviceFactory.Create(0, model, power, room.Id, (string)_cmbType.SelectedItem);
            _devRepo.Add(dev);
            _txtModel.Clear();
            _txtPower.Clear();
            await ReloadAsync();
        }

        private void BtnClone_Click(object sender, EventArgs e)
        {
            var original = SelectedDevice();
            if (original == null) return;

            var copy = (Device)original.Clone();
            copy.Calibration.PassportNumber = original.Calibration.PassportNumber + "-COPY";

            bool sameRef = ReferenceEquals(original.Calibration, copy.Calibration);
            MessageBox.Show(
                $"Оригинал: {original.Calibration.PassportNumber}\n" +
                $"Копия:    {copy.Calibration.PassportNumber}\n" +
                $"Один и тот же объект калибровки: {sameRef}  (False = Deep Copy)",
                "Результат клонирования");
        }

        // ============ Вкладка 2: Асинхронный опрос & Events ============
        private TabPage BuildAsyncTab()
        {
            var tab = new TabPage("Асинхронный опрос & Events");
            _log = new ListBox { Dock = DockStyle.Fill, HorizontalScrollbar = true };

            var btnScan = new Button { Text = "Сканировать датчики (Task.WhenAll)", AutoSize = true };
            btnScan.Click += async (s, e) => await ScanAsync(btnScan);

            var btnCached = new Button { Text = "Кэш (ValueTask)", AutoSize = true };
            btnCached.Click += async (s, e) =>
            {
                foreach (var sensor in _devices.OfType<SmartSensor>().Take(3))
                    _log.Items.Insert(0, await _cache.GetAsync(sensor));
            };

            var btnToggle = new Button { Text = "Вкл/Выкл выбранное (событие)", AutoSize = true };
            btnToggle.Click += (s, e) =>
            {
                var d = SelectedDevice();
                d?.SetActive(!d.IsActive);   // -> StatusChanged?.Invoke(...)
            };

            _numThreshold = new NumericUpDown { Minimum = 0, Maximum = 10000, Value = 100, Width = 80 };
            var btnThreshold = new Button { Text = "Порог (замыкание)", AutoSize = true };
            btnThreshold.Click += (s, e) =>
            {
                // Замыкание: локальный порог захватывается внутри Func<double,bool>
                Func<double, bool> over = Analytics.CreateThresholdChecker((double)_numThreshold.Value);
                var hits = _devices.Where(d => over(d.Power)).ToList();
                _log.Items.Insert(0, $"Выше порога {_numThreshold.Value} Вт: {hits.Count} устр.");
            };

            var btnCritical = new Button { Text = "Критичные (Predicate)", AutoSize = true };
            btnCritical.Click += (s, e) =>
            {
                var critical = _devices.FindAll(Analytics.IsCritical);
                _log.Items.Insert(0, "Критичные: " + (critical.Count == 0
                    ? "нет"
                    : string.Join(", ", critical.Select(d => d.Model))));
                double total = _devices.Sum(Analytics.CalcPower);   // Func<Device,double>
                _log.Items.Insert(0, $"Суммарное потребление: {total:F1} Вт");
            };

            tab.Controls.Add(_log);
            tab.Controls.Add(Row(btnScan, btnCached, btnToggle));
            tab.Controls.Add(Row(Lbl("Порог, Вт:"), _numThreshold, btnThreshold, btnCritical));
            return tab;
        }

        private async System.Threading.Tasks.Task ScanAsync(Button btn)
        {
            btn.Enabled = false;
            try
            {
                var sensors = _devices.OfType<SmartSensor>().ToList();
                _log.Items.Insert(0, $"Опрос {sensors.Count} датчиков параллельно...");

                var tasks = sensors.Select(s => s.ReadAsync()).ToList();
                double[] results = await System.Threading.Tasks.Task.WhenAll(tasks);

                for (int i = 0; i < sensors.Count; i++)
                {
                    _log.Items.Insert(0, $"{sensors[i].Model} (#{sensors[i].Id}): {results[i]:F2}");
                    _telRepo.Add(new TelemetryLog
                    {
                        DeviceId = sensors[i].Id,
                        Reading = results[i],
                        Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
                _log.Items.Insert(0, "Опрос завершён, показания записаны в TelemetryLogs.");
            }
            finally
            {
                btn.Enabled = true;
            }
        }

        // ============ Вкладка 3: LINQ-аналитика & DTO ============
        private TabPage BuildLinqTab()
        {
            var tab = new TabPage("LINQ-Аналитика & DTO");
            _gridLinq = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _txtLinq = new TextBox { Dock = DockStyle.Bottom, Height = 70, Multiline = true, ReadOnly = true };

            var btnJoin = new Button { Text = "Join -> DTO", AutoSize = true };
            btnJoin.Click += (s, e) =>
            {
                _gridLinq.DataSource = Analytics.BuildReport(_devices, _rooms);
                var counts = Analytics.RoomsWithCounts(_devices, _rooms);
                _txtLinq.Text = "GroupJoin: " + string.Join("; ", counts.Select(c => $"{c.Room}={c.Count}"));
            };

            var btnFilter = new Button { Text = "Фильтр + OrderBy.ThenBy", AutoSize = true };
            btnFilter.Click += (s, e) =>
            {
                var report = Analytics.BuildReport(_devices, _rooms);
                _gridLinq.DataSource = report.Where(d => d.Power >= 50)
                                             .OrderBy(d => d.Floor)
                                             .ThenByDescending(d => d.Power)
                                             .ToList();
                _txtLinq.Text = "Where(Power>=50) -> OrderBy(Floor).ThenByDescending(Power)";
            };

            var btnGroup = new Button { Text = "GroupBy + агрегаты", AutoSize = true };
            btnGroup.Click += (s, e) =>
            {
                var report = Analytics.BuildReport(_devices, _rooms);
                _gridLinq.DataSource = Analytics.GroupStats(report);
                _txtLinq.Text = "GroupBy(RoomTitle): Count, Sum, Average, Aggregate(строка моделей)";
            };

            var btnQuant = new Button { Text = "Any / All / FirstOrDefault", AutoSize = true };
            btnQuant.Click += (s, e) => _txtLinq.Text = Analytics.Quantifiers(_devices);

            var btnDeferred = new Button { Text = "Отложенное выполнение", AutoSize = true };
            btnDeferred.Click += (s, e) => _txtLinq.Text = Analytics.DeferredDemo(_devices);

            tab.Controls.Add(_gridLinq);
            tab.Controls.Add(_txtLinq);
            tab.Controls.Add(Row(btnJoin, btnFilter, btnGroup, btnQuant, btnDeferred));
            return tab;
        }

        // ============ Вкладка 4: Множества & Пагинация ============
        private TabPage BuildSetsTab()
        {
            var tab = new TabPage("Множества & Пагинация");

            _cmbA = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbB = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            _lstSets = new ListBox { Dock = DockStyle.Left, Width = 300 };

            void ShowSet(string title, Func<List<string>, List<string>, IEnumerable<string>> op)
            {
                if (!(_cmbA.SelectedItem is Room a) || !(_cmbB.SelectedItem is Room b)) return;
                var ma = Analytics.ModelsOfRoom(_devices, a);
                var mb = Analytics.ModelsOfRoom(_devices, b);
                _lstSets.Items.Clear();
                _lstSets.Items.Add($"{title}: {a.Title} / {b.Title}");
                foreach (var m in op(ma, mb)) _lstSets.Items.Add("  " + m);
            }

            var btnInter = new Button { Text = "Intersect (общие)", AutoSize = true };
            btnInter.Click += (s, e) => ShowSet("Intersect", Analytics.Common);
            var btnExcept = new Button { Text = "Except (только в A)", AutoSize = true };
            btnExcept.Click += (s, e) => ShowSet("Except", Analytics.Exclusive);
            var btnUnion = new Button { Text = "Union (все)", AutoSize = true };
            btnUnion.Click += (s, e) => ShowSet("Union", Analytics.All);

            _gridPage = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            _lblPage = new Label { AutoSize = true, Margin = new Padding(10, 8, 10, 0) };
            _numMinPower = new NumericUpDown { Minimum = 0, Maximum = 10000, Value = 0, Width = 70 };
            _numMinPower.ValueChanged += (s, e) => { _page = 1; RefreshPage(); };

            var btnPrev = new Button { Text = "< Назад", AutoSize = true };
            btnPrev.Click += (s, e) => { if (_page > 1) { _page--; RefreshPage(); } };
            var btnNext = new Button { Text = "Вперёд >", AutoSize = true };
            btnNext.Click += (s, e) =>
            {
                int pages = TotalPages();
                if (_page < pages) { _page++; RefreshPage(); }
            };

            tab.Controls.Add(_gridPage);
            tab.Controls.Add(_lstSets);
            tab.Controls.Add(Row(Lbl("Мин. мощность:"), _numMinPower, btnPrev, _lblPage, btnNext));
            tab.Controls.Add(Row(Lbl("A:"), _cmbA, Lbl("B:"), _cmbB, btnInter, btnExcept, btnUnion));
            return tab;
        }

        private int TotalPages()
        {
            var report = Analytics.BuildReport(_devices, _rooms);
            int count = Analytics.CountFiltered(report, (double)_numMinPower.Value);
            return Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
        }

        private void RefreshPage()
        {
            if (_gridPage == null) return;
            var report = Analytics.BuildReport(_devices, _rooms);
            _gridPage.DataSource = Analytics.GetPage(report, (double)_numMinPower.Value, _page, PageSize);
            _lblPage.Text = $"Страница {_page} из {TotalPages()}";
        }
    }
}
