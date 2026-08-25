namespace RSMS.Models
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = null!;
        public int SmtpPort { get; set; }
        public bool UseSsl { get; set; }
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FromAddress { get; set; } = null!;
        public string FromName { get; set; } = null!;
        public int AlertCooldownMinutes { get; set; } = 15;
        public List<string> Recipients { get; set; } = new();


    }
}
