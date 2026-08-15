using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using JobCareerPlatform.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "SystemAdmin")]
    public class AdminJobModerationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminJobModerationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // LIST / SEARCH / FILTER
        public async Task<IActionResult> Index(
            string? search,
            string? status,
            int? categoryId)
        {
            var jobs =
                _context.JobPostings
                    .Include(j => j.Employer)
                    .Include(j => j.JobCategory)
                    .AsQueryable();

            // Search by Job Title or Employer Name
            if (!string.IsNullOrWhiteSpace(search))
            {
                jobs = jobs.Where(j =>
                    j.JobTitle.Contains(search) ||
                    (j.Employer != null &&
                     j.Employer.FullName.Contains(search)));
            }

            // Filter moderation status
            if (!string.IsNullOrWhiteSpace(status))
            {
                jobs = jobs.Where(j =>
                    j.ModerationStatus == status);
            }

            // Filter job category
            if (categoryId.HasValue)
            {
                jobs = jobs.Where(j =>
                    j.CategoryId ==
                    categoryId.Value);
            }

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.CategoryId = categoryId;

            ViewBag.Categories =
                await _context.JobCategories
                    .Where(c =>
                        c.CategoryStatus == "Active")
                    .OrderBy(c =>
                        c.CategoryName)
                    .ToListAsync();

            // Pending jobs appear first
            return View(
                await jobs
                    .OrderBy(j =>
                        j.ModerationStatus == "Pending"
                            ? 0
                            : 1)
                    .ThenByDescending(j =>
                        j.CreatedAt)
                    .ToListAsync());
        }

        // DETAILS
        public async Task<IActionResult> Details(
            int id)
        {
            var job =
                await _context.JobPostings
                    .Include(j => j.Employer)
                    .Include(j => j.JobCategory)
                    .FirstOrDefaultAsync(j =>
                        j.JobId == id);

            if (job == null)
            {
                return NotFound();
            }

            ViewBag.ModerationHistory =
                await _context.JobModerations
                    .Where(m =>
                        m.JobId == id)
                    .OrderByDescending(m =>
                        m.ModeratedAt)
                    .ToListAsync();

            return View(job);
        }

        // APPROVE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            int id)
        {
            var job =
                await _context.JobPostings
                    .FindAsync(id);

            if (job == null)
            {
                return NotFound();
            }

            if (job.ModerationStatus != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending job postings can be approved.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var previousStatus =
                job.ModerationStatus;

            job.ModerationStatus =
                "Approved";

            job.VacancyStatus =
                "Open";

            job.ModeratedAt =
                DateTime.UtcNow;

            AddModerationHistory(
                job.JobId,
                previousStatus,
                "Approved",
                null);

            AddUserActivity(
                "Approve Job",
                "JobPosting",
                job.JobId.ToString(),
                $"Approved job posting '{job.JobTitle}'.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Job posting approved successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // REJECT - GET
        [HttpGet]
        public async Task<IActionResult> Reject(
            int id)
        {
            var job =
                await _context.JobPostings
                    .Include(j => j.Employer)
                    .FirstOrDefaultAsync(j =>
                        j.JobId == id);

            if (job == null)
            {
                return NotFound();
            }

            if (job.ModerationStatus != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending job postings can be rejected.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var model =
                new RejectJobViewModel
                {
                    JobId = job.JobId,
                    JobTitle = job.JobTitle,
                    EmployerName =
                        job.Employer?.FullName
                        ?? "Unknown Employer"
                };

            return View(model);
        }

        // REJECT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            RejectJobViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var job =
                await _context.JobPostings
                    .FindAsync(model.JobId);

            if (job == null)
            {
                return NotFound();
            }

            if (job.ModerationStatus != "Pending")
            {
                TempData["ErrorMessage"] =
                    "Only pending job postings can be rejected.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = job.JobId });
            }

            var previousStatus =
                job.ModerationStatus;

            job.ModerationStatus =
                "Rejected";

            job.RejectionReason =
                model.Reason;

            job.ModeratedAt =
                DateTime.UtcNow;

            AddModerationHistory(
                job.JobId,
                previousStatus,
                "Rejected",
                model.Reason);

            AddUserActivity(
                "Reject Job",
                "JobPosting",
                job.JobId.ToString(),
                $"Rejected job posting '{job.JobTitle}'. Reason: {model.Reason}");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Job posting has been rejected.";

            return RedirectToAction(
                nameof(Details),
                new { id = job.JobId });
        }

        // MODERATION HISTORY
        private void AddModerationHistory(
            int jobId,
            string previousStatus,
            string newStatus,
            string? reason)
        {
            var adminId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(adminId))
            {
                return;
            }

            _context.JobModerations.Add(
                new JobModeration
                {
                    JobId = jobId,
                    AdminId = adminId,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    Reason = reason,
                    ModeratedAt = DateTime.UtcNow
                });
        }

        // USER ACTIVITY LOG
        private void AddUserActivity(
            string activityType,
            string? entityType,
            string? entityId,
            string? description)
        {
            var adminId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(adminId))
            {
                return;
            }

            _context.UserActivityLogs.Add(
                new UserActivityLog
                {
                    UserId = adminId,
                    UserRole = "SystemAdmin",
                    ActivityType = activityType,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                });
        }
    }
}