namespace TechGear
{
    using System;
    using System.Linq;

    /// <summary>
    /// Die Hauptklasse der Anwendung. Steuert den Programmfluss (Main Loop) 
    /// und verwaltet die Benutzeroberfläche (Console UI).
    /// </summary>
    internal class Program
    {
        // Unsere beiden "Helfer", die die eigentliche Arbeit im Hintergrund machen:
        // _userManager kümmert sich um alle Benutzer und Passwörter.
        // _inventory verwaltet alle Geräte und Ausleihen.
        // Das Wort 'readonly' (nur lesen) schützt diese Variablen davor, später im Code aus Versehen gelöscht oder durch etwas anderes ersetzt zu werden.
        private static readonly UserManager _userManager = new();
        private static readonly InventoryManager _inventory = new();

        // Speichert den aktuell am System angemeldeten Benutzer (Session-State).
        // Ist null, wenn sich der Benutzer abmeldet oder die Anwendung frisch gestartet wird.
        private static User? _currentUser;

        /// <summary>
        /// Der Haupteinstiegspunkt (Entry Point) der Konsolenanwendung.
        /// </summary>
        private static void Main()
        {
            // Setzt die Konsolenausgabe auf UTF-8, um Sonderzeichen (z. B. Umlaute) korrekt darzustellen.
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Ändert den Fenstertitel der Konsolenanwendung
            Console.Title = "TechGear";

            // Initialisierung: Lädt persistente Daten (Benutzer und Inventar) aus den CSV-Dateien in den Arbeitsspeicher.
            _userManager.LoadUsersFromFile();
            _inventory.LoadDevicesFromFile();

            // Endlose Hauptschleife (Main Loop), die die Anwendung am Leben hält, 
            // bis sie explizit beendet wird (z. B. durch Schließen des Fensters).
            while (true)
            {
                // Löscht die Konsole bei jedem Durchlauf für eine saubere Menüdarstellung
                Console.Clear();

                // Routing-Logik basierend auf dem Authentifizierungsstatus und der Benutzerrolle (RBAC)
                if (_currentUser == null)
                {
                    // Fall 1: Niemand ist angemeldet -> Login-Bildschirm anzeigen
                    ShowLoginScreen();
                }
                else if (_currentUser.Role == UserRole.SuperAdmin || _currentUser.Role == UserRole.Admin)
                {
                    // Fall 2: Ein Administrator oder SuperAdmin ist angemeldet -> Admin-Dashboard anzeigen
                    ShowAdminMenu();
                }
                else
                {
                    // Fall 3: Ein regulärer Benutzer ist angemeldet -> Standard-Benutzermenü anzeigen
                    ShowUserMenu();
                }
            }
        }

        private static void ShowLoginScreen()
        {
            // Zeigt eine schöne Überschrift für den Login-Bereich an
            PrintHeader("Anmeldung");

            // Fragt den Benutzernamen ab und speichert ihn
            Console.Write("Benutzername: ");
            string? username = Console.ReadLine();

            // Fragt das Passwort ab. Wir nutzen hier unsere eigene Methode, damit man beim Tippen nur Sternchen (*) auf dem Bildschirm sieht.
            Console.Write("Passwort: ");
            string? password = ReadPasswordMasked();

            // Wir fragen unseren 'Helfer' (den UserManager), ob Name und Passwort stimmen.
            // Die '?? ""' bedeuten: Wenn der Benutzer einfach nur Enter gedrückt hat (ohne Text), schicken wir einen leeren Text weiter, damit das Programm nicht abstürzt.
            User? found = _userManager.ValidateLogin(username ?? "", password ?? "");

            // Wenn der Benutzer nicht gefunden wurde (Name oder Passwort war falsch)
            if (found == null)
            {
                PrintError("Anmeldung fehlgeschlagen. Benutzername oder Passwort ist falsch.");
                // Wir warten auf einen Tastendruck, damit der Benutzer die Fehlermeldung noch lesen kann, bevor der Bildschirm im nächsten Durchlauf wieder gelöscht wird.
                WaitForKey();
                return; // Bricht den Login-Vorgang hier ab
            }

            // Wenn Name und Passwort stimmen, merken wir uns diesen Benutzer für den Rest des Programms.
            // So weiß das System, wer gerade angemeldet ist.
            _currentUser = found;

            // Zeigt eine grüne Erfolgsmeldung an, damit man sieht, dass es geklappt hat
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
                Console.WriteLine("2) Geräte suchen");
                Console.WriteLine("3) Neues Gerät anlegen");
                Console.WriteLine("4) Gerät sperren/entsperren (Defekt)");
                Console.WriteLine("5) Neuen Benutzer anlegen");
                Console.WriteLine("6) Benutzer löschen");
                Console.WriteLine("7) Ausleih-Historie anzeigen");
                Console.WriteLine("8) Abmelden");
                Console.WriteLine("R) Benutzer-Passwort zurücksetzen");
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
                Console.WriteLine("4) Geräte suchen");
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
            // Überschrift für dieses Menü
            PrintHeader("Gerät sperren / entsperren");

            // Zuerst zeigen wir dem Admin alle Geräte an, damit er die richtige ID findet
            Console.WriteLine("Liste aller Geräte:");
            foreach (var d in _inventory.Devices)
            {
                Console.WriteLine(d); // Nutzt automatisch unsere ToString()Methode oder unsere farbige Methode
            }

            Console.WriteLine();
            Console.Write("Geben Sie die Geräte-ID ein, die Sie sperren/entsperren möchten (oder [Enter] für Abbruch): ");

            // Wir versuchen, eine Zahl einzulesen. Wenn der Admin nur Enter drückt, 
            // bekommen wir -1 zurück und brechen den Vorgang hier ab.
            int id = ReadInt();
            if (id == -1) return;

            // Wir suchen das Gerät in unserer Liste
            var device = _inventory.FindById(id);

            // Wenn es kein Gerät mit dieser ID gibt, zeigen wir eine Fehlermeldung
            if (device == null)
            {
                PrintError("Ein Gerät mit dieser ID existiert nicht.");
                return;
            }

            // Wichtige Prüfung: Wir dürfen kein Gerät sperren, das gerade jemand zu Hause hat!
            // (!device.IsAvailable bedeutet: Es ist nicht hier. !device.IsBlocked bedeutet: Es ist nicht schon gesperrt.)
            if (!device.IsAvailable && !device.IsBlocked)
            {
                PrintError($"Das Gerät [{device.Id}] ist aktuell an '{device.BorrowedBy}' ausgeliehen und kann erst nach der Rückgabe gesperrt werden.");
                return;
            }

            // Wenn das Gerät schon gesperrt ist, machen wir es wieder verfügbar (entsperren)
            if (device.IsBlocked)
            {
                device.UnblockDevice(); // Ändert den Status im Gerät

                // Wir schreiben in unsere Log-Datei (log.txt), wer das Gerät entsperrt hat
                Logger.LogEvent(_currentUser!.Username, "entsperrt (repariert)", device.Name);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\nGerät [{device.Id}] \"{device.Name}\" wurde erfolgreich ENTSPERRT und steht wieder zur Verfügung.");
            }
            // Andernfalls (wenn es verfügbar ist), sperren wir es jetzt (z.B. weil es kaputt ist)
            else
            {
                device.BlockDevice(); // Ändert den Status im Gerät

                // Auch das Sperren wird in der Log-Datei festgehalten
                Logger.LogEvent(_currentUser!.Username, "gesperrt (defekt)", device.Name);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\nGerät [{device.Id}] \"{device.Name}\" wurde erfolgreich GESPERRT (z.B. Defekt).");
            }

            // Am Ende stellen wir die normale Textfarbe wieder her
            Console.ResetColor();

            // Ganz wichtig: Wir speichern die Änderungen in unserer CSV-Datei, 
            // damit das Gerät auch nach einem Neustart des Programms noch gesperrt/entsperrt bleibt.
            _inventory.SaveDevicesToFile();
        }

        private static void CreateNewUser()
        {
            // Wenn niemand angemeldet ist, dürfen wir hier gar nicht erst weitermachen.
            if (_currentUser == null) return;

            // Menü-Überschrift
            PrintHeader("Neuen Benutzer anlegen");

            // Ein kleiner Info-Text für normale Admins, damit sie wissen, dass sie keine Kollegen (Admins) anlegen können.
            if (_currentUser.Role != UserRole.SuperAdmin)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Hinweis: Als Admin können Sie nur normale Benutzer (User) anlegen.");
                Console.ResetColor();
                Console.WriteLine();
            }

            Console.Write("Benutzername (oder [Enter] für Abbruch): ");
            string? username = Console.ReadLine();

            // Bricht ab, wenn der Name leer ist
            if (string.IsNullOrWhiteSpace(username))
            {
                Console.WriteLine("Vorgang abgebrochen.");
                return;
            }

            // Wir prüfen bei unserem 'Helfer' (UserManager), ob es diesen Namen schon in der Liste gibt.
            // Zwei Benutzer mit dem gleichen Namen würden das System durcheinanderbringen!
            if (_userManager.UserExists(username.Trim()))
            {
                PrintError("Ein Benutzer mit diesem Namen existiert bereits.");
                return;
            }

            // ACHTUNG: Hier tippt der Admin das Passwort für den neuen Benutzer ein.
            // (Normalerweise würden wir es verstecken, aber beim Anlegen ist das erstmal okay so).
            Console.Write("Passwort (oder [Enter] für Abbruch): ");
            string? password = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Vorgang abgebrochen.");
                return;
            }

            // Standardmäßig bekommt jeder neue Benutzer erstmal nur die einfache "User" Rolle
            UserRole newRole = UserRole.User;

            // Nur der SuperAdmin darf bei Bedarf entscheiden, ob der Neue auch ein Admin wird
            if (_currentUser.Role == UserRole.SuperAdmin)
            {
                Console.Write("Soll der Benutzer ein [A]dmin oder normaler [U]ser sein? (a/u): ");
                string? roleInput = Console.ReadLine();

                // Wenn er "a" oder "A" tippt, ändern wir die Rolle von User auf Admin.
                if (!string.IsNullOrWhiteSpace(roleInput) && roleInput.Trim().ToLower() == "a")
                {
                    newRole = UserRole.Admin;
                }
            }

            // SEHR WICHTIG (Sicherheit): Wir speichern niemals das normale Passwort!
            // Der SecurityHelper macht aus "Hallo123" einen langen, unlesbaren Text (Hash-Wert)
            string hashedPassword = SecurityHelper.HashPassword(password.Trim());

            // Wir bauen den neuen Benutzer zusammen und geben ihn unserem Helfer, der ihn in die Liste aufnimmt.
            _userManager.AddUser(new User(username.Trim(), hashedPassword, newRole));

            // Damit der neue Benutzer auch nach dem Neustart noch da ist, speichern wir sofort in die CSV-Datei.
            _userManager.SaveUsersToFile();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\nBenutzer '{username.Trim()}' (Rolle: {newRole}) wurde erfolgreich angelegt.");
            Console.ResetColor();
        }

        private static void DeleteUser()
        {
            // Überschrift für dieses Menü
            PrintHeader("Benutzer löschen");

            // Zuerst zeigen wir eine kleine Liste aller Benutzer an, 
            // damit der Admin/SuperAdmin weiß, wer überhaupt im System existiert.
            Console.WriteLine("Liste aller aktuellen Benutzer:");
            foreach (var user in _userManager.Users)
            {
                Console.WriteLine($"- {user.Username} ({user.Role})");
            }

            Console.WriteLine();
            Console.Write("Geben Sie den Benutzernamen ein, der gelöscht werden soll (oder [Enter] für Abbruch): ");
            string? targetUsername = Console.ReadLine();

            // Wenn nur Enter gedrückt wurde oder nichts eingetippt wurde, brechen wir hier ab.
            if (string.IsNullOrWhiteSpace(targetUsername))
            {
                Console.WriteLine("Vorgang abgebrochen.");
                return;
            }

            // .Trim() schneidet versehentliche Leerzeichen am Anfang und Ende weg
            targetUsername = targetUsername.Trim();
            var userToDelete = _userManager.FindByUsername(targetUsername);

            // Existiert dieser Name überhaupt in unserer CSV/Liste?
            if (userToDelete == null)
            {
                PrintError("Ein Benutzer mit diesem Namen wurde nicht gefunden.");
                return;
            }

            // WICHTIGE REGEL: Ein normaler Admin darf keine anderen Admins oder SuperAdmins löschen!
            // (Nur ein SuperAdmin darf andere Admins löschen)
            if (_currentUser?.Role == UserRole.Admin && (userToDelete.Role == UserRole.Admin || userToDelete.Role == UserRole.SuperAdmin))
            {
                PrintError("Fehlende Berechtigung: Sie können als Admin keine anderen Administratoren oder SuperAdmins löschen.");
                return;
            }

            // Sicherheitsprüfung: Man darf sich nicht aus Versehen selbst aus dem System aussperren!
            if (string.Equals(userToDelete.Username, _currentUser?.Username, StringComparison.OrdinalIgnoreCase))
            {
                PrintError("Sie können sich nicht selbst löschen.");
                return;
            }

            // GANZ WICHTIG: Hat dieser Benutzer noch Laptops oder Monitore zu Hause?
            // Mit '.Any()' prüfen wir schnell, ob in der Inventarliste irgendwo der Name des Benutzers steht.
            bool hasBorrowedDevices = _inventory.Devices.Any(d => string.Equals(d.BorrowedBy, userToDelete.Username, StringComparison.OrdinalIgnoreCase));

            if (hasBorrowedDevices)
            {
                // Wenn er noch Geräte hat, sperren wir das Löschen, sonst gehen uns die Geräte "verloren".
                PrintError($"Der Benutzer '{userToDelete.Username}' kann nicht gelöscht werden, da er noch Geräte ausgeliehen hat.");
                return;
            }

            // Letzte Sicherheitsabfrage, bevor wirklich etwas gelöscht wird
            Console.Write($"Möchten Sie den Benutzer '{userToDelete.Username}' wirklich löschen? (j/n): ");
            string? confirmation = Console.ReadLine();

            // Nur wenn genau "j" (oder "J", dank ToLower()) eingegeben wurde, löschen wir den Benutzer.
            if (!string.IsNullOrWhiteSpace(confirmation) && confirmation.Trim().ToLower() == "j")
            {
                // Wir entfernen den Benutzer aus unserer Liste im Arbeitsspeicher
                _userManager.RemoveUser(userToDelete);

                // Und wir überschreiben die CSV-Datei, damit er für immer weg ist
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
            // Sicherheitsprüfung: Abbruch, falls kein Benutzer angemeldet ist
            if (_currentUser == null) return;

            PrintHeader("Gerät ausleihen");

            bool foundAny = false;
            // Durchläuft die Liste aller aktuell verfügbaren Geräte und gibt diese farbig formatiert aus
            foreach (var device in _inventory.GetAvailableDevices())
            {
                PrintDeviceColored(device);
                foundAny = true; // Markiert, dass mindestens ein Gerät zur Auswahl steht
            }

            // Falls die Liste leer ist, wird der Vorgang mit einer entsprechenden Meldung abgebrochen
            if (!foundAny)
            {
                Console.WriteLine();
                Console.WriteLine("Es sind aktuell keine Geräte verfügbar.");
                return;
            }

            Console.WriteLine();
            Console.Write("Bitte die Geräte-ID eingeben (oder [Enter] für Abbruch): ");

            // Einlesen der Geräte-ID. Rückgabewert -1 signalisiert den Abbruch durch den Benutzer.
            int id = ReadInt();
            if (id == -1) return;

            // Suchen des Geräts anhand der eingegebenen ID im gesamten Inventar
            var selected = _inventory.FindById(id);

            // Validierung 1: Existiert die ID im System?
            if (selected == null)
            {
                PrintError("Ein Gerät mit dieser ID existiert nicht.");
                return;
            }

            // Validierung 2: Ist das Gerät gesperrt (z. B. wegen Defekt)
            if (selected.IsBlocked)
            {
                PrintError("Dieses Gerät ist aktuell gesperrt (z.B. Defekt) und kann nicht ausgeliehen werden.");
                return;
            }

            // Validierung 3: Ist das Gerät bereits von jemand anderem ausgeliehen?
            // (Zusätzliche Sicherheitsprüfung, falls sich der Status zwischen Anzeige und Eingabe geändert hat)
            if (!selected.IsAvailable)
            {
                PrintError("Dieses Gerät ist bereits ausgeliehen.");
                return;
            }

            // Statusaktualisierung: Markiert das Gerät als ausgeliehen und hinterlegt den aktuellen Benutzer
            selected.MarkAsBorrowed(_currentUser);

            // Persistente Speicherung der Bestandsänderung in der CSV-Datei
            _inventory.SaveDevicesToFile();

            // Protokollierung der erfolgreichen Ausleihe im Audit-Log
            Logger.LogEvent(_currentUser.Username, "ausgeliehen", selected.Name);

            // Optische Bestätigung für den Benutzer in der Konsole
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine($"Gerät [{selected.Id}] \"{selected.Name}\" wurde erfolgreich ausgeliehen.");
            Console.ResetColor();
        }

        private static void ReturnDevice()
        {
            // Sicherheitsprüfung: Nur angemeldete Benutzer können Geräte zurückgeben
            if (_currentUser == null) return;

            PrintHeader("Gerät zurückgeben");

            // Filtern der Inventarliste mittels LINQ: Es werden nur Geräte ermittelt, 
            // die aktuell auf den Namen des angemeldeten Benutzers ausgeliehen sind
            var myBorrowedDevices = _inventory.Devices
                .Where(d => d.BorrowedBy == _currentUser.Username)
                .ToList();

            // Benutzerfreundliche Rückmeldung, falls keine Ausleihen vorhanden sind
            if (myBorrowedDevices.Count == 0)
            {
                Console.WriteLine("Sie haben aktuell keine Geräte ausgeliehen.");
                return;
            }

            // Anzeige aller Geräte, die der Benutzer aktuell besitzt
            foreach (var device in myBorrowedDevices)
            {
                Console.WriteLine(device);
            }

            Console.WriteLine();
            Console.Write("Bitte die Geräte-ID eingeben, die Sie zurückgeben möchten (oder [Enter] für Abbruch): ");

            // Einlesen und Validieren der Benutzereingabe
            int id = ReadInt();
            if (id == -1) return; // Abbruch durch den Benutzer

            // Suche nach dem spezifischen Gerät in der zuvor gefilterten Liste des Benutzers
            var selected = myBorrowedDevices.Find(d => d.Id == id);

            // Sicherheitsprüfung: Verhindert die Rückgabe von Geräten, die anderen Benutzern gehören
            if (selected == null)
            {
                PrintError("Ungültige ID oder dieses Gerät gehört nicht zu Ihren Ausleihen.");
                return;
            }

            // Aktualisiert den Status des Geräts auf 'verfügbar' und entfernt den Entleihernamen
            selected.MarkAsReturned();

            // Persistente Speicherung der Bestandsänderung in der CSV-Datei
            _inventory.SaveDevicesToFile();

            // Protokollierung des Vorgangs im Audit-Log
            Logger.LogEvent(_currentUser.Username, "zurückgegeben", selected.Name);

            // Optische Bestätigung des erfolgreichen Vorgangs in der Konsole
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

            // Endlosschleife, die jeden Tastendruck einzeln abfängt, bis [Enter] gedrückt wird
            while (true)
            {
                // Liest die gedrückte Taste ein, ohne sie in der Konsole anzuzeigen (intercept: true)
                var key = Console.ReadKey(intercept: true);

                // Bei [Enter] wird die Eingabe abgeschlossen und die Schleife beendet
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                // Behandlung der Rücktaste (Backspace): Entfernt das letzte Zeichen aus dem Passwort und der Konsole
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        // Entfernt das letzte Zeichen aus dem String
                        password = password[..^1];
                        // Bewegt den Cursor zurück, überschreibt das '*' mit einem Leerzeichen und springt wieder zurück
                        Console.Write("\b \b");
                    }
                }
                // Lässt nur sichtbare Zeichen zu (keine Strg/Alt-Kombinationen oder Pfeiltasten)
                else if (!char.IsControl(key.KeyChar))
                {
                    password += key.KeyChar;
                    // Gibt ein '*' als Maskierung für das eingegebene Zeichen in der Konsole aus
                    Console.Write("*");
                }
            }

            return password;
        }

        private static void ResetPasswordMenu()
        {
            PrintHeader("Benutzer-Passwort zurücksetzen");

            // Sicherheitsprüfung: Abbruch, falls kein Benutzer angemeldet ist
            if (_currentUser == null) return;

            Console.Write("Benutzername des Ziel-Accounts: ");
            string? targetUsername = Console.ReadLine();

            // Validierung der Eingabe: Leere Benutzernamen werden abgelehnt
            if (string.IsNullOrWhiteSpace(targetUsername))
            {
                PrintError("Benutzername darf nicht leer sein.");
                return;
            }

            // Verhindert, dass der aktuell angemeldete Administrator sein eigenes Passwort über dieses Menü ändert (Best Practice)
            if (string.Equals(targetUsername, _currentUser.Username, StringComparison.OrdinalIgnoreCase))
            {
                PrintError("Sie können Ihr eigenes Passwort hier nicht ändern.");
                return;
            }

            Console.Write("Neues Passwort vergeben: ");
            string newPassword = ReadPasswordMasked();

            // Durchsetzung der Passwortrichtlinie: Mindestens 6 Zeichen erforderlich
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                PrintError("Das neue Passwort muss mindestens 6 Zeichen lang sein.");
                return;
            }

            // Übergibt die Passwortänderung an den UserManager, welcher die Hierarchie-Prüfung durchführt 
            // (z. B. Ein Admin darf das Passwort eines SuperAdmins nicht ändern)
            bool success = _userManager.ResetUserPassword(_currentUser, targetUsername, newPassword);

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nPasswort wurde erfolgreich zurückgesetzt.");
                Console.ResetColor();

                // Optional: Protokollierung des sicherheitsrelevanten Ereignisses im Audit-Log
                // Logger.LogEvent(_currentUser.Username, $"Passwort von '{targetUsername}' zurückgesetzt", "Sicherheit");
            }
            else
            {
                PrintError("Fehler: Benutzer nicht gefunden oder unzureichende Berechtigungen (z. B. Admin darf keinen SuperAdmin ändern).");
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
            // Gibt die Geräte-ID in der Farbe Cyan aus, um sie optisch hervorzuheben
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(device.Id);
            Console.ResetColor();
            Console.Write($"] {device.Name} ({device.Category}) - Status: ");

            // Prüft den Gerätestatus und wendet die entsprechende Farbcodierung an
            if (device.IsBlocked)
            {
                // Gesperrte oder defekte Geräte werden rot markiert
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("GESPERRT");
            }
            else if (!device.IsAvailable)
            {
                // Geräte, die weder verfügbar noch gesperrt sind, gelten als ausgeliehen (gelb markiert)
                Console.ForegroundColor = ConsoleColor.Yellow;

                // Prüft, ob der Name des Entleihers hinterlegt ist, und gibt diesen mit aus
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
                // Verfügbare Geräte werden grün markiert
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("VERFÜGBAR");
            }

            // Setzt die Konsolenfarbe auf den Standardwert zurück, um nachfolgende Ausgaben nicht zu beeinflussen
            Console.ResetColor();
        }
    }
}
