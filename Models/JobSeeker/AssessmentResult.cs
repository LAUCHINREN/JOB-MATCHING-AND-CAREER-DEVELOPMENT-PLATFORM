using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class AssessmentResult
    {
        public int AssessmentResultId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int SkillAssessmentId { get; set; }

        [Range(0, 100)]
        public int Score { get; set; }

        public int CorrectAnswers { get; set; }

        public int TotalQuestions { get; set; }

        [Display(Name = "Result")]
        public bool IsPassed { get; set; }

        [Display(Name = "Completed Date")]
        public DateTime CompletedDate { get; set; } = DateTime.Now;
    }
}