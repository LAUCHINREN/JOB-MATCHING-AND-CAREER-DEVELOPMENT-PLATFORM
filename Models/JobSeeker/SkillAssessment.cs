using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JobCareerPlatform.Data;

namespace JobCareerPlatform.Models
{
    public class SkillAssessment
    {
        public int SkillAssessmentId { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Skill")]
        public string SkillName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, 100)]
        [Display(Name = "Passing Score")]
        public int PassingScore { get; set; } = 60;

        [Range(1, 120)]
        [Display(Name = "Duration (Minutes)")]
        public int DurationMinutes { get; set; } = 15;

        public bool IsActive { get; set; } = true;

        // Nullable — pre-existing/legacy assessments have no author on record.
        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public ApplicationUser? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}