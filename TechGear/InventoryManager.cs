using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TechGear
{
    internal class InventoryManager
    {
        private readonly List<Device> _devices = new();
        private const string DeviceFilePath = "devices.csv";

        public IReadOnlyList<Device> Devices => _devices;

        public void LoadDevicesFromFile()
        {
            _devices.Clear();

            if (!File.Exists(DeviceFilePath))
            {
                var d1 = new Device(1, "Laptop Dell", "Laptop");
                var d2 = new Device(2, "Monitor HP 24\"", "Monitor");
                var d3 = new Device(3, "Beamer Epson", "Beamer");
                _devices.Add(d1);
                _devices.Add(d2);
                _devices.Add(d3);

                SaveDevicesToFile();
                return;
            }

            string[] lines = File.ReadAllLines(DeviceFilePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split(';');
                if (parts.Length < 4)
                    continue;

                if (!int.TryParse(parts[0], out int id))
                    continue;

                string name = parts[1];
                string category = parts[2];

                bool isAvailable = true;
                if (bool.TryParse(parts[3], out bool parsedAvail))
                    isAvailable = parsedAvail;

                string? borrowedBy = null;
                if (parts.Length >= 5 && !string.IsNullOrWhiteSpace(parts[4]))
                    borrowedBy = parts[4];

                var device = new Device(id, name, category);

                if (!isAvailable && borrowedBy != null)
                    device.MarkAsBorrowedBy(borrowedBy);

                if (parts.Length >= 6 &&
                    bool.TryParse(parts[5], out bool isBlocked) &&
                    isBlocked)
                {
                    device.BlockDevice();
                }

                _devices.Add(device);
            }
        }

        public void SaveDevicesToFile()
        {
            var lines = new List<string>();

            foreach (var device in _devices)
            {
                string borrowedBy = device.BorrowedBy ?? string.Empty;
                string line =
                    $"{device.Id};{device.Name};{device.Category};{device.IsAvailable};{borrowedBy};{device.IsBlocked}";
                lines.Add(line);
            }

            File.WriteAllLines(DeviceFilePath, lines);
        }

        public IEnumerable<Device> GetAvailableDevices()
            => _devices.Where(d => d.IsAvailable && !d.IsBlocked);

        public Device? FindById(int id)
            => _devices.Find(d => d.Id == id);

        public void AddDevice(Device device)
            => _devices.Add(device);
    }
}
