namespace JobCareerPlatform.Models
{
    public class TakeAssessmentViewModel
    {
        public int SkillAssessmentId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string SkillName { get; set; } = string.Empty;

        public int PassingScore { get; set; }

        public int DurationMinutes { get; set; }

        public List<AssessmentAnswerViewModel> Questions { get; set; } = new();
    }
}