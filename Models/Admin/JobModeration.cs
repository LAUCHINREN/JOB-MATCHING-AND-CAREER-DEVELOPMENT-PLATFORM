using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobCareerPlatform.Models.Admin
{
    public class JobModeration
    {
        [Key]
        public int ModerationId { get; set; }

        [Required]
        public int JobId { get; set; }

        [ForeignKey(nameof(JobId))]
        public JobCareerPlatform.Models.JobPosting? JobPosting { get; set; }

        [Required]
        public string AdminId { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string PreviousStatus { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string NewStatus { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Reason { get; set; }

        public DateTime ModeratedAt { get; set; } = DateTime.UtcNow;
    }
}