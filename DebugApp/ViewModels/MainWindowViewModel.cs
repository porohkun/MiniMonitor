using DataCollector;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;

namespace DebugApp
{
    public class MainWindowViewModelDummy : MainWindowViewModel
    {
        public MainWindowViewModelDummy()
        {
            Values.Add(new ValueData() { Name = "TestName", Value = 3.14f });
            Values.Add(new ValueData() { Name = "TestName2", Value = 92 });

            Logs = "lorem ipsum sit doloren\nconnection established\nok";

            Ports.Add("COM1");
            Ports.Add("COM2");
            Ports.Add("COM3");

            SelectedPort = "COM3";
        }
    }

    public class MainWindowViewModel : BindableBase
    {
        Monitor _monitor = new Monitor(1, "CPU Package", "GPU Core");

        public ObservableCollection<ValueData> Values { get; } = new ObservableCollection<ValueData>();
        private string _logs = "";
        public string Logs
        {
            get => _logs;
            set => SetProperty(ref _logs, value);
        }
        public ObservableCollection<string> Ports { get; } = new ObservableCollection<string>();

        private string _selectedPort;
        public string SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }
        public bool TimerEnabled
        {
            get => _monitor.TimerEnabled;
            set => _monitor.TimerEnabled = value;
        }

        public DelegateCommand ListenPortCommand { get; }
        public DelegateCommand StartMonitorCommand { get; }
        public DelegateCommand XBmpEditorOpenCommand { get; }

        private object _valuesLock = new object();
        private object _portsLock = new object();

        public MainWindowViewModel()
        {
            ListenPortCommand = new DelegateCommand(() => Task.Run(ListenPort));
            StartMonitorCommand = new DelegateCommand(() => Task.Run(StartMonitor));
            XBmpEditorOpenCommand = new DelegateCommand(() => new XBmpWindow().Show());

            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(Values, _valuesLock);
            System.Windows.Data.BindingOperations.EnableCollectionSynchronization(Ports, _portsLock);

            //_monitor.Update();

            Task.Run(() => RefreshPorts());
        }

        private async void RefreshPorts()
        {
            while (true)
            {
                var newPorts = SerialPort.GetPortNames();

                foreach (var port in newPorts)
                    if (!Ports.Contains(port))
                        Ports.Add(port);

                var forRemove = new List<string>();
                foreach (var port in Ports)
                    if (!newPorts.Contains(port))
                        forRemove.Add(port);

                foreach (var port in forRemove)
                    Ports.Remove(port);

                if (SelectedPort == null && Ports.Count == 1)
                    SelectedPort = Ports[0];

                System.Threading.Thread.Sleep(200);
            }
        }

        private async void ListenPort()
        {
            try
            {
                using (SerialPort port = new SerialPort(SelectedPort)
                {
                    BaudRate = 9600,
                    //DataBits = 8,
                    //Parity = Parity.None,
                    //StopBits = StopBits.One,
                    //ReadTimeout = 3000,
                    //WriteTimeout = 3000
                })
                {
                    port.Open();
                    port.RtsEnable = true;
                    port.DtrEnable = true;
                    Log("==== Connected ====\n");
                    //port.Write(new byte[] { 5 }, 0, 1);
                    while (true)
                    {
                        while (port.BytesToRead != 0)
                            LogAppend(port.ReadExisting());
                        while (port.BytesToRead == 0) ;
                    }
                    //while (true)
                    //{
                    //    string a = port.ReadExisting();
                    //    Log(a);
                    //    System.Threading.Thread.Sleep(200);
                    //}
                }
            }
            catch { }

            Log("==== Disonnected ====");

        }

        private void StartMonitor()
        {
            _monitor.Update();
            Values.Clear();
            foreach (var value in _monitor.GetValues())
                Values.Add(new ValueData() { Name = value.Key, Value = value.Value });
        }

        private void Log(string message)
        {
            Logs += $"\n{message}";
        }

        private void LogAppend(string message)
        {
            Logs += $"{message}";
        }

    }
}
