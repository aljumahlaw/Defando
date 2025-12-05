using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Defando.Models
{
    [Table("incoming")]
    public class Incoming
    {
        [Key]
        [Column("incoming_id")]
        public int IncomingId { get; set; }

        [Column("document_id")]
        public int? DocumentId { get; set; }
        public virtual Document? Document { get; set; }
        
        [Required, MaxLength(60)]
        [Column("incoming_number")]
        public string IncomingNumber { get; set; } = string.Empty;
        
        [Required]
        [Column("received_date")]
        public DateTime ReceivedDate { get; set; }
        
        [Required, MaxLength(100)]
        [Column("sender_name")]
        public string SenderName { get; set; } = string.Empty;
        
        [MaxLength(255)]
        [Column("sender_email")]
        public string SenderEmail { get; set; } = string.Empty;
        
        [Required, MaxLength(500)]
        [Column("subject")]
        public string Subject { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column("sender_address")]
        public string? SenderAddress { get; set; }

        [MaxLength(120)]
        [Column("original_number")]
        public string? OriginalNumber { get; set; }

        [MaxLength(1000)]
        [Column("notes")]
        public string? Notes { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("priority")]
        public string Priority { get; set; } = "normal";

        [Column("requires_response")]
        public bool RequiresResponse { get; set; } = true;

        [Column("response_deadline")]
        public DateTime? ResponseDeadline { get; set; }

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        public virtual User? CreatedByUser { get; set; }
    }
}
