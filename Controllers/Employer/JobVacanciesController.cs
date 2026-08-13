using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "Employer")]
    public class JobVacanciesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JobVacanciesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: JobVacancies?tab=Approved&search=...
        public async Task<IActionResult> Index(string tab = "Approved", string? search = null)
        {
            string userId = _userManager.GetUserId(User)!;
            bool hasCompany = await _context.CompanyProfileTable.AnyAsync(c => c.UserId == userId);
            if (!hasCompany)
            {
                return RedirectToAction("Index", "CompanyProfiles"); // must create a company profile first
            }

            IQueryable<JobPosting> query = _context.JobPostings
                .Include(v => v.JobCategory)
                .Where(v => v.EmployerId == userId);

            query = tab switch
            {
                "Pending" => query.Where(v => v.ModerationStatus == "Pending"),
                "Rejected" => query.Where(v => v.ModerationStatus == "Rejected"),
                "Closed" => query.Where(v => v.ModerationStatus == "Approved" && v.VacancyStatus == "Closed"),
                _ => query.Where(v => v.ModerationStatus == "Approved" && v.VacancyStatus == "Open") // "Approved" tab, default
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(v => v.JobTitle.Contains(search));
            }

            ViewData["CurrentTab"] = tab;
            ViewData["CurrentSearch"] = search;

            List<JobPosting> vacancies = await query.OrderByDescending(v => v.CreatedAt).ToListAsync();
            return View(vacancies);
        }

        // GET: JobVacancies/AddData
        public async Task<IActionResult> AddData()
        {
            await PopulateCategoryDropdown();
            return View();
        }

        // POST: JobVacancies/AddData
        [HttpPost]
        public async Task<IActionResult> AddData(JobVacancyInput input)
        {
            string userId = _userManager.GetUserId(User)!;
            bool hasCompany = await _context.CompanyProfileTable.AnyAsync(c => c.UserId == userId);
            if (!hasCompany)
            {
                return RedirectToAction("Index", "CompanyProfiles");
            }

            if (input.SalaryMin.HasValue && input.SalaryMax.HasValue && input.SalaryMin > input.SalaryMax)
            {
                ModelState.AddModelError(nameof(input.SalaryMin), "Minimum salary cannot be greater than maximum salary.");
            }

            if (ModelState.IsValid)
            {
                JobPosting vacancy = new JobPosting
                {
                    EmployerId = userId,
                    CategoryId = input.JobCategoryId,
                    JobTitle = input.JobTitle!,
                    JobDescription = input.Description!,
                    RequiredSkills = input.RequiredSkills,
                    Qualification = input.Qualification,
                    EmploymentType = input.EmploymentType,
                    Location = input.Location!,
                    SalaryMin = input.SalaryMin,
                    SalaryMax = input.SalaryMax,
                    ModerationStatus = "Pending",
                    VacancyStatus = "Closed",
                    CreatedAt = DateTime.Now
                };

                _context.Add(vacancy);

                // First save generates JobVacancyId
                await _context.SaveChangesAsync();

                AddUserActivity(
                    "Create Job",
                    "JobVacancy",
                    vacancy.JobId.ToString(),
                    $"Created job posting '{vacancy.JobTitle}' and submitted it for moderation.");

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { tab = "Pending" });
            }

            await PopulateCategoryDropdown();
            return View(input);
        }

        // POST: JobVacancies/Close — submitted from the Bootstrap modal on Index
        [HttpPost]
        public async Task<IActionResult> Close(JobVacancyCloseInput input)
        {
            JobPosting? vacancy = await GetOwnedVacancyAsync(input.JobVacancyId);
            if (vacancy == null) return NotFound();

            if (vacancy.ModerationStatus != "Approved" || vacancy.VacancyStatus != "Open")
            {
                return RedirectToAction(nameof(Index), new { tab = "Approved" }); // illegal transition, ignore
            }

            if (ModelState.IsValid)
            {
                vacancy.VacancyStatus = "Closed";
                vacancy.CloseReason = input.CloseReason;

                AddUserActivity(
                    "Close Job",
                    "JobVacancy",
                    vacancy.JobId.ToString(),
                    $"Closed job vacancy '{vacancy.JobTitle}'.");

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { tab = "Approved" });
        }

        // POST: JobVacancies/Reopen/5
        [HttpPost]
        public async Task<IActionResult> Reopen(int id)
        {
            JobPosting? vacancy = await GetOwnedVacancyAsync(id);
            if (vacancy == null) return NotFound();

            if (vacancy.ModerationStatus == "Approved" && vacancy.VacancyStatus == "Closed")
            {
                vacancy.VacancyStatus = "Open";
                vacancy.CloseReason = null;

                AddUserActivity(
                    "Reopen Job",
                    "JobVacancy",
                    vacancy.JobId.ToString(),
                    $"Reopened job vacancy '{vacancy.JobTitle}'.");

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { tab = "Approved" });
        }

        // POST: JobVacancies/Delete/5 — only for Pending or Rejected postings
        [HttpPost]
        public async Task<IActionResult> Delete(int id, string tab)
        {
            JobPosting? vacancy = await GetOwnedVacancyAsync(id);
            if (vacancy == null) return NotFound();

            if (vacancy.ModerationStatus != "Approved")
            {
                AddUserActivity(
                    "Delete Job",
                    "JobVacancy",
                    vacancy.JobId.ToString(),
                    $"Deleted job posting '{vacancy.JobTitle}'.");

                _context.JobPostings.Remove(vacancy);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { tab });
        }

        // GET: JobVacancies/ViewPost/5 — employer's own view, any status, ownership-scoped
        public async Task<IActionResult> ViewPost(int id)
        {
            string userId = _userManager.GetUserId(User)!;
            bool hasCompany = await _context.CompanyProfileTable.AnyAsync(c => c.UserId == userId);
            if (!hasCompany)
            {
                return RedirectToAction("Index", "CompanyProfiles");
            }

            JobPosting? vacancy = await _context.JobPostings
                .Include(v => v.JobCategory)
                .Include(v => v.Employer)
                .FirstOrDefaultAsync(v => v.JobId == id && v.EmployerId == userId);

            if (vacancy == null) return NotFound();

            ViewBag.CompanyProfile = await _context.CompanyProfileTable
                .FirstOrDefaultAsync(c => c.UserId == vacancy.EmployerId);

            return View("Details", vacancy);
        }

        private async Task<JobPosting?> GetOwnedVacancyAsync(int id)
        {
            string userId = _userManager.GetUserId(User)!;

            return await _context.JobPostings
                .FirstOrDefaultAsync(v => v.JobId == id && v.EmployerId == userId);
        }

        private async Task PopulateCategoryDropdown()
        {
            ViewBag.Categories = new SelectList(
                await _context.JobCategories.Where(c => c.CategoryStatus == "Active").OrderBy(c => c.CategoryName).ToListAsync(),
                "CategoryId", "CategoryName");
        }

        // =========================
        // USER ACTIVITY LOG
        // =========================
        private void AddUserActivity(
            string activityType,
            string? entityType,
            string? entityId,
            string? description)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            _context.UserActivityLogs.Add(
                new UserActivityLog
                {
                    UserId = userId,
                    UserRole = "Employer",
                    ActivityType = activityType,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                });
        }
    }
}
