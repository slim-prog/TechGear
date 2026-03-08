namespace TechGear
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    // Verwaltet alle Benutzeroperationen und die Persistenz der Benutzerdaten.
    internal class UserManager
    {
        private readonly List<User> _users = new();
        private const string UserFilePath = "users.csv";

        // Bietet einen Nur-Lese-Zugriff auf die Benutzerliste nach außen
        public IReadOnlyList<User> Users => _users.AsReadOnly();

        public void LoadUsersFromFile()
        {
            _users.Clear();

            if (!File.Exists(UserFilePath))
            {
                // Erstelle Standard-Benutzer...
                _users.Add(new User("superadmin", "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918", UserRole.SuperAdmin));
                _users.Add(new User("admin", "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918", UserRole.Admin));
                SaveUsersToFile();
                return;
            }

            string[] lines = File.ReadAllLines(UserFilePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Parsing mit Unterstützung für escaped fields
                var parts = ParseCsvLine(line);

                if (parts.Length != 3) continue;

                string username = parts[0];
                string passwordHash = parts[1];
                string roleString = parts[2];

                if (!Enum.TryParse(roleString, true, out UserRole role))
                {
                    role = UserRole.User;
                }

                _users.Add(new User(username, passwordHash, role));
            }
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

        // Hilfsmethode für CSV-Escaping
        private static string EscapeCsvField(string field)
        {
            // Wenn das Feld `;`, `"`, oder Zeilenumbruch enthält, umgeben wir es mit Anführungszeichen
            if (field.Contains(";") || field.Contains("\"") || field.Contains("\n"))
            {
                // Escapen Sie doppelte Anführungszeichen durch Verdopplung: " → ""
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }

        public User? ValidateLogin(string username, string password)
        {
            string hashedInput = SecurityHelper.HashPassword(password);
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
            // Căutăm userul căruia vrem să-i schimbăm parola
            User? targetUser = FindByUsername(targetUsername);

            if (targetUser == null)
            {
                return false; // Userul țintă nu a fost găsit
            }

            // --- LOGICA DE PERMISIUNI (RBAC) ---
            if (executingUser.Role == UserRole.Admin)
            {
                // Un Admin are voie să schimbe parola DOAR unui User normal
                if (targetUser.Role == UserRole.SuperAdmin || targetUser.Role == UserRole.Admin)
                {
                    return false; // Interzis!
                }
            }
            else if (executingUser.Role != UserRole.SuperAdmin)
            {
                // Orice alt rol (ex. User normal) nu are ce căuta aici
                return false;
            }

            // Dacă am ajuns aici, permisiunile sunt valide.
            // Hash-uim noua parolă
            string newHash = SecurityHelper.HashPassword(newPassword);

            // Înlocuim utilizatorul (pentru a actualiza PasswordHash)
            _users.Remove(targetUser);
            _users.Add(new User(targetUser.Username, newHash, targetUser.Role));

            SaveUsersToFile();
            return true;
        }

    }
}
