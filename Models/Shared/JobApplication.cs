using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobCareerPlatform.Models
{
    public class JobApplication
    {
        public int JobApplicationId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int JobId { get; set; }

        [ForeignKey(nameof(JobId))]
        public JobPosting? JobPosting { get; set; }

        [StringLength(1000)]
        [Display(Name = "Cover Message")]
        public string? CoverMessage { get; set; }

        [Required]
        public string Status { get; set; } = "Submitted";

        [StringLength(500)]
        public string? RejectionNote { get; set; }

        public DateTime AppliedDate { get; set; } = DateTime.Now;
    }
}
