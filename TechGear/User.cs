namespace TechGear
{
    /// <summary>
    /// Repräsentiert einen Benutzer im System.
    /// Diese Klasse ist als unveränderliches (immutable) Datenmodell konzipiert.
    /// </summary>
    internal class User
    {
        /// <summary>
        /// Der eindeutige Anmeldename des Benutzers.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Der SHA-256 Hash-Wert des Benutzerpassworts. 
        /// Das Klartextpasswort wird aus Sicherheitsgründen niemals im System gespeichert.
        /// </summary>
        public string PasswordHash { get; }

        /// <summary>
        /// Die Berechtigungsstufe des Benutzers (User, Admin oder SuperAdmin).
        /// Steuert den Zugriff auf verschiedene Menüpunkte und Systemfunktionen.
        /// </summary>
        public UserRole Role { get; }

        /// <summary>
        /// Initialisiert eine neue Instanz der User-Klasse.
        /// </summary>
        /// <param name="username">Der Benutzername.</param>
        /// <param name="passwordHash">Der verschlüsselte Passwort-Hash.</param>
        /// <param name="role">Die zugewiesene Rolle im System.</param>
        public User(string username, string passwordHash, UserRole role)
        {
            Username = username;
            PasswordHash = passwordHash;
            Role = role;
        }
    }
}
