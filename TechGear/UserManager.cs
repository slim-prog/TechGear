namespace TechGear
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    // Verwaltet alle Benutzeroperationen und die Persistenz der Benutzerdaten
    internal class UserManager
    {
        private readonly List<User> _users = new();
        private const string UserFilePath = "users.csv";

        // Bietet einen Lese Zugriff auf die Benutzerliste nach außen
        public IReadOnlyList<User> Users => _users.AsReadOnly();

        public void LoadUsersFromFile()
        {
            // Bevor wir aus der Datei lesen, leeren wir zur Sicherheit unsere aktuelle Liste damit wir keine Benutzer doppelt haben
            _users.Clear();

            // Wenn das Programm zum allerersten Mal gestartet wird, gibt es noch keine "users.csv" Datei 
            // In diesem Fall müssen wir erste "Start Benutzer" (Seed Data) anlegen damit man sich überhaupt ins System einloggen kann
            if (!File.Exists(UserFilePath))
            {
                // Wir legen einen SuperAdmin und einen normalen Admin an
                _users.Add(new User("superadmin", "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918", UserRole.SuperAdmin));
                _users.Add(new User("admin", "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918", UserRole.Admin));

                // Jetzt speichern wir diese beiden neuen Benutzer direkt in eine brandneue CSV-Datei
                SaveUsersToFile();
                return; // Wir sind fertig, da die Datei ja frisch erstellt wurde
            }

            // Wenn die Datei schon existiert, lesen wir alle Zeilen auf einmal in einen Text-Block (Array) ein
            string[] lines = File.ReadAllLines(UserFilePath);

            // Wir gehen jede Zeile einzeln durch
            foreach (var line in lines)
            {
                // Wenn eine Zeile leer ist (z.B. am Ende der Datei), überspringen wir sie einfach
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Wir zerteilen die Zeile in ihre einzelnen Stücke (Name, Passwort, Rolle)
                // Unsere spezielle Methode 'ParseCsvLine' sorgt dafür, dass auch Fehler durch versehentlich eingetippte Sonderzeichen repariert werden.
                var parts = ParseCsvLine(line);

                // Wir erwarten genau 3 Teile (Name; Hash; Rolle)
                // Wenn das nicht stimmt, ist die Zeile kaputt und wir ignorieren sie
                if (parts.Length != 3) continue;

                string username = parts[0];
                string passwordHash = parts[1];
                string roleString = parts[2];

                // Hier versuchen wir, den Text (z.B. "Admin") in unser echtes UserRole Enum umzuwandeln
                // Das "true" bedeutet, dass uns Groß/Kleinschreibung egal ist (Admin = admin)
                if (!Enum.TryParse(roleString, true, out UserRole role))
                {
                    role = UserRole.User;
                }

                // Wir bauen den Benutzer aus den gelesenen Daten zusammen und fügen ihn der Liste hinzu
                _users.Add(new User(username, passwordHash, role));
            }
        }
        public void SaveUsersToFile()
        {
            var lines = new List<string>();

            foreach (var user in _users)
            {
                // Escapen: Wenn Benutzername oder PasswordHash `;` oder `"` enthalten, umgeben wir sie mit Anführungszeichen
                string escapedUsername = EscapeCsvField(user.Username);
                string escapedPasswordHash = EscapeCsvField(user.PasswordHash);
                string roleString = user.Role.ToString();

                string line = $"{escapedUsername};{escapedPasswordHash};{roleString}";
                lines.Add(line);
            }

            File.WriteAllLines(UserFilePath, lines);
        }
        // Hilfsmethode zum Parsen von CSV-Zeilen mit Escape-Unterstützung
        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Doppeltes Anführungszeichen "" bedeutet ein literales Anführungszeichen
                        currentField.Append('"');
                        i++; // Nächstes Anführungszeichen überspringen
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (c == ';' && !insideQuotes)
                {
                    fields.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            fields.Add(currentField.ToString());
            return fields.ToArray();
        }

        // Hilfsmethode für CSV-Escaping
        private static string EscapeCsvField(string field)
        {
            // Wenn das Feld `;`, `"`, oder Zeilenumbruch enthält, umgeben wir es mit Anführungszeichen
            if (field.Contains(";") || field.Contains("\"") || field.Contains("\n"))
            {
                // Escapen Sie doppelte Anführungszeichen durch Verdopplung
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }

        public User? ValidateLogin(string username, string password)
        {
            // Wir nehmen das Passwort, das der Benutzer gerade abgetippt hat und machen daraus mit unserem Helfer wieder einen Hash
            string hashedInput = SecurityHelper.HashPassword(password);

            // Wir suchen in unserer Liste nach jemandem, bei dem zwei Dinge zutreffen:
            // 1. Der Benutzername muss genau passen (Groß/Kleinschreibung ist dabei egal: "Admin" = "admin")
            // 2. Der gerade erstellte Hash muss exakt mit dem Hash in der Liste übereinstimmen
            // Wenn wir so jemanden finden, geben wir ihn zurück. Wenn nicht, geben wir 'null' nichts zurück
            return _users.Find(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) && u.PasswordHash == hashedInput);
        }

        public bool UserExists(string username)
        {
            return _users.Exists(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        }

        public User? FindByUsername(string username)
        {
            return _users.Find(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
        }

        public void AddUser(User user)
        {
            _users.Add(user);
        }

        public void RemoveUser(User user)
        {
            _users.Remove(user);
        }

        public bool ResetUserPassword(User executingUser, string targetUsername, string newPassword)
        {
            // Wir suchen zuerst den Benutzer in unserer Liste, dessen Passwort geändert werden soll
            User? targetUser = FindByUsername(targetUsername);

            // Wenn wir den Namen in der CSV-Datei/Liste nicht finden, brechen wir ab (false)
            if (targetUser == null)
            {
                return false;
            }

            //Wer darf was?
            if (executingUser.Role == UserRole.Admin)
            {
                // Ein normaler Admin darf das Passwort NUR von einfachen "Usern" ändern.
                // Er darf nicht die Passwörter von anderen Admins oder dem SuperAdmin anfassen!
                if (targetUser.Role == UserRole.SuperAdmin || targetUser.Role == UserRole.Admin)
                {
                    return false; // Aktion verboten!
                }
            }
            // Wenn die Person, die diese Methode aufruft, weder Admin noch SuperAdmin ist, 
            // darf sie hier überhaupt nichts machen (z.B. ein normaler User)
            else if (executingUser.Role != UserRole.SuperAdmin)
            {
                return false;
            }

            // --- PASSWORT ÄNDERN ---
            // Wenn wir hier ankommen, bedeutet das: Die Rechte stimmen!

            // Wir machen aus dem neuen, getippten Passwort wieder einen sicheren, unlesbaren Hash
            string newHash = SecurityHelper.HashPassword(newPassword);

            // Da unsere User-Klasse keine direkten Änderungen (set) am Passwort erlaubt löschen wir den alten Benutzer aus der Liste...
            _users.Remove(targetUser);

            // ... und legen ihn mit genau den gleichen Daten (Name, Rolle) aber dem NEUEN Passwort-Hash sofort wieder an.
            _users.Add(new User(targetUser.Username, newHash, targetUser.Role));

            // Wir speichern die neue Liste sofort in der CSV-Datei, damit nichts verloren geht
            SaveUsersToFile();

            return true; // Meldet zurück: "Alles hat geklappt!"
        }


    }
}
