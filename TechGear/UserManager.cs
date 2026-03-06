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
                _users.Add(new User("superadmin", "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918", UserRole.SuperAdmin));
                _users.Add(new User("admin", "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918", UserRole.Admin));
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
