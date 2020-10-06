using DataCollector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DebugApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Monitor _monitor = new Monitor(1, "CPU Package", "GPU Core");

        public ObservableCollection<ValueData> Values { get; set; } = new ObservableCollection<ValueData>();
        public bool TimerEnabled
        {
            get => _monitor.TimerEnabled;
            set => _monitor.TimerEnabled = value;
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            //_monitor.Update();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            using (SerialPort port = new SerialPort("COM5")
            {
                BaudRate = 115200,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                ReadTimeout = 3000,
                WriteTimeout = 3000
            })
            {
                port.Open();
                port.Write(new byte[] { 5 }, 0, 1);
                while (port.BytesToRead == 0) ;
                MessageBox.Show(port.ReadByte().ToString());
            }
            return;
            _monitor.Update();
            Values.Clear();
            foreach (var value in _monitor.GetValues())
                Values.Add(new ValueData() { Name = value.Key, Value = value.Value });
        }
    }

    public class ValueData
    {
        public string Name { get; set; }
        public float Value { get; set; }
    }
}
