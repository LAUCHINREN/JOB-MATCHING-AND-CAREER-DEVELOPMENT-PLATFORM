using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JobCareerPlatform.Data;

namespace JobCareerPlatform.Models
{
    public class CareerResource
    {
        [Key]
        public int CareerResourceId { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        // Guide / Article / Video / LearningMaterial / InterviewPrep
        [Required]
        [StringLength(30)]
        public string ResourceType { get; set; } = "Guide";

        [StringLength(500)]
        public string? ContentUrl { get; set; }

        // Optional file attachment (PDF, worksheet, etc.) stored in S3 — separate from ContentUrl,
        // which is for linking out to external content.
        [StringLength(500)]
        public string? AttachmentUrl { get; set; }

        [StringLength(500)]
        public string? AttachmentS3Key { get; set; }

        // Drives automatic matching to job seekers — compared against JobSeekerProfile.PreferredJobCategory.
        [StringLength(50)]
        [Display(Name = "Related Job Category")]
        public string? RelatedCategory { get; set; }

        // Comma-separated skill/topic tags — matched against JobSeekerProfile.FieldOfStudy and
        // the RequiredSkills of jobs the job seeker has applied to (see ResourceMatcher).
        [StringLength(200)]
        [Display(Name = "Related Skills")]
        public string? RelatedSkill { get; set; }

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(CreatedByUserId))]
        public ApplicationUser? CreatedBy { get; set; }

        public bool IsPublished { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PublishedAt { get; set; }
    }
}
