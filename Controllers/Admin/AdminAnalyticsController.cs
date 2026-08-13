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
    }
}