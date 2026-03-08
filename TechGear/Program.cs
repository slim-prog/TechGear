namespace TechGear
{
    using System;
    using System.IO;
    using System.Linq;

    internal class Program
    {
        private static readonly UserManager _userManager = new();
        private static readonly InventoryManager _inventory = new();
        private static User? _currentUser;

        private static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "TechGear";

            _userManager.LoadUsersFromFile();
            _inventory.LoadDevicesFromFile();

            while (true)
            {
                Console.Clear();

                if (_currentUser == null)
                {
                    ShowLoginScreen();
                }
                else if (_currentUser.Role == UserRole.SuperAdmin || _currentUser.Role == UserRole.Admin)
                {
                    ShowAdminMenu();
                }
                else
                {
                    ShowUserMenu();
                }
            }
        }

        private static void ShowLoginScreen()
        {
            PrintHeader("Anmeldung");

            Console.Write("Benutzername: ");
            string? username = Console.ReadLine();

            Console.Write("Passwort: ");
            string? password = ReadPasswordMasked();

            // Delegierung der Validierung an den UserManager (inklusive Hashing)
            User? found = _userManager.ValidateLogin(username ?? "", password ?? "");

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
        }

        private static void ShowAdminMenu()
        {
            while (true)
            {
                string menuTitle = _currentUser?.Role == UserRole.SuperAdmin ? "Global Administrator-Menü" : "Admin-Menü";
                PrintHeader(menuTitle);

                Console.WriteLine("1) Alle Geräte anzeigen");
                Console.WriteLine("2) Geräte suchen");              // neue Funktion
                Console.WriteLine("3) Neues Gerät anlegen");
                Console.WriteLine("4) Gerät sperren/entsperren (Defekt)");
                Console.WriteLine("5) Neuen Benutzer anlegen");
                Console.WriteLine("6) Benutzer löschen");
                Console.WriteLine("7) Ausleih-Historie anzeigen");
                Console.WriteLine("8) Abmelden");
                Console.WriteLine("R) Benutzer-Passwort zurücksetzen");     // neue Funktion
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
                        SearchForDevice();
                        break;
                    case "3":
                        CreateNewDevice();
                        break;
                    case "4":
                        ToggleDeviceBlockStatus();
                        break;
                    case "5":
                        CreateNewUser();
                        break;
                    case "6":
                        DeleteUser();
                        break;
                    case "7":
                        PrintHeader("Ausleih-Historie");
                        Logger.ShowHistory();
                        break;
                    case "8":
                        _currentUser = null;
                        return;
                    case "R":
                        ResetPasswordMenu();
                        break;
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
                Console.WriteLine("3) Gerät zurückgeben");
                Console.WriteLine("4) Geräte suchen");              // neue Funktion 
                Console.WriteLine("5) Abmelden");
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
                    case "3":
                        ReturnDevice();
                        break;
                    case "4":
                        SearchForDevice();
                        break;
                    case "5":
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
                PrintDeviceColored(device);
            }
        }

        private static void ShowAvailableDevices()
        {
            PrintHeader("Verfügbare Geräte");

            bool foundAny = false;

            foreach (var device in _inventory.GetAvailableDevices())
            {
                PrintDeviceColored(device);
                foundAny = true;
            }

            if (!foundAny)
            {
                Console.WriteLine("Es sind aktuell keine Geräte verfügbar.");
            }
        }

        private static void SearchForDevice()
        {
            PrintHeader("Geräte suchen");

            Console.Write("Bitte geben Sie einen Suchbegriff ein (z.B. Marke, Kategorie oder [Enter] für Abbruch): ");
            string? searchTerm = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                Console.WriteLine("Suche abgebrochen.");
                return;
            }

            // Wir rufen die neue Suchmethode aus dem InventoryManager auf
            var results = _inventory.SearchDevices(searchTerm).ToList();

            Console.WriteLine();

            if (results.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Keine Geräte gefunden, die den Begriff '{searchTerm}' enthalten.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Es wurden {results.Count} Gerät(e) gefunden:");
                Console.ResetColor();

                foreach (var device in results)
                {
                    PrintDeviceColored(device);
                }
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
                Logger.LogEvent(_currentUser!.Username, "entsperrt (repariert)", device.Name);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nGerät [{device.Id}] \"{device.Name}\" wurde erfolgreich ENTSPERRT und steht wieder zur Verfügung.");
            }
            else
            {
                device.BlockDevice();
                Logger.LogEvent(_currentUser!.Username, "gesperrt (defekt)", device.Name);

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

            if (_currentUser.Role != UserRole.SuperAdmin)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Hinweis: Als Admin können Sie nur normale Benutzer (User) anlegen.");
                Console.ResetColor();
                Console.WriteLine();
            }

            Console.Write("Benutzername (oder [Enter] für Abbruch): ");
            string? username = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(username))
            {
                Console.WriteLine("Vorgang abgebrochen.");
                return;
            }

            if (_userManager.UserExists(username.Trim()))
            {
                PrintError("Ein Benutzer mit diesem Namen existiert bereits.");
                return;
            }

            Console.Write("Passwort (oder [Enter] für Abbruch): ");
            string? password = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Vorgang abgebrochen.");
                return;
            }

            UserRole newRole = UserRole.User;

            if (_currentUser.Role == UserRole.SuperAdmin)
            {
                Console.Write("Soll der Benutzer ein [A]dmin oder normaler [U]ser sein? (a/u): ");
                string? roleInput = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(roleInput) && roleInput.Trim().ToLower() == "a")
                {
                    newRole = UserRole.Admin;
                }
            }

            // Passwort vor dem Speichern hashen
            string hashedPassword = SecurityHelper.HashPassword(password.Trim());
            _userManager.AddUser(new User(username.Trim(), hashedPassword, newRole));
            _userManager.SaveUsersToFile();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nBenutzer '{username.Trim()}' (Rolle: {newRole}) wurde erfolgreich angelegt.");
            Console.ResetColor();
        }

        private static void DeleteUser()
        {
            PrintHeader("Benutzer löschen");

            Console.WriteLine("Liste aller aktuellen Benutzer:");
            foreach (var user in _userManager.Users)
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
            var userToDelete = _userManager.FindByUsername(targetUsername);

            if (userToDelete == null)
            {
                PrintError("Ein Benutzer mit diesem Namen wurde nicht gefunden.");
                return;
            }

            // Strikte typsichere Berechtigungsprüfung mit Enum
            if (_currentUser?.Role == UserRole.Admin && (userToDelete.Role == UserRole.Admin || userToDelete.Role == UserRole.SuperAdmin))
            {
                PrintError("Fehlende Berechtigung: Sie können als Admin keine anderen Administratoren oder SuperAdmins löschen.");
                return;
            }

            if (string.Equals(userToDelete.Username, _currentUser?.Username, StringComparison.OrdinalIgnoreCase))
            {
                PrintError("Sie können sich nicht selbst löschen.");
                return;
            }

            bool hasBorrowedDevices = _inventory.Devices.Any(d => string.Equals(d.BorrowedBy, userToDelete.Username, StringComparison.OrdinalIgnoreCase));

            if (hasBorrowedDevices)
            {
                PrintError($"Der Benutzer '{userToDelete.Username}' kann nicht gelöscht werden, da er noch Geräte ausgeliehen hat.");
                return;
            }

            Console.Write($"Möchten Sie den Benutzer '{userToDelete.Username}' wirklich löschen? (j/n): ");
            string? confirmation = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(confirmation) && confirmation.Trim().ToLower() == "j")
            {
                _userManager.RemoveUser(userToDelete);
                _userManager.SaveUsersToFile();

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
            if (_currentUser == null) return;

            PrintHeader("Gerät ausleihen");

            bool foundAny = false;
            foreach (var device in _inventory.GetAvailableDevices())
            {
                PrintDeviceColored(device);
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

            Logger.LogEvent(_currentUser.Username, "ausgeliehen", selected.Name);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine($"Gerät [{selected.Id}] \"{selected.Name}\" wurde erfolgreich ausgeliehen.");
            Console.ResetColor();

        }

        private static void ReturnDevice()
        {
            if (_currentUser == null) return;

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

            Logger.LogEvent(_currentUser.Username, "zurückgegeben", selected.Name);

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
                if (string.IsNullOrWhiteSpace(input)) return -1;

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

        private static void ResetPasswordMenu()
        {
            PrintHeader("Benutzer-Passwort zurücksetzen");

            if (_currentUser == null) return;

            Console.Write("Benutzername des Ziel-Accounts: ");
            string? targetUsername = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(targetUsername))
            {
                PrintError("Benutzername darf nicht leer sein.");
                return;
            }

            // Nu ne permitem să ne resetăm singuri parola aici (opțional, dar recomandat)
            if (string.Equals(targetUsername, _currentUser.Username, StringComparison.OrdinalIgnoreCase))
            {
                PrintError("Sie können Ihr eigenes Passwort hier nicht ändern.");
                return;
            }

            Console.Write("Neues Passwort vergeben: ");
            string newPassword = ReadPasswordMasked();

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                PrintError("Das neue Passwort muss mindestens 6 Zeichen lang sein.");
                return;
            }

            // Apelăm UserManager pentru a face resetarea, respectând ierarhia
            bool success = _userManager.ResetUserPassword(_currentUser, targetUsername, newPassword);

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nPasswort wurde erfolgreich zurückgesetzt.");
                Console.ResetColor();
                /// Logger.LogEvent(_currentUser.Username, $"Passwort von '{targetUsername}' zurückgesetzt" , "sysadmin" ); // Dacă folosești Logger-ul
            }
            else
            {
                PrintError("Fehler: Benutzer nicht gefunden oder unzureichende Berechtigungen (ein Admin darf keinen SuperAdmin ändern).");
            }
        }

        private static void PrintHeader(string title)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("                TechGear                ");
            Console.WriteLine("========================================");
            Console.WriteLine("     IT Inventar und Ausleihsystem      ");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine();

            int totalWidth = 40;
            int spacesToCenter = (totalWidth - title.Length) / 2;

            if (spacesToCenter > 0)
            {
                string padding = new string(' ', spacesToCenter);
                Console.WriteLine($"{padding}{title}");
                Console.WriteLine($"{padding}{new string('-', title.Length)}");
            }
            else
            {
                Console.WriteLine(title);
                Console.WriteLine(new string('-', title.Length));
            }
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

        private static void PrintDeviceColored(Device device)
        {
            // Printează ID-ul cu Cyan
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(device.Id);
            Console.ResetColor();
            Console.Write($"] {device.Name} ({device.Category}) - Status: ");

            // Printează Starea cu culoarea potrivită
            if (device.IsBlocked)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("GESPERRT");
            }
            // Dacă NU este disponibil și NU este blocat, înseamnă că este împrumutat
            else if (!device.IsAvailable)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                // Verificăm dacă știm la cine este (BorrowedBy)
                if (!string.IsNullOrEmpty(device.BorrowedBy))
                {
                    Console.WriteLine($"AUSGELIEHEN (von {device.BorrowedBy})");
                }
                else
                {
                    Console.WriteLine("AUSGELIEHEN");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("VERFÜGBAR");
            }

            Console.ResetColor(); // Ne asigurăm că setăm culoarea la loc
        }



    }
}
