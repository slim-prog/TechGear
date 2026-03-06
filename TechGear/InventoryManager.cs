namespace TechGear
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    // Verwaltet den gesamten Bestand an Geräten (Devices). 
    // Ist verantwortlich für das Laden, Speichern, Suchen und Hinzufügen von Geräten.
    // Trennt die Datenlogik (Business Logic) sauber von der Benutzeroberfläche (Console/Program.cs).
    internal class InventoryManager
    {
        // Interne Liste der Geräte
        private readonly List<Device> _devices = new();

        // Dateipfad für die Persistenz der Inventardaten
        private const string DeviceFilePath = "devices.csv";

        // Gibt eine schreibgeschützte Liste (IReadOnlyList) aller Geräte zurück, um unbeabsichtigte Änderungen von außen zu verhindern.
        public IReadOnlyList<Device> Devices => _devices;

        // Lädt die Geräte aus der CSV-Datei in die interne Liste.
        // Falls die Datei nicht existiert, werden Standard-Demodaten erstellt.

        public void LoadDevicesFromFile()
        {
            _devices.Clear();

            // Überprüfen, ob die Datei existiert. Wenn nicht, Demodaten anlegen.
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

            // Alle Zeilen aus der CSV-Datei einlesen
            string[] lines = File.ReadAllLines(DeviceFilePath);

            foreach (var line in lines)
            {
                // Leere Zeilen überspringen, um Abstürze zu vermeiden
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Zeile am Trennzeichen (Semikolon) aufsplitten
                string[] parts = line.Split(';');

                // Eine gültige Zeile muss mindestens 4 Eigenschaften haben (ID, Name, Kategorie, Status)
                if (parts.Length < 4)
                    continue;

                // Sicheres Parsen der ID (Fehlerbehandlung mit TryParse)
                if (!int.TryParse(parts[0], out int id))
                    continue;

                string name = parts[1];
                string category = parts[2];

                // Status der Verfügbarkeit parsen (Standard: true)
                bool isAvailable = true;
                if (bool.TryParse(parts[3], out bool parsedAvail)) isAvailable = parsedAvail;

                // Prüfen, ob das Gerät aktuell ausgeliehen ist und von wem
                string? borrowedBy = null;
                if (parts.Length >= 5 && !string.IsNullOrWhiteSpace(parts[4])) borrowedBy = parts[4];

                var device = new Device(id, name, category);

                // Wenn das Gerät ausgeliehen ist, den Status entsprechend setzen
                if (!isAvailable && borrowedBy != null) device.MarkAsBorrowedBy(borrowedBy);

                // Prüfen der 6 Spalte: Ist das Gerät gesperrt (z.B. defekt)?
                if (parts.Length >= 6 && bool.TryParse(parts[5], out bool isBlocked) && isBlocked)
                {
                    device.BlockDevice();
                }

                _devices.Add(device);
            }
        }

        // Speichert die aktuelle Liste der Geräte in die CSV-Datei, um die Daten dauerhaft zu sichern.

        public void SaveDevicesToFile()
        {
            var lines = new List<string>();

            foreach (var device in _devices)
            {
                // Null-Werte abfangen, falls das Gerät nicht ausgeliehen ist
                string borrowedBy = device.BorrowedBy ?? string.Empty;

                // String-Interpolation zur Erstellung des CSV-Formats
                string line = $"{device.Id};{device.Name};{device.Category};{device.IsAvailable};{borrowedBy};{device.IsBlocked}";
                lines.Add(line);
            }

            File.WriteAllLines(DeviceFilePath, lines);
        }

        // Filtert die Liste und gibt nur Geräte zurück, die ausleihbar sind 
        // Die Pfeil-Syntax (=>) ist ein Expression-bodied member, der den Code kürzer macht.
        // Das Fragezeichen bei 'Device?' zeigt, dass die Methode auch 'null' zurückgeben kann, falls die ID nicht existiert.

        public IEnumerable<Device> GetAvailableDevices() => _devices.Where(d => d.IsAvailable && !d.IsBlocked);

        // Sucht ein Gerät anhand seiner eindeutigen ID.
        public Device? FindById(int id) => _devices.Find(d => d.Id == id);

        // Sucht nach Geräten, die den übergebenen Suchbegriff entweder im Namen oder in der Kategorie enthalten.
        // Die Suche ignoriert die Groß- und Kleinschreibung (case-insensitive).
        public IEnumerable<Device> SearchDevices(string searchTerm)
        {
            // Wenn der Suchbegriff leer ist, geben wir eine leere Liste zurück
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<Device>();

            string lowerTerm = searchTerm.ToLower();

            // LINQ-Abfrage: Prüfe, ob Name ODER Kategorie den Suchbegriff enthalten
            return _devices.Where(d => d.Name.ToLower().Contains(lowerTerm) || d.Category.ToLower().Contains(lowerTerm));
        }

        // Fügt ein neues Gerät zum Inventar hinzu.
        public void AddDevice(Device device)
            => _devices.Add(device);
    }
}
