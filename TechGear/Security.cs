namespace TechGear
{
    using System.Security.Cryptography;
    using System.Text;

    // Stellt Sicherheitsfunktionen wie das Hashen von Passwörtern.
    public static class SecurityHelper
    {
        // Konvertiert ein Klartext-Passwort in einen sicheren SHA256-Hash.
        public static string HashPassword(string password)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
