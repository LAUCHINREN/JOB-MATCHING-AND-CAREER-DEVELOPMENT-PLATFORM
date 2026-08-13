namespace JobCareerPlatform.Models.Admin
{
    public class AdminAnalyticsReportViewModel
    {
        // REPORT INFORMATION
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public DateTime GeneratedAt { get; set; }


        // JOB SEEKER / APPLICATION STATISTICS
        public int ApplicationsSubmitted { get; set; }

        // USER ENGAGEMENT
        public int ActiveUsers { get; set; }


        // JOB MARKET TRENDS
        public List<string> JobCategoryLabels { get; set; } = new();
        public List<int> JobCategoryCounts { get; set; } = new();

        public List<string> JobLocationLabels { get; set; } = new();
        public List<int> JobLocationCounts { get; set; } = new();

        public List<string> EmploymentTypeLabels { get; set; } = new();
        public List<int> EmploymentTypeCounts { get; set; } = new();

        public List<string> RequiredSkillLabels { get; set; } = new();
        public List<int> RequiredSkillCounts { get; set; } = new();


        // JOB SEEKER / APPLICATION STATISTICS
        public int SubmittedApplications { get; set; }
        public int UnderReviewApplications { get; set; }
        public int ShortlistedApplications { get; set; }
        public int InterviewApplications { get; set; }
        public int OfferedApplications { get; set; }
        public int RejectedApplications { get; set; }


        // USER ENGAGEMENT
        public int ActivitiesRecorded { get; set; }

        public List<string> ActivityRoleLabels { get; set; } = new();
        public List<int> ActivityRoleCounts { get; set; } = new();


        // CURRENT PLATFORM SNAPSHOT
        public int TotalUsers { get; set; }
        public int TotalJobSeekers { get; set; }
        public int TotalPublicJobs { get; set; }
        public int TotalApplications { get; set; }
    }
}