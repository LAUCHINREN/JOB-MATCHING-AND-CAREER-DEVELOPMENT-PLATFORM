namespace JobCareerPlatform.Models
{
    public class AssessmentAnswerViewModel
    {
        public int AssessmentQuestionId { get; set; }

        public string QuestionText { get; set; } = string.Empty;

        public string OptionA { get; set; } = string.Empty;

        public string OptionB { get; set; } = string.Empty;

        public string OptionC { get; set; } = string.Empty;

        public string OptionD { get; set; } = string.Empty;

        public string? SelectedAnswer { get; set; }
    }
}