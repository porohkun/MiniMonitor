using NModbus;
using NModbus.Serial;
using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataCollector
{
    public class Monitor
    {
        public float CPU => GetValue(_cpuSensorName);
        public float GPU => GetValue(_gpuSensorName);

        readonly byte _slaveID;
        readonly string _cpuSensorName;
        readonly string _gpuSensorName;

        readonly Dictionary<string, float> _values;
        readonly Computer _computer;

        public Monitor(byte slaveID, string cpuSensorName, string gpuSensorName)
        {
            _slaveID = slaveID;
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
            WriteRegisters();
        }

        private SerialPort GetSerialPort()
        {
            SerialPort port = new SerialPort("COM4")
            {
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One
            };
            port.Open();
            return port;
        }

        private void WriteRegisters()
        {
            using (var port = GetSerialPort())
            {
                var factory = new ModbusFactory();
                IModbusSerialMaster master = factory.CreateRtuMaster(port);

                master.WriteMultipleRegisters(_slaveID, 0, new ushort[]
                {
                    (ushort)'M',
                    (ushort)'M',
                    (ushort)'r',
                    (ushort)DateTime.Now.Year,
                    (ushort)DateTime.Now.Month,
                    (ushort)DateTime.Now.Day,
                    (ushort)DateTime.Now.Hour,
                    (ushort)DateTime.Now.Minute,
                    (ushort)DateTime.Now.Second,
                    (ushort)CPU,
                    (ushort)GPU
                });
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
