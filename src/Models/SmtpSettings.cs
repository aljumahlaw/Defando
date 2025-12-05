using System.ComponentModel.DataAnnotations;

namespace Defando.Models
{
    public class SmtpSettings
    {
        public int Id { get; set; }

        [Required]
        public bool Enabled { get; set; }
        
        [Required]
        public string Host { get; set; } = string.Empty;
        
        [Required]
        [Range(1, 65535)]
        public int Port { get; set; } = 587;
        
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        public bool UseSsl { get; set; } = true;
        
        public string FromName { get; set; } = "نظام إدارة المستندات";
        
        [Required]
        [EmailAddress]
        public string FromAddress { get; set; } = string.Empty;
        
        public bool NotifyOnSharedLinkCreated { get; set; }
        public bool NotifyOnSharedLinkAccessed { get; set; }
        public bool NotifyOnTaskReminder { get; set; }
    }
}
