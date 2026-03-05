namespace TechGear
{
    internal class Program
    {
        // Einfache In-Memory-Datenhaltung
        private static readonly List<User> _users = new();
        private static readonly InventoryManager _inventory = new();
        private static User? _currentUser;
        private const string DeviceFilePath = "devices.csv";
        private const string UserFilePath = "users.csv";

        private static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "TechGear IT Inventar und Ausleihsystem";

            LoadUsersFromFile();
            _inventory.LoadDevicesFromFile();


            while (true)
            {
                Console.Clear();

                if (_currentUser == null)
                {
                    ShowLoginScreen();
                }
                else if (_currentUser.Role == "SuperAdmin" || _currentUser.Role == "Admin")

                {
                    ShowAdminMenu();
                }
                else
                {
                    ShowUserMenu();
                }
            }
        }
        // Nur Benutzer kommen aus dem Code, nicht aus der Datei
        private static void LoadUsersFromFile()
        {
            _users.Clear();

            if (!File.Exists(UserFilePath))
            {
                // Creăm cele 3 conturi demo dacă fișierul nu există
                _users.Add(new User("superadmin", "superadmin", "SuperAdmin"));
                _users.Add(new User("admin", "admin", "Admin"));
                _users.Add(new User("user", "user", "User"));
                SaveUsersToFile();
                return;
            }

            string[] lines = File.ReadAllLines(UserFilePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split(';');
                if (parts.Length < 3)
                    continue;

                string username = parts[0];
                string password = parts[1];

                // Dacă e fișier vechi (cu "True"/"False"), convertim în "Admin"/"User"
                string role = parts[2];
                if (role == "True") role = "Admin";
                else if (role == "False") role = "User";

                _users.Add(new User(username, password, role));
            }
        }
        private static void SaveUsersToFile()
        {
            var lines = new List<string>();

            foreach (var user in _users)
            {
                string line = $"{user.Username};{user.Password};{user.Role}";
                lines.Add(line);
            }

            File.WriteAllLines(UserFilePath, lines);
        }
        private static void ShowLoginScreen()
        {
            PrintHeader("Anmeldung");

            Console.Write("Benutzername: ");
            string? username = Console.ReadLine();

            Console.Write("Passwort: ");
            string? password = ReadPasswordMasked();

            User? found = _users.Find(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)
                && u.Password == password);

            if (found == null)
            {
                PrintError("Anmeldung fehlgeschlagen. Benutzername oder Passwort ist falsch.");
                WaitForKey();
                return;
            }

            _currentUser = found;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine($"Erfolgreich angemeldet als: {_currentUser.Username} (Rolle: {_currentUser.Role})");
            Console.ResetColor();
            // Keine Pause mehr – direkt zurück, Main-Schleife zeigt Menü
        }
        private static void ShowAdminMenu()
        {
            while (true)
            {
                string menuTitle = _currentUser?.Role == "SuperAdmin" ? "Global Administrator-Menü" : "Admin-Menü";
                PrintHeader(menuTitle);

                Console.WriteLine("1) Alle Geräte anzeigen");
                Console.WriteLine("2) Neues Gerät anlegen");
                Console.WriteLine("3) Gerät sperren/entsperren (Defekt)");
                Console.WriteLine("4) Neuen Benutzer anlegen");
                Console.WriteLine("5) Benutzer löschen");
                Console.WriteLine("6) Abmelden");
                Console.WriteLine("0) Programm beenden");
                Console.WriteLine();
                Console.Write("Auswahl: ");

                string? input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        ShowAllDevices();
                        break;

                    case "2":
                        CreateNewDevice();
                        break;

                    case "3":
                        ToggleDeviceBlockStatus();
                        break;

                    case "4":
                        CreateNewUser();
                        break;

                    case "5":                // <-- Cazul nou pentru ștergere utilizator
                        DeleteUser();
                        break;

                    case "6":                // <-- Cazul de abmelden a devenit 5
                        _currentUser = null;
                        return;

                    case "0":
                        Environment.Exit(0);
                        break;

                    default:
                        PrintError("Ungültige Eingabe.");
                        break;
                }

                WaitForKey();
            }
        }
        private static void ShowUserMenu()
        {
            while (true)
            {
                PrintHeader("Benutzer-Menü");

                Console.WriteLine("1) Verfügbare Geräte anzeigen");
                Console.WriteLine("2) Gerät ausleihen");
                Console.WriteLine("3) Gerät zurückgeben"); // <-- Opțiune nouă
                Console.WriteLine("4) Abmelden");          // <-- Numărul a fost schimbat
                Console.WriteLine("0) Programm beenden");
                Console.WriteLine();
                Console.Write("Auswahl: ");

                string? input = Console.ReadLine();

                switch (input)
                {
                    case "1":
                        ShowAvailableDevices();
                        break;

                    case "2":
                        BorrowDevice();
                        break;

                    case "3":                // <-- Cazul nou pentru returnare
                        ReturnDevice();
                        break;

                    case "4":                // <-- Cazul de abmelden a devenit 4
                        _currentUser = null;
                        return;

                    case "0":
                        Environment.Exit(0);
                        break;

                    default:
                        PrintError("Ungültige Eingabe.");
                        break;
                }

                WaitForKey();
            }
        }
        private static void ShowAllDevices()
        {
            PrintHeader("Alle Geräte");

            if (_inventory.Devices.Count == 0)
            {
                Console.WriteLine("Keine Geräte vorhanden.");
                return;
            }

            foreach (var device in _inventory.Devices)
            {
                Console.WriteLine(device);
            }
        }
        private static void ShowAvailableDevices()
        {
            PrintHeader("Verfügbare Geräte");

            bool foundAny = false;

            foreach (var device in _inventory.GetAvailableDevices())
            {
                Console.WriteLine(device);
                foundAny = true;
            }

            if (!foundAny)
            {
                Console.WriteLine("Es sind aktuell keine Geräte verfügbar.");
            }
        }
        private static void CreateNewDevice()
        {
            PrintHeader("Neues Gerät anlegen");

            Console.Write("Geräte-ID (Ganzzahl, oder [Enter] für Abbruch): ");
            int id = ReadInt();
            if (id == -1) return;

            Console.Write("Name des Geräts (oder [Enter] für Abbruch): ");
            string? name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Vorgang abgebrochen.");
                return;
            }

            Console.Write("Kategorie (z.B. Laptop, Monitor oder [Enter] für Abbruch): ");
            string? category = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(category))
            {
                Console.WriteLine("Vorgang abgebrochen.");
                return;
            }

            if (_inventory.Devices.Any(d => d.Id == id))
            {
                PrintError("Ein Gerät mit dieser ID existiert bereits.");
                return;
            }

            _inventory.AddDevice(new Device(id, name.Trim(), category.Trim()));
            _inventory.SaveDevicesToFile();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Gerät wurde erfolgreich angelegt.");
            Console.ResetColor();
        }
        private static void ToggleDeviceBlockStatus()
        {
            PrintHeader("Gerät sperren / entsperren");

            Console.WriteLine("Liste aller Geräte:");
            foreach (var d in _inventory.Devices)
            {
                Console.WriteLine(d);
            }

            Console.WriteLine();
            Console.Write("Geben Sie die Geräte-ID ein, die Sie sperren/entsperren möchten (oder [Enter] für Abbruch): ");

            int id = ReadInt();
            if (id == -1) return;

            var device = _inventory.FindById(id);

            if (device == null)
            {
                PrintError("Ein Gerät mit dieser ID existiert nicht.");
                return;
            }

            if (!device.IsAvailable && !device.IsBlocked)
            {
                PrintError($"Das Gerät [{device.Id}] ist aktuell an '{device.BorrowedBy}' ausgeliehen und kann erst nach der Rückgabe gesperrt werden.");
                return;
            }

            if (device.IsBlocked)
            {
                device.UnblockDevice();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nGerät [{device.Id}] \"{device.Name}\" wurde erfolgreich ENTSPERRT und steht wieder zur Verfügung.");
            }
            else
            {
                device.BlockDevice();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\nGerät [{device.Id}] \"{device.Name}\" wurde erfolgreich GESPERRT (z.B. Defekt).");
            }

            Console.ResetColor();
            _inventory.SaveDevicesToFile();
        }
        private static void CreateNewUser()
        {
            if (_currentUser == null) return;

            PrintHeader("Neuen Benutzer anlegen");

            // Mutăm avertismentul la începutul metodei, înainte de orice introducere de date
            if (_currentUser.Role != "SuperAdmin")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Hinweis: Als Admin können Sie nur normale Benutzer (User) anlegen.");
                Console.ResetColor();
                Console.WriteLine();
            }

            // Modificați rândul cu Benutzername și validarea sa
            Console.Write("Benutzername (oder [Enter] für Abbruch): ");
            string? username = Console.ReadLine();

            // Dacă apasă Enter și textul este gol, oprim și dăm mesaj de anulare
            if (string.IsNullOrWhiteSpace(username))
            {
                Console.WriteLine("Vorgang abgebrochen.");
                return;
            }

            if (_users.Exists(u => string.Equals(u.Username, username.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                PrintError("Ein Benutzer mit diesem Namen existiert bereits.");
                return;
            }

            // Modificați și la Parolă, pentru consecvență
            Console.Write("Passwort (oder [Enter] für Abbruch): ");
            string? password = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Vorgang abgebrochen.");
                return;
            }

            string newRole = "User"; // Implicit va fi user

            // Doar SuperAdmin primește întrebarea de a alege rolul
            if (_currentUser.Role == "SuperAdmin")
            {
                Console.Write("Soll der Benutzer ein [A]dmin oder normaler [U]ser sein? (a/u): ");
                string? roleInput = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(roleInput) && roleInput.Trim().ToLower() == "a")
                {
                    newRole = "Admin";
                }
            }

            _users.Add(new User(username.Trim(), password.Trim(), newRole));
            SaveUsersToFile();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nBenutzer '{username.Trim()}' (Rolle: {newRole}) wurde erfolgreich angelegt.");
            Console.ResetColor();
        }
        private static void DeleteUser()
        {
            PrintHeader("Benutzer löschen");

            Console.WriteLine("Liste aller aktuellen Benutzer:");
            foreach (var user in _users)
            {
                Console.WriteLine($"- {user.Username} ({user.Role})");
            }

            Console.WriteLine();
            Console.Write("Geben Sie den Benutzernamen ein, der gelöscht werden soll (oder [Enter] für Abbruch): ");
            string? targetUsername = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(targetUsername))
            {
                Console.WriteLine("Vorgang abgebrochen.");
                return;
            }

            targetUsername = targetUsername.Trim();
            var userToDelete = _users.Find(u => string.Equals(u.Username, targetUsername, StringComparison.OrdinalIgnoreCase));
            // Un Admin simplu nu are voie să șteargă un alt Admin sau un SuperAdmin
            if (_currentUser?.Role == "Admin" && (userToDelete!.Role == "Admin" || userToDelete!.Role == "SuperAdmin"))
            {
                PrintError("Fehlende Berechtigung: Sie können als Admin keine anderen Administratoren oder SuperAdmins löschen.");
                return;
            }


            if (userToDelete == null)
            {
                PrintError("Ein Benutzer mit diesem Namen wurde nicht gefunden.");
                return;
            }

            // Prevenim situația în care administratorul se șterge pe sine însuși
            if (string.Equals(userToDelete.Username, _currentUser?.Username, StringComparison.OrdinalIgnoreCase))
            {
                PrintError("Sie können sich nicht selbst löschen.");
                return;
            }

            // Verificăm dacă utilizatorul are echipamente împrumutate
            bool hasBorrowedDevices = _inventory.Devices.Any(d => string.Equals(d.BorrowedBy, userToDelete.Username, StringComparison.OrdinalIgnoreCase));

            if (hasBorrowedDevices)
            {
                PrintError($"Der Benutzer '{userToDelete.Username}' kann nicht gelöscht werden, da er noch Geräte ausgeliehen hat.");
                return;
            }

            // Confirmare finală înainte de ștergere
            Console.Write($"Möchten Sie den Benutzer '{userToDelete.Username}' wirklich löschen? (j/n): ");
            string? confirmation = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(confirmation) && confirmation.Trim().ToLower() == "j")
            {
                _users.Remove(userToDelete);
                SaveUsersToFile(); // Salvăm modificarea în fișier

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine();
                Console.WriteLine($"Benutzer '{userToDelete.Username}' wurde erfolgreich gelöscht.");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("Löschvorgang abgebrochen.");
            }
        }
        private static void BorrowDevice()
        {
            if (_currentUser == null)
                return;

            PrintHeader("Gerät ausleihen");

            bool foundAny = false;
            foreach (var device in _inventory.GetAvailableDevices())
            {
                Console.WriteLine(device);
                foundAny = true;
            }

            if (!foundAny)
            {
                Console.WriteLine();
                Console.WriteLine("Es sind aktuell keine Geräte verfügbar.");
                return;
            }

            Console.WriteLine();
            Console.Write("Bitte die Geräte-ID eingeben (oder [Enter] für Abbruch): ");

            int id = ReadInt();
            if (id == -1) return;

            var selected = _inventory.FindById(id);

            if (selected == null)
            {
                PrintError("Ein Gerät mit dieser ID existiert nicht.");
                return;
            }

            if (selected.IsBlocked)
            {
                PrintError("Dieses Gerät ist aktuell gesperrt (z.B. Defekt) und kann nicht ausgeliehen werden.");
                return;
            }

            if (!selected.IsAvailable)
            {
                PrintError("Dieses Gerät ist bereits ausgeliehen.");
                return;
            }

            selected.MarkAsBorrowed(_currentUser);
            _inventory.SaveDevicesToFile();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine($"Gerät [{selected.Id}] \"{selected.Name}\" wurde erfolgreich ausgeliehen.");
            Console.ResetColor();
        }
        private static void ReturnDevice()
        {
            if (_currentUser == null)
                return;

            PrintHeader("Gerät zurückgeben");

            var myBorrowedDevices = _inventory.Devices
                .Where(d => d.BorrowedBy == _currentUser.Username)
                .ToList();

            if (myBorrowedDevices.Count == 0)
            {
                Console.WriteLine("Sie haben aktuell keine Geräte ausgeliehen.");
                return;
            }

            foreach (var device in myBorrowedDevices)
            {
                Console.WriteLine(device);
            }

            Console.WriteLine();
            Console.Write("Bitte die Geräte-ID eingeben, die Sie zurückgeben möchten (oder [Enter] für Abbruch): ");

            int id = ReadInt();
            if (id == -1) return;

            var selected = myBorrowedDevices.Find(d => d.Id == id);

            if (selected == null)
            {
                PrintError("Ungültige ID oder dieses Gerät gehört nicht zu Ihren Ausleihen.");
                return;
            }

            selected.MarkAsReturned();
            _inventory.SaveDevicesToFile();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine($"Gerät [{selected.Id}] \"{selected.Name}\" wurde erfolgreich zurückgegeben.");
            Console.ResetColor();
        }
        private static int ReadInt()
        {
            while (true)
            {
                string? input = Console.ReadLine();

                // Dacă utilizatorul nu introduce nimic (apasă doar Enter), considerăm că vrea să anuleze
                if (string.IsNullOrWhiteSpace(input))
                {
                    return -1; // -1 va semnifica "Abbruch" în logica noastră
                }

                // Dacă a introdus ceva, verificăm dacă este număr valid
                if (!int.TryParse(input, out int value))
                {
                    PrintError("Bitte eine gültige ganze Zahl eingeben (oder [Enter] drücken zum Abbrechen).");
                    Console.Write("Eingabe wiederholen: ");
                    continue;
                }

                return value;
            }
        }
        private static string ReadPasswordMasked()
        {
            var password = string.Empty;

            while (true)
            {
                var key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password = password[..^1];
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
            }

            return password;
        }
        private static void PrintHeader(string title)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("              TechGear                  ");
            Console.WriteLine("========================================");
            Console.WriteLine("      IT Inventar und Ausleihsystem     ");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine(title);
            Console.WriteLine(new string('-', title.Length));
            Console.WriteLine();
        }
        private static void PrintError(string message)
        {
            var old = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine(message);
            Console.ForegroundColor = old;
        }
        private static void WaitForKey()
        {
            Console.WriteLine();
            Console.Write("Weiter mit beliebiger Taste...");
            Console.ReadKey(intercept: true);
        }
    }
}
