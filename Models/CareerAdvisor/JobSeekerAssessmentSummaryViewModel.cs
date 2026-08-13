namespace JobCareerPlatform.Models
{
    // One row per job seeker on the Career Advisor's "Review Job Seekers" list page.
    public class JobSeekerAssessmentSummaryViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PreferredJobCategory { get; set; }

        public int AssessmentsTaken { get; set; }
        public int AssessmentsPassed { get; set; }
        public double AverageScore { get; set; }
    }

    // Full detail page: profile + every assessment attempt for one job seeker.
    public class JobSeekerAssessmentDetailViewModel
    {
        public JobSeekerProfile? Profile { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }

        public List<JobSeekerAssessmentAttempt> Attempts { get; set; } = new();
    }

    public class JobSeekerAssessmentAttempt
    {
        public string AssessmentTitle { get; set; } = string.Empty;
        public string SkillName { get; set; } = string.Empty;
        public int Score { get; set; }
        public bool IsPassed { get; set; }
        public DateTime CompletedDate { get; set; }
    }
}
