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
        Computer computer;

        public Monitor()
        {
            computer = new Computer() { CPUEnabled = true, GPUEnabled = true };
            computer.Open();
        }

        public void Refresh()
        {
            foreach (IHardware hardware in computer.Hardware)
            {
                hardware.Update();

                Console.WriteLine("{0}: {1}", hardware.HardwareType, hardware.Name);
                foreach (ISensor sensor in hardware.Sensors)
                {
                    // Celsius is default unit
                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        Console.WriteLine("{0}: {1}°C", sensor.Name, sensor.Value);
                        // Console.WriteLine("{0}: {1}°F", sensor.Name, sensor.Value*1.8+32);
                    }

                }
            }
        }
    }
}
