using JobCareerPlatform.Models;

namespace JobCareerPlatform.Models.Admin
{
    public class AdminAnalyticsReportViewModel
    {
        // Report information
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public DateTime GeneratedAt { get; set; }

        // Current system snapshot
        public int CurrentTotalUsers { get; set; }
        public int CurrentTotalJobs { get; set; }
        public int CurrentTotalApplications { get; set; }

        // Selected period summary
        public int NewUsers { get; set; }
        public int JobsCreated { get; set; }
        public int ApplicationsSubmitted { get; set; }
        public int ActivitiesRecorded { get; set; }
        public int ActiveUsers { get; set; }

        // New users by role
        public int NewJobSeekers { get; set; }
        public int NewEmployers { get; set; }
        public int NewCareerAdvisors { get; set; }
        public int NewSystemAdmins { get; set; }

        // Job moderation breakdown
        public int PendingJobs { get; set; }
        public int ApprovedJobs { get; set; }
        public int RejectedJobs { get; set; }

        // Job category breakdown
        public List<string> JobCategoryLabels { get; set; } = new();
        public List<int> JobCategoryCounts { get; set; } = new();

        // Application status breakdown
        public List<string> ApplicationStatusLabels { get; set; } = new();
        public List<int> ApplicationStatusCounts { get; set; } = new();

        // Activity by role
        public List<string> ActivityRoleLabels { get; set; } = new();
        public List<int> ActivityRoleCounts { get; set; } = new();

        // Recent activities within selected period
        public List<UserActivityLogViewModel> RecentActivities { get; set; }
            = new();
    }
}