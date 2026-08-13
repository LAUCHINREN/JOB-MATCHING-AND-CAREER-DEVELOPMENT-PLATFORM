using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class JobSeekerSkill
    {
        public int JobSeekerSkillId { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Skill")]
        public string SkillName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Proficiency Level")]
        public string ProficiencyLevel { get; set; } = string.Empty;
    }
}