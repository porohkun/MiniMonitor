using LibreHardwareMonitor.Hardware;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;

namespace DataCollector
{
    public class Monitor
    {
        public bool TimerEnabled { get; set; }

        private readonly byte _slaveID;
        private readonly string _cpuSensorName;
        private readonly string _gpuSensorName;

        private readonly List<MonitorDevice> _devices;
        private readonly Dictionary<string, float> _values;
        private readonly Computer _computer;
        private readonly Timer _timer;

        private MonitorDevice _activeDevice;

        public Monitor(byte slaveID, string cpuSensorName, string gpuSensorName)
        {
            _slaveID = slaveID;
            _cpuSensorName = cpuSensorName;
            _gpuSensorName = gpuSensorName;
            _devices = new List<MonitorDevice>();
            _values = new Dictionary<string, float>();

            UpdateVisitor updateVisitor = new UpdateVisitor();
            _computer = new Computer() { IsCpuEnabled = true, IsGpuEnabled = true };
            _computer.Open();
            _computer.Accept(updateVisitor);
            _timer = new Timer(TimerTick, new object(), 0, 500);
        }

        private void TimerTick(object state)
        {
            if (TimerEnabled)
                Update();
        }

        public void Update()
        {
            foreach (IHardware hardware in _computer.Hardware)
            {
                hardware.Update();

                foreach (ISensor sensor in hardware.Sensors)
                    if (sensor.SensorType == SensorType.Temperature)
                        _values[sensor.Name] = sensor?.Value ?? 0f;
            }

            if (_activeDevice != null)
            {
                if (_activeDevice.IsBusy)
                    return;
                if (!_activeDevice.Available)
                    _activeDevice = null;
            }

            if (_activeDevice == null)
                _activeDevice = _devices.FirstOrDefault(d => d.Available);

            if (_activeDevice == null)
            {
                _devices.Clear();
                using (var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\""))
                    foreach (var queryObj in searcher.Get())
                    {
                        var name = (string)queryObj.GetPropertyValue("Name");
                        if (name?.Contains("Arduino") ?? false)
                        {
                            var match = Regex.Match(name, @"^Arduino Micro \((COM\d+)\)$");
                            if (match.Success && match.Groups.Count > 0)
                                _devices.Add(new MonitorDevice(_slaveID, match.Groups[1].Value, _cpuSensorName, _gpuSensorName));
                        }
                    }
                _activeDevice = _devices.FirstOrDefault(d => d.Available);
            }

            if (_activeDevice != null)
                _activeDevice.SendSensorDataAsync(_values);
        }

        public IEnumerable<KeyValuePair<string, float>> GetValues()
        {
            return _values;
        }
    }
}
