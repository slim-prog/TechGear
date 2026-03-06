namespace TechGear
{
    using System;
    using System.IO;

    // Statische Klasse zur Protokollierung von Systemereignissen (Ausleihe, Rückgabe, Sperrung).
    internal static class Logger
    {
        private const string LogFilePath = "history.txt";

        // Protokolliert eine Aktion mit einem exakten Zeitstempel in die Historien-Datei.
        public static void LogAction(string username, string action, string deviceName)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                string logMessage = $"[{timestamp}] Benutzer '{username}' hat {action}: '{deviceName}'";

                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
            }
            catch (IOException ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[Log-Fehler: Die Datei ist blockiert oder unzugänglich - {ex.Message}]");
                Console.ResetColor();
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[Log-Fehler: Fehlende Schreibrechte für die Log-Datei - {ex.Message}]");
                Console.ResetColor();
            }
        }

        // Liest die Log-Datei aus und gibt alle Einträge in der Konsole aus.
        public static void ShowHistory()
        {
            if (!File.Exists(LogFilePath))
            {
                Console.WriteLine("Es gibt noch keine Historie.");
                return;
            }

            try
            {
                // File.ReadLines ist speichereffizienter als File.ReadAllLines, 
                // da es einen IEnumerable zurückgibt und nicht das gesamte Array in den RAM lädt.
                foreach (var line in File.ReadLines(LogFilePath))
                {
                    Console.WriteLine(line);
                }
            }
            catch (IOException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fehler beim Lesen der Historie: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
