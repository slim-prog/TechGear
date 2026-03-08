namespace TechGear
{
    using System.Security.Cryptography;
    using System.Text;

    // Eine Hilfsklasse für die Sicherheit (Kryptografie).
    // "static" bedeutet, dass wir sie einfach überall im Code nutzen können, 
    // ohne sie vorher mit "new" erstellen zu müssen.
    public static class SecurityHelper
    {
        // Macht aus einem normalen Passwort (z.B. "Geheim123") einen langen, unlesbaren Text (Hash).
        // Das ist wichtig, damit niemand die Passwörter in der CSV-Datei lesen kann.
        public static string HashPassword(string password)
        {
            // Wir erstellen ein "SHA256"-Werkzeug. Das "using" sorgt dafür, 
            // dass dieses Werkzeug danach sofort wieder sauber aus dem Speicher gelöscht wird.
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // 1. Zuerst machen wir aus dem normalen Text-Passwort eine Reihe von Zahlen (Bytes),
                // weil der Computer (und der Hash-Algorithmus) besser mit Zahlen rechnen kann.
                // 2. Dann lassen wir den Algorithmus rechnen und bekommen verschlüsselte Bytes zurück.
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Wir nutzen einen "StringBuilder", um diese verschlüsselten Zahlen Stück für Stück wieder zu einem Text (String) zusammenzubauen.
                StringBuilder builder = new StringBuilder();

                // Wir gehen jede einzelne Zahl (Byte) durch
                for (int i = 0; i < bytes.Length; i++)
                {
                    // Das "x2" ist ein Format-Befehl. Er wandelt die Zahl in einen sogenannten "Hexadezimal"-Text um (z.B. aus 255 wird "ff").
                    // So sieht ein typischer Hash-Code aus.
                    builder.Append(bytes[i].ToString("x2"));
                }

                // Am Ende schicken wir den fertigen, langen Hash-Text zurück an unser Programm.
                return builder.ToString();
            }
        }
    }
}
