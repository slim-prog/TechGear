namespace TechGear
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
                // Erstellung der Standardkonten mit gehashten Passwörtern
                _users.Add(new User("superadmin", SecurityHelper.HashPassword("superadmin"), UserRole.SuperAdmin));
                _users.Add(new User("admin", SecurityHelper.HashPassword("admin"), UserRole.Admin));
                _users.Add(new User("user", SecurityHelper.HashPassword("user"), UserRole.User));
                SaveUsersToFile();
                return;
            }

            string[] lines = File.ReadAllLines(UserFilePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split(';');
                if (parts.Length < 3) continue;

                string username = parts[0];
                string passwordHash = parts[1];

                // Typsichere Umwandlung des Strings in den Enum
                if (!Enum.TryParse(parts[2], true, out UserRole role))
                {
                    role = UserRole.User; // Fallback für fehlerhafte Einträge
                }

                _users.Add(new User(username, passwordHash, role));
            }
        }

        public void SaveUsersToFile()
        {
            var lines = new List<string>();

            foreach (var user in _users)
            {
                lines.Add($"{user.Username};{user.PasswordHash};{user.Role}");
            }

            File.WriteAllLines(UserFilePath, lines);
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
    }
}
