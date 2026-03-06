namespace TechGear
{
    // Die Klasse Device repräsentiert ein einzelnes Gerät im Inventarsystem.
    // Sie speichert alle relevanten Eigenschaften und verwaltet den Ausleih- und Sperrstatus.
    internal class Device
    {
        // Eindeutige Identifikationsnummer des Geräts.
        public int Id { get; }

        // Der Name oder die Bezeichnung des Geräts (z.B. "Laptop Dell").
        public string Name { get; }

        // Die Kategorie, zu der das Gerät gehört (z.B. "Laptop", "Monitor").
        public string Category { get; }

        // Gibt an, ob das Gerät aktuell für eine Ausleihe zur Verfügung steht.
        // Kann nur innerhalb dieser Klasse (private set) geändert werden.
        public bool IsAvailable { get; private set; } = true;

        // Speichert den Benutzernamen der Person, die das Gerät aktuell ausgeliehen hat.
        // Ist null, wenn das Gerät nicht ausgeliehen ist.
        public string? BorrowedBy { get; private set; }

        // Gibt an, ob das Gerät für die Ausleihe gesperrt ist (z.B. wegen eines Defekts).
        public bool IsBlocked { get; private set; } = false;

        // Konstruktor: Wird aufgerufen, wenn ein neues Gerät erstellt wird.
        // Standardmäßig ist ein neues Gerät verfügbar und nicht ausgeliehen.
        public Device(int id, string name, string category)
        {
            Id = id;
            Name = name;
            Category = category;
        }

        // Markiert das Gerät als ausgeliehen durch einen bestimmten Benutzer (User-Objekt).
        public void MarkAsBorrowed(User user)
        {
            IsAvailable = false;
            BorrowedBy = user.Username;
        }

        // Überladene Methode: Markiert das Gerät als ausgeliehen, aber nimmt direkt einen String (Username).
        // Wird vor allem beim Einlesen aus der CSV-Datei (LoadDevicesFromFile) genutzt.
        public void MarkAsBorrowedBy(string username)
        {
            IsAvailable = false;
            BorrowedBy = username;
        }

        // Setzt den Status des Geräts zurück, wenn es von einem Benutzer zurückgegeben wird.
        public void MarkAsReturned()
        {
            IsAvailable = true;
            BorrowedBy = null;
        }

        // Setzt den Status auf "gesperrt", sodass das Gerät im User-Menü nicht mehr ausleihbar ist.
        public void BlockDevice()
        {
            IsBlocked = true;
        }

        // Hebt die Sperrung auf (z.B. nach einer erfolgreichen Reparatur).
        public void UnblockDevice()
        {
            IsBlocked = false;
        }

        // Überschreibt die Standard-ToString-Methode, um das Gerät sauber in der Konsole auszugeben.
        // Liefert eine formatierte Zeichenkette inklusive des aktuellen Status.
        public override string ToString()
        {
            string status;

            // Priorität hat die Sperrung: Wenn es defekt ist, wird dies zuerst angezeigt.
            if (IsBlocked)
            {
                status = "GESPERRT (Defekt/Wartung)";
            }
            else
            {
                // Ternärer Operator für eine kompakte if-else-Abfrage des Ausleihstatus
                status = IsAvailable ? "verfügbar" : $"ausgeliehen (von {BorrowedBy})";
            }

            return $"[{Id}] {Name} ({Category}) - {status}";
        }
    }
}
