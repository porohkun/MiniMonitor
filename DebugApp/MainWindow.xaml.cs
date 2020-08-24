using DataCollector;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        Monitor _monitor = new Monitor();

        public ObservableCollection<ValueData> Values { get; set; } = new ObservableCollection<ValueData>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _monitor.Update();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
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
