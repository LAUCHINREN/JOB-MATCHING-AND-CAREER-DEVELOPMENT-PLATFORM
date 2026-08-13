using JobCareerPlatform.Data;
using JobCareerPlatform.Models.Admin;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobCareerPlatform.Models
{
    public class JobPosting
    {
        [Key]
        public int JobId { get; set; }

        [Required]
        [StringLength(150)]
        public string JobTitle { get; set; } = string.Empty;

        [Required]
        public string EmployerId { get; set; } = string.Empty;

        [ForeignKey(nameof(EmployerId))]
        public ApplicationUser? Employer { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public JobCategory? JobCategory { get; set; }

        [Required]
        [StringLength(2000)]
        public string JobDescription { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Location { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal? SalaryMin { get; set; }

        [Precision(18, 2)]
        public decimal? SalaryMax { get; set; }

        [StringLength(500)]
        public string? RequiredSkills { get; set; }

        [StringLength(500)]
        public string? Qualification { get; set; }

        [StringLength(100)]
        public string? EmploymentType { get; set; }

        // Pending / Approved / Rejected
        [Required]
        [StringLength(30)]
        public string ModerationStatus { get; set; } = "Pending";

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ModeratedAt { get; set; }

        // Open / Closed — only meaningful once ModerationStatus is Approved
        [Required]
        [StringLength(30)]
        public string VacancyStatus { get; set; } = "Closed";

        [StringLength(500)]
        public string? CloseReason { get; set; }

        public DateTime? PostedAt { get; set; }
    }
}