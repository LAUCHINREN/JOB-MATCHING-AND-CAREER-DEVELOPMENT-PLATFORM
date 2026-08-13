using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using JobCareerPlatform.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "SystemAdmin")]
    public class AdminAnalyticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminAnalyticsController(
            ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            var model = new AdminAnalyticsViewModel();


            // USER STATISTICS
            model.TotalUsers =
                await _context.Users.CountAsync();

            model.ActiveAccounts =
                await _context.Users.CountAsync(u =>
                    u.AccountStatus == "Active");

            model.SuspendedAccounts =
                await _context.Users.CountAsync(u =>
                    u.AccountStatus == "Suspended");

            model.DeactivatedAccounts =
                await _context.Users.CountAsync(u =>
                    u.AccountStatus == "Deactivated");


            model.JobSeekers =
                await _context.Users.CountAsync(u =>
                    u.UserRole == "JobSeeker");

            model.Employers =
                await _context.Users.CountAsync(u =>
                    u.UserRole == "Employer");

            model.CareerAdvisors =
                await _context.Users.CountAsync(u =>
                    u.UserRole == "CareerAdvisor");

            model.SystemAdmins =
                await _context.Users.CountAsync(u =>
                    u.UserRole == "SystemAdmin");


            // USER ENGAGEMENT
            var sevenDaysAgo =
                DateTime.UtcNow.AddDays(-7);

            model.ActiveUsersLast7Days =
                await _context.UserActivityLogs
                    .Where(a =>
                        a.CreatedAt >= sevenDaysAgo)
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();

            model.ActivitiesLast7Days =
                await _context.UserActivityLogs
                    .CountAsync(a =>
                        a.CreatedAt >= sevenDaysAgo);

            model.TotalActivities =
                await _context.UserActivityLogs
                    .CountAsync();


            // JOB STATISTICS
            model.TotalJobs =
                await _context.JobPostings.CountAsync();

            model.PendingJobs =
                await _context.JobPostings.CountAsync(j =>
                    j.ModerationStatus == "Pending");

            model.ApprovedJobs =
                await _context.JobPostings.CountAsync(j =>
                    j.ModerationStatus == "Approved");

            model.RejectedJobs =
                await _context.JobPostings.CountAsync(j =>
                    j.ModerationStatus == "Rejected");


            // JOBS BY CATEGORY
            var jobsByCategory =
                await _context.JobPostings
                    .Include(j => j.JobCategory)
                    .GroupBy(j =>
                        j.JobCategory != null
                            ? j.JobCategory.CategoryName
                            : "Uncategorized")
                    .Select(g => new
                    {
                        Category = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();


            model.JobCategoryLabels =
                jobsByCategory
                    .Select(x => x.Category)
                    .ToList();

            model.JobCategoryCounts =
                jobsByCategory
                    .Select(x => x.Count)
                    .ToList();


            // ACTIVITY BY USER ROLE
            var activitiesByRole =
                await _context.UserActivityLogs
                    .GroupBy(a => a.UserRole)
                    .Select(g => new
                    {
                        Role = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();


            model.ActivityRoleLabels =
                activitiesByRole
                    .Select(x => x.Role)
                    .ToList();

            model.ActivityRoleCounts =
                activitiesByRole
                    .Select(x => x.Count)
                    .ToList();


            // ACTIVITY TREND - LAST 7 DAYS
            var activityData =
                await _context.UserActivityLogs
                    .Where(a =>
                        a.CreatedAt >= sevenDaysAgo)
                    .GroupBy(a => a.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();


            // Generate every date so days with 0 activity
            // still appear on the chart.
            for (int i = 6; i >= 0; i--)
            {
                var date =
                    DateTime.UtcNow.Date.AddDays(-i);

                var count =
                    activityData
                        .Where(x => x.Date == date)
                        .Select(x => x.Count)
                        .FirstOrDefault();

                model.ActivityDateLabels.Add(
                    date.ToString("dd/MM"));

                model.ActivityDateCounts.Add(count);
            }


            // RECENT ACTIVITIES
            model.RecentActivities =
                await (
                    from activity in _context.UserActivityLogs

                    join user in _context.Users
                        on activity.UserId equals user.Id
                        into users

                    from user in users.DefaultIfEmpty()

                    orderby activity.CreatedAt descending

                    select new UserActivityLogViewModel
                    {
                        ActivityId =
                            activity.ActivityId,

                        UserId =
                            activity.UserId,

                        UserName = user != null
                            ? user.FullName
                            : "Unknown User",

                        UserEmail = user != null
                            ? user.Email ?? ""
                            : "",

                        UserRole =
                            activity.UserRole,

                        ActivityType =
                            activity.ActivityType,

                        EntityType =
                            activity.EntityType ?? "",

                        EntityId =
                            activity.EntityId ?? "",

                        Description =
                            activity.Description ?? "",

                        CreatedAt =
                            activity.CreatedAt
                    }
                )
                .Take(5)
                .ToListAsync();


            return View(model);
        }

        // GENERATE ANALYTICS REPORT
        [HttpGet]
        public async Task<IActionResult> GenerateReport(
            DateTime? dateFrom,
            DateTime? dateTo)
        {
            // VALIDATE REPORT PERIOD
            if (!dateFrom.HasValue || !dateTo.HasValue)
            {
                TempData["ErrorMessage"] =
                    "Please select both Date From and Date To.";

                return RedirectToAction(nameof(Index));
            }

            if (dateFrom.Value.Date > dateTo.Value.Date)
            {
                TempData["ErrorMessage"] =
                    "Date From cannot be later than Date To.";

                return RedirectToAction(nameof(Index));
            }

            DateTime fromDate = dateFrom.Value.Date;

            DateTime toDateExclusive =
                dateTo.Value.Date.AddDays(1);


            // JOB SEEKERS REGISTERED DURING PERIOD
            var jobSeekersInPeriod =
                _context.Users
                    .Where(u =>
                        u.UserRole == "JobSeeker" &&
                        u.CreatedAt >= fromDate &&
                        u.CreatedAt < toDateExclusive);


            // PUBLIC JOBS POSTING DURING PERIOD
            // Current Job Seeker implementation considers
            // ModerationStatus == Approved as visible/public.
            var publicJobsInPeriod =
                _context.JobPostings
                    .Include(j => j.JobCategory)
                    .Where(j =>
                        j.ModerationStatus == "Approved" &&
                        j.CreatedAt >= fromDate &&
                        j.CreatedAt < toDateExclusive);


            // Materialize because RequiredSkills needs
            // string Split(), which is easier in memory.
            var publicJobs =
                await publicJobsInPeriod
                    .ToListAsync();


            // APPLICATIONS DURING PERIOD
            var applicationsInPeriod =
                _context.JobApplications
                    .Where(a =>
                        a.AppliedDate >= fromDate &&
                        a.AppliedDate < toDateExclusive);


            // USER ACTIVITIES DURING PERIOD
            var activitiesInPeriod =
                _context.UserActivityLogs
                    .Where(a =>
                        a.CreatedAt >= fromDate &&
                        a.CreatedAt < toDateExclusive);


            // JOBS BY CATEGORY
            var jobsByCategory =
                publicJobs
                    .GroupBy(j =>
                        j.JobCategory?.CategoryName
                        ?? "Uncategorized")
                    .Select(g => new
                    {
                        Label = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Label)
                    .ToList();


            // JOBS BY LOCATION
            var jobsByLocation =
                publicJobs
                    .Where(j =>
                        !string.IsNullOrWhiteSpace(j.Location))
                    .GroupBy(j =>
                        j.Location.Trim())
                    .Select(g => new
                    {
                        Label = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Label)
                    .ToList();


            // JOBS BY EMPLOYMENT TYPE
            var jobsByEmploymentType =
                publicJobs
                    .Where(j =>
                        !string.IsNullOrWhiteSpace(
                            j.EmploymentType))
                    .GroupBy(j =>
                        j.EmploymentType!.Trim())
                    .Select(g => new
                    {
                        Label = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Label)
                    .ToList();


            // TOP REQUIRED SKILLS
            // RequiredSkills is stored as comma-separated text.
            var topRequiredSkills =
                publicJobs
                    .Where(j =>
                        !string.IsNullOrWhiteSpace(
                            j.RequiredSkills))
                    .SelectMany(j =>
                        j.RequiredSkills!
                            .Split(
                                ',',
                                StringSplitOptions
                                    .RemoveEmptyEntries))
                    .Select(skill =>
                        skill.Trim())
                    .Where(skill =>
                        !string.IsNullOrWhiteSpace(skill))
                    .GroupBy(
                        skill => skill,
                        StringComparer.OrdinalIgnoreCase)
                    .Select(g => new
                    {
                        Label = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Label)
                    .Take(10)
                    .ToList();


            // APPLICATION OUTCOMES
            int applicationsSubmitted =
                await applicationsInPeriod.CountAsync();

            int submittedApplications =
                await applicationsInPeriod.CountAsync(
                    a => a.Status == "Submitted");

            int underReviewApplications =
                await applicationsInPeriod.CountAsync(
                    a => a.Status == "Under Review");

            int shortlistedApplications =
                await applicationsInPeriod.CountAsync(
                    a => a.Status == "Shortlisted");

            int interviewApplications =
                await applicationsInPeriod.CountAsync(
                    a => a.Status == "Interview");

            int offeredApplications =
                await applicationsInPeriod.CountAsync(
                    a => a.Status == "Offered");

            int rejectedApplications =
                await applicationsInPeriod.CountAsync(
                    a => a.Status == "Rejected");


            // USER ENGAGEMENT
            int activitiesRecorded =
                await activitiesInPeriod.CountAsync();

            int activeUsers =
                await activitiesInPeriod
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync();


            var activitiesByRole =
                await activitiesInPeriod
                    .GroupBy(a => a.UserRole)
                    .Select(g => new
                    {
                        Role = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();


            // CURRENT PLATFORM SNAPSHOT
            int totalUsers =
                await _context.Users.CountAsync();

            int totalJobSeekers =
                await _context.Users.CountAsync(
                    u => u.UserRole == "JobSeeker");

            int totalPublicJobs =
                await _context.JobPostings.CountAsync(
                    j =>
                        j.ModerationStatus ==
                        "Approved");

            int totalApplications =
                await _context.JobApplications
                    .CountAsync();


            // BUILD REPORT MODEL
            var model =
                new AdminAnalyticsReportViewModel
                {
                    DateFrom =
                        fromDate,

                    DateTo =
                        dateTo.Value.Date,

                    GeneratedAt =
                        DateTime.Now,


                    // Summary
                    ApplicationsSubmitted =
                        applicationsSubmitted,

                    ActiveUsers =
                        activeUsers,


                    // Job Market - Category
                    JobCategoryLabels =
                        jobsByCategory
                            .Select(x => x.Label)
                            .ToList(),

                    JobCategoryCounts =
                        jobsByCategory
                            .Select(x => x.Count)
                            .ToList(),


                    // Job Market - Location
                    JobLocationLabels =
                        jobsByLocation
                            .Select(x => x.Label)
                            .ToList(),

                    JobLocationCounts =
                        jobsByLocation
                            .Select(x => x.Count)
                            .ToList(),


                    // Job Market - Employment Type
                    EmploymentTypeLabels =
                        jobsByEmploymentType
                            .Select(x => x.Label)
                            .ToList(),

                    EmploymentTypeCounts =
                        jobsByEmploymentType
                            .Select(x => x.Count)
                            .ToList(),


                    // Job Market - Required Skills
                    RequiredSkillLabels =
                        topRequiredSkills
                            .Select(x => x.Label)
                            .ToList(),

                    RequiredSkillCounts =
                        topRequiredSkills
                            .Select(x => x.Count)
                            .ToList(),


                    // Applications
                    SubmittedApplications =
                        submittedApplications,

                    UnderReviewApplications =
                        underReviewApplications,

                    ShortlistedApplications =
                        shortlistedApplications,

                    InterviewApplications =
                        interviewApplications,

                    OfferedApplications =
                        offeredApplications,

                    RejectedApplications =
                        rejectedApplications,


                    // Engagement
                    ActivitiesRecorded =
                        activitiesRecorded,

                    ActivityRoleLabels =
                        activitiesByRole
                            .Select(x => x.Role)
                            .ToList(),

                    ActivityRoleCounts =
                        activitiesByRole
                            .Select(x => x.Count)
                            .ToList(),


                    // Current Snapshot
                    TotalUsers =
                        totalUsers,

                    TotalJobSeekers =
                        totalJobSeekers,

                    TotalPublicJobs =
                        totalPublicJobs,

                    TotalApplications =
                        totalApplications
                };

            return View(
                "Report",
                model);
        }

    }
}