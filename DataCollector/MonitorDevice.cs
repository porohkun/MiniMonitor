using NModbus;
using NModbus.Serial;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;

namespace DataCollector
{
    public class MonitorDevice
    {
        public bool Available => !IsBusy && !HasError && SerialPort.GetPortNames().Contains(_portName);
        public bool IsBusy => _task != null && !_task.IsCompleted;
        public bool? Authed { get; private set; }
        public bool HasError { get; private set; }
        public string PortName => _portName;
        public string ErrorMessage { get; private set; }

        private readonly byte _slaveID;
        private readonly string _portName;
        private readonly string _cpuSensorName;
        private readonly string _gpuSensorName;

        private Task _task;

        public MonitorDevice(byte slaveID, string portName, string cpuSensorName, string gpuSensorName)
        {
            _slaveID = slaveID;
            _portName = portName;
            _cpuSensorName = cpuSensorName;
            _gpuSensorName = gpuSensorName;
        }

        public void SendSensorDataAsync(Dictionary<string, float> sensorData)
        {
            var data = new Dictionary<string, float>();
            foreach (var entry in sensorData)
                data.Add(entry.Key, entry.Value);
            _task = SendSensorData(data);
        }

        private async Task SendSensorData(Dictionary<string, float> sensorData)
        {
            await Task.Run(() =>
            {
                if (!Authed.HasValue)
                {
                    var name = ReadDeviceName();
                    Authed = name[0] == 'M' && name[1] == 'M' && name[2] == 'r';
                }
                if (Authed.Value)
                    WriteRegisters(sensorData);
                else
                    ThrowError($"Not authed");
            });
        }

        private ushort[] ReadDeviceName()
        {
            try
            {
                using (var port = GetSerialPort())
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

        private void WriteRegisters(Dictionary<string, float> sensorData)
        {
            try
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
                    (ushort)(sensorData.TryGetValue(_cpuSensorName, out var cpuValue)?cpuValue: 0f),
                    (ushort)(sensorData.TryGetValue(_gpuSensorName, out var gpuValue)?gpuValue: 0f)
                    });
                }
            }
            catch (Exception ex)
            {
                ThrowError(ex.Message);
            }
        }

        private SerialPort GetSerialPort()
        {
            SerialPort port = new SerialPort(_portName)
            {
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                ReadTimeout = 3000,
                WriteTimeout = 3000
            };
            port.Open();
            port.RtsEnable = true;
            port.DtrEnable = true;
            return port;
        }

        private void ThrowError(string message)
        {
            HasError = true;
            ErrorMessage = message;
        }
    }
}
