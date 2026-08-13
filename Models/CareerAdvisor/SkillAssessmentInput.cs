using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class SkillAssessmentInput
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
        [Display(Name = "Passing Score (%)")]
        public int PassingScore { get; set; } = 60;

        [Range(1, 120)]
        [Display(Name = "Duration (Minutes)")]
        public int DurationMinutes { get; set; } = 15;

        [Display(Name = "Active (visible to job seekers)")]
        public bool IsActive { get; set; } = true;
    }
}
