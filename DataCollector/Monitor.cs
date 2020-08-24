using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCollector
{
    public class Monitor
    {
        readonly Dictionary<string, float> _values;
        readonly Computer _computer;

        readonly string _cpuSensorName;
        readonly string _gpuSensorName;

        public float CPU => GetValue(_cpuSensorName);
        public float GPU => GetValue(_gpuSensorName);

        public Monitor(string cpuSensorName, string gpuSensorName)
        {
            _cpuSensorName = cpuSensorName;
            _gpuSensorName = gpuSensorName;
            _values = new Dictionary<string, float>();
            _computer = new Computer() { CPUEnabled = true, GPUEnabled = true };
            _computer.Open();
        }

        public void Update()
        {
            foreach (IHardware hardware in _computer.Hardware)
            {
                hardware.Update();

                foreach (ISensor sensor in hardware.Sensors)
                    if (sensor.SensorType == SensorType.Temperature)
                        _values[sensor.Name] = sensor.Value ?? 0f;
            }
        }

        public IEnumerable<KeyValuePair<string, float>> GetValues()
        {
            return _values;
        }

        public float GetValue(string sensorName)
        {
            if (_values.TryGetValue(sensorName, out var value))
                return value;
            else return 0f;
        }
    }
}
