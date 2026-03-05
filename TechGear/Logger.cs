namespace TechGear
{
    /// Statische Klasse zur Protokollierung von Systemereignissen (Ausleihe, Rückgabe, Sperrung).
    internal static class Logger
    {
        // Dateipfad für die Speicherung der Historie
        private const string LogFilePath = "history.txt";

        public static void LogAction(string username, string action, string deviceName)
        {
            try
            {
                // Erstellung eines exakten Zeitstempels für die Nachverfolgbarkeit
                string timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                string logMessage = $"[{timestamp}] Benutzer '{username}' hat {action}: '{deviceName}'";

                // AppendAllText fügt den Text am Ende der Datei hinzu, ohne vorhandene Daten zu überschreiben
                File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Falls die Datei blockiert ist (z.B. in einem anderen Programm geöffnet),
                // wird das Programm nicht abgestürzt, sondern gibt nur eine dezente Warnung aus.
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[Log-Fehler: Die Historie konnte nicht gespeichert werden - {ex.Message}]");
                Console.ResetColor();
            }
        }

        /// Liest die Log-Datei aus und gibt alle Einträge in der Konsole aus.
        public static void ShowHistory()
        {
            // Prüfung, ob die Datei überhaupt existiert (verhindert FileNotFoundException)
            if (!File.Exists(LogFilePath))
            {
                Console.WriteLine("Es gibt noch keine Historie.");
                return;
            }

            // Alle Zeilen einlesen und direkt in der Konsole ausgeben
            string[] lines = File.ReadAllLines(LogFilePath);
            foreach (var line in lines)
            {
                Console.WriteLine(line);
            }
        }
    }
}
