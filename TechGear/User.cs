namespace TechGear
{
    internal class User
    {
        public string Username { get; }
        public string PasswordHash { get; }
        public UserRole Role { get; }

        public User(string username, string passwordHash, UserRole role)
        {
            Username = username;
            PasswordHash = passwordHash;
            Role = role;
        }
    }
}
