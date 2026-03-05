namespace TechGear
{
    internal class User
    {
        public string Username { get; }
        public string Password { get; }

        // În loc de bool IsAdmin, folosim un string (ex: "SuperAdmin", "Admin", "User")
        public string Role { get; }

        public User(string username, string password, string role)
        {
            Username = username;
            Password = password;
            Role = role;
        }
    }

}
