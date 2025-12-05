using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Defando.Models
{
    [Table("outgoing")]
    public class Outgoing
    {
        [Key]
        [Column("outgoing_id")]
        public int OutgoingId { get; set; }

        [Column("document_id")]
        public int? DocumentId { get; set; }
        public virtual Document? Document { get; set; }
        
        [Required, MaxLength(60)]
        [Column("outgoing_number")]
        public string OutgoingNumber { get; set; } = string.Empty;
        
        [Required]
        [Column("send_date")]
        public DateTime SendDate { get; set; }
        
        [Required, MaxLength(100)]
        [Column("recipient_name")]
        public string RecipientName { get; set; } = string.Empty;
        
        [Required, MaxLength(255)]
        [Column("recipient_email")]
        public string RecipientEmail { get; set; } = string.Empty;
        
        [Required, MaxLength(500)]
        [Column("subject")]
        public string Subject { get; set; } = string.Empty;
        
        [MaxLength(50)]
        [Column("delivery_method")]
        public string DeliveryMethod { get; set; } = "Email";
        
        [MaxLength(100)]
        [Column("tracking_number")]
        public string? TrackingNumber { get; set; }

        [MaxLength(255)]
        [Column("recipient_address")]
        public string RecipientAddress { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Column("notes")]
        public string Notes { get; set; } = string.Empty;

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        public virtual User? CreatedByUser { get; set; }
    }
}
