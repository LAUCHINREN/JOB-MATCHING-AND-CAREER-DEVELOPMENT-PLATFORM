using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class UserActivityLog
    {
        [Key]
        public int ActivityId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string UserRole { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ActivityType { get; set; } = string.Empty;

        [StringLength(100)]
        public string? EntityType { get; set; }

        [StringLength(100)]
        public string? EntityId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}