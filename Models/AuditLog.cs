using System.ComponentModel.DataAnnotations;

namespace IT_Asset_Management_System.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        [StringLength(1000)]
        public string Details { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string IPAddress { get; set; } = string.Empty;
    }
}
