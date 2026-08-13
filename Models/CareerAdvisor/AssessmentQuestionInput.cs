using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class AssessmentQuestionInput
    {
        public int AssessmentQuestionId { get; set; }

        [Required]
        public int SkillAssessmentId { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Question")]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string OptionA { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string OptionB { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string OptionC { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string OptionD { get; set; } = string.Empty;

        [Required]
        [StringLength(1)]
        [Display(Name = "Correct Answer (A/B/C/D)")]
        public string CorrectAnswer { get; set; } = string.Empty;
    }
}
