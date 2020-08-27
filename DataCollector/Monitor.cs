using NModbus;
using NModbus.Serial;
using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataCollector
{
    public class Monitor
    {
        public float CPU => GetValue(_cpuSensorName);
        public float GPU => GetValue(_gpuSensorName);
        public bool TimerEnabled { get; set; }

        private readonly byte _slaveID;
        private readonly string _cpuSensorName;
        private readonly string _gpuSensorName;

        private readonly Dictionary<string, Task> _busyPorts;
        private readonly Dictionary<string, float> _values;
        private readonly Computer _computer;
        private readonly Timer _timer;


        public Monitor(byte slaveID, string cpuSensorName, string gpuSensorName)
        {
            _slaveID = slaveID;
            _cpuSensorName = cpuSensorName;
            _gpuSensorName = gpuSensorName;
            _busyPorts = new Dictionary<string, Task>();
            _values = new Dictionary<string, float>();
            _computer = new Computer() { CPUEnabled = true, GPUEnabled = true };
            _computer.Open();
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
                        _values[sensor.Name] = sensor.Value ?? 0f;
            }

            foreach (var portName in SerialPort.GetPortNames())
            {
                if (_busyPorts.TryGetValue(portName, out var portTask))
                    if (!portTask.IsCompleted)
                        continue;
                _busyPorts[portName] = SendSensorData(portName);
            }
        }

        private async Task SendSensorData(string portName)
        {
            await Task.Run(() =>
            {
                var name = ReadGadgetName(portName);
                if (name[0] == 'M' && name[1] == 'M' && name[2] == 'r')
                    WriteRegisters(portName);
            });
        }

        private SerialPort GetSerialPort(string portName)
        {
            SerialPort port = new SerialPort(portName)
            {
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                ReadTimeout = 3000,
                WriteTimeout = 3000
            };
            port.Open();
            return port;
        }

        private ushort[] ReadGadgetName(string portName)
        {
            try
            {
                using (var port = GetSerialPort(portName))
                {
                    var factory = new ModbusFactory();
                    IModbusSerialMaster master = factory.CreateRtuMaster(port);

                    return master.ReadHoldingRegisters(_slaveID, 0, 3);
                }
            }
            catch
            {
                return new ushort[3];
            }
        }

        private void WriteRegisters(string portName)
        {
            try
            {
                using (var port = GetSerialPort(portName))
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
            catch { }
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
