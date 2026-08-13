using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using JobCareerPlatform.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "SystemAdmin")]
    public class AdminActivityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminActivityController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // ACTIVITY MONITORING
        public async Task<IActionResult> Index(
            string? search,
            string? role,
            string? activityType,
            string? entityType,
            DateTime? dateFrom,
            DateTime? dateTo)
        {
            var query =
                from activity in _context.UserActivityLogs

                join user in _context.Users
                    on activity.UserId equals user.Id
                    into userGroup

                from user in userGroup.DefaultIfEmpty()

                select new UserActivityLogViewModel
                {
                    ActivityId = activity.ActivityId,

                    UserId = activity.UserId,

                    UserName = user != null
                        ? user.FullName
                        : "Unknown User",

                    UserEmail = user != null
                        ? user.Email ?? ""
                        : "",

                    UserRole = activity.UserRole,

                    ActivityType = activity.ActivityType,

                    EntityType =
                        activity.EntityType ?? "",

                    EntityId =
                        activity.EntityId ?? "",

                    Description =
                        activity.Description ?? "",

                    CreatedAt = activity.CreatedAt
                };


            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.UserName.Contains(search) ||
                    x.UserEmail.Contains(search) ||
                    x.ActivityType.Contains(search) ||
                    x.EntityType.Contains(search) ||
                    x.Description.Contains(search));
            }


            // ROLE FILTER
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(x =>
                    x.UserRole == role);
            }


            // ACTIVITY TYPE FILTER
            if (!string.IsNullOrWhiteSpace(activityType))
            {
                query = query.Where(x =>
                    x.ActivityType == activityType);
            }


            // MODULE FILTER
            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(x =>
                    x.EntityType == entityType);
            }


            // DATE FROM
            if (dateFrom.HasValue)
            {
                var fromDate =
                    dateFrom.Value.Date;

                query = query.Where(x =>
                    x.CreatedAt >= fromDate);
            }


            // DATE TO
            if (dateTo.HasValue)
            {
                var nextDate =
                    dateTo.Value.Date.AddDays(1);

                query = query.Where(x =>
                    x.CreatedAt < nextDate);
            }


            // PRESERVE FILTERS
            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.ActivityType = activityType;
            ViewBag.EntityType = entityType;

            ViewBag.DateFrom =
                dateFrom?.ToString("yyyy-MM-dd");

            ViewBag.DateTo =
                dateTo?.ToString("yyyy-MM-dd");


            // ACTIVITY TYPE DROPDOWN
            ViewBag.ActivityTypes =
                await _context.UserActivityLogs
                    .Select(x => x.ActivityType)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();


            // MODULE DROPDOWN
            ViewBag.EntityTypes =
                await _context.UserActivityLogs
                    .Where(x => x.EntityType != null)
                    .Select(x => x.EntityType!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();


            var results =
                await query
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();


            return View(results);
        }


        // ACTIVITY DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var activity =
                await (
                    from log in _context.UserActivityLogs

                    join user in _context.Users
                        on log.UserId equals user.Id
                        into userGroup

                    from user in userGroup.DefaultIfEmpty()

                    where log.ActivityId == id

                    select new UserActivityLogViewModel
                    {
                        ActivityId =
                            log.ActivityId,

                        UserId =
                            log.UserId,

                        UserName = user != null
                            ? user.FullName
                            : "Unknown User",

                        UserEmail = user != null
                            ? user.Email ?? ""
                            : "",

                        UserRole =
                            log.UserRole,

                        ActivityType =
                            log.ActivityType,

                        EntityType =
                            log.EntityType ?? "",

                        EntityId =
                            log.EntityId ?? "",

                        Description =
                            log.Description ?? "",

                        CreatedAt =
                            log.CreatedAt
                    }
                )
                .FirstOrDefaultAsync();


            if (activity == null)
            {
                return NotFound();
            }


            return View(activity);
        }
    }
}