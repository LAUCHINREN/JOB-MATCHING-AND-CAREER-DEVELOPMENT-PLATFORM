namespace JobCareerPlatform.Models
{
    public class ApplicantDashboardViewModel
    {
        public List<JobApplication> Applications { get; set; } = new();
        public Dictionary<string, int> StatusCounts { get; set; } = new();
        public List<JobPosting> MyVacancies { get; set; } = new();   // for the vacancy filter dropdown

        // Keyed by JobApplication.UserId — avoids adding a hard FK from JobApplication to JobSeekerProfile
        public Dictionary<string, JobSeekerProfile> Profiles { get; set; } = new();

        // Fallback display name for applicants who haven't created a JobSeekerProfile yet
        // (Profiles won't have an entry for them, so FullName would otherwise render blank).
        public Dictionary<string, string> Names { get; set; } = new();

        public int? SelectedVacancyId { get; set; }
        public string? SelectedStatus { get; set; }
        public string? Search { get; set; }

        public Dictionary<int, int> FitScores { get; set; } = new();
        public Dictionary<int, List<FitScoreAspect>> FitBreakdowns { get; set; } = new();
        public string? SortBy { get; set; }

        // Keyed by JobApplication.UserId — every skill assessment the applicant has taken,
        // scoped to applicants of this employer's own vacancies (see JobApplicationsController).
        public Dictionary<string, List<AssessmentResult>> AssessmentsByApplicant { get; set; } = new();

        // Resolves SkillAssessmentId -> title/skill name for display (AssessmentResult has no nav property).
        public Dictionary<int, SkillAssessment> AssessmentCatalog { get; set; } = new();
    }
}
