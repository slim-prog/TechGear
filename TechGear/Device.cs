namespace TechGear
{
    internal class Device
    {
        public int Id { get; }
        public string Name { get; }
        public string Category { get; }
        public bool IsAvailable { get; private set; } = true;
        public string? BorrowedBy { get; private set; }

        // NOU: Proprietate pentru a bloca dispozitivul
        public bool IsBlocked { get; private set; } = false;

        public Device(int id, string name, string category)
        {
            Id = id;
            Name = name;
            Category = category;
        }

        public void MarkAsBorrowed(User user)
        {
            IsAvailable = false;
            BorrowedBy = user.Username;
        }

        public void MarkAsBorrowedBy(string username)
        {
            IsAvailable = false;
            BorrowedBy = username;
        }

        public void MarkAsReturned()
        {
            IsAvailable = true;
            BorrowedBy = null;
        }

        // NOU: Metode pentru blocare/deblocare
        public void BlockDevice()
        {
            IsBlocked = true;
        }

        public void UnblockDevice()
        {
            IsBlocked = false;
        }

        public override string ToString()
        {
            string status;
            if (IsBlocked)
            {
                status = "GESPERRT (Defekt/Wartung)";
            }
            else
            {
                status = IsAvailable ? "verfügbar" : $"ausgeliehen (von {BorrowedBy})";
            }

            return $"[{Id}] {Name} ({Category}) - {status}";
        }
    }

}
