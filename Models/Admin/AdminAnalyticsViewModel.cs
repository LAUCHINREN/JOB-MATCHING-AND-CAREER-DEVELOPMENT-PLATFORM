namespace JobCareerPlatform.Models.Admin
{
    public class AdminAnalyticsViewModel
    {
        // USER STATISTICS
        public int TotalUsers { get; set; }

        public int ActiveAccounts { get; set; }

        public int SuspendedAccounts { get; set; }

        public int DeactivatedAccounts { get; set; }

        public int JobSeekers { get; set; }

        public int Employers { get; set; }

        public int CareerAdvisors { get; set; }

        public int SystemAdmins { get; set; }


        // USER ENGAGEMENT
        public int ActiveUsersLast7Days { get; set; }

        public int ActivitiesLast7Days { get; set; }

        public int TotalActivities { get; set; }


        // JOB STATISTICS
        public int TotalJobs { get; set; }

        public int PendingJobs { get; set; }

        public int ApprovedJobs { get; set; }

        public int RejectedJobs { get; set; }


        // CHART DATA
        public List<string> JobCategoryLabels { get; set; }
            = new List<string>();

        public List<int> JobCategoryCounts { get; set; }
            = new List<int>();


        public List<string> ActivityRoleLabels { get; set; }
            = new List<string>();

        public List<int> ActivityRoleCounts { get; set; }
            = new List<int>();


        public List<string> ActivityDateLabels { get; set; }
            = new List<string>();

        public List<int> ActivityDateCounts { get; set; }
            = new List<int>();


        // RECENT ACTIVITY
        public List<UserActivityLogViewModel> RecentActivities { get; set; }
            = new List<UserActivityLogViewModel>();
    }
}