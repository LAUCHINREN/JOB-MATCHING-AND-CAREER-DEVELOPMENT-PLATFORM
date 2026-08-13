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
    public class AdminJobCategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminJobCategoriesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // READ - LIST
        public async Task<IActionResult> Index(
    string? search,
    string? status,
    int? parentCategoryId)
        {
            var categories = _context.JobCategories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .AsQueryable();

            // Search category name
            if (!string.IsNullOrWhiteSpace(search))
            {
                categories = categories.Where(c =>
                    c.CategoryName.Contains(search));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status))
            {
                categories = categories.Where(c =>
                    c.CategoryStatus == status);
            }

            // Filter by selected parent category
            // Show BOTH:
            // 1. The selected parent category itself
            // 2. Its subcategories
            if (parentCategoryId.HasValue)
            {
                categories = categories.Where(c =>
                    c.CategoryId == parentCategoryId.Value ||
                    c.ParentCategoryId == parentCategoryId.Value);
            }

            // Keep selected filter values after searching
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.ParentCategoryId = parentCategoryId;

            // Only main categories are shown in the Parent Category filter
            ViewBag.MainCategories = await _context.JobCategories
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            // If a parent is selected, show the parent first,
            // followed by its subcategories alphabetically
            if (parentCategoryId.HasValue)
            {
                return View(
                    await categories
                        .OrderBy(c =>
                            c.CategoryId == parentCategoryId.Value ? 0 : 1)
                        .ThenBy(c => c.CategoryName)
                        .ToListAsync());
            }

            return View(
                await categories
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync());
        }

        // READ - DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var category = await _context.JobCategories
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // CREATE - GET
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadParentCategories();

            return View(
                new AdminJobCategoryViewModel());
        }

        // CREATE - POST

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AdminJobCategoryViewModel model)
        {
            await LoadParentCategories();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string normalizedName =
                model.CategoryName.Trim().ToLower();

            bool duplicate =
                await _context.JobCategories.AnyAsync(c =>
                    c.CategoryName.ToLower() == normalizedName);

            if (duplicate)
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "A job category with this name already exists.");

                return View(model);
            }

            var category = new JobCategory
            {
                CategoryName = model.CategoryName.Trim(),
                Description = model.Description?.Trim(),
                ParentCategoryId = model.ParentCategoryId,
                CategoryStatus = "Active",
                CreatedAt = DateTime.UtcNow
            };

            _context.JobCategories.Add(category);

            AddUserActivity(
                "Create Category",
                "JobCategory",
                category.CategoryId.ToString(),
                $"Created job category '{category.CategoryName}'.");

            await _context.SaveChangesAsync();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Job category created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // UPDATE - GET
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category =
                await _context.JobCategories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            await LoadParentCategories(id);

            var model = new AdminJobCategoryViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId
            };

            return View(model);
        }

        // UPDATE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            AdminJobCategoryViewModel model)
        {
            await LoadParentCategories(model.CategoryId);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var category =
                await _context.JobCategories
                    .FindAsync(model.CategoryId);

            if (category == null)
            {
                return NotFound();
            }

            if (model.ParentCategoryId == model.CategoryId)
            {
                ModelState.AddModelError(
                    "ParentCategoryId",
                    "A category cannot be its own parent.");

                return View(model);
            }

            string normalizedName =
                model.CategoryName.Trim().ToLower();

            bool duplicate =
                await _context.JobCategories.AnyAsync(c =>
                    c.CategoryId != model.CategoryId &&
                    c.CategoryName.ToLower() == normalizedName);

            if (duplicate)
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "Another category with this name already exists.");

                return View(model);
            }

            string oldName = category.CategoryName;

            category.CategoryName =
                model.CategoryName.Trim();

            category.Description =
                model.Description?.Trim();

            category.ParentCategoryId =
                model.ParentCategoryId;

            category.UpdatedAt =
                DateTime.UtcNow;

            AddUserActivity(
                "Update Category",
                "JobCategory",
                category.CategoryId.ToString(),
                $"Updated job category '{category.CategoryName}'.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Job category updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = category.CategoryId });
        }

        // DEACTIVATE

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var category =
                await _context.JobCategories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            category.CategoryStatus = "Inactive";
            category.UpdatedAt = DateTime.UtcNow;

            AddUserActivity(
                "Deactivate Category",
                "JobCategory",
                category.CategoryId.ToString(),
                $"Deactivated job category '{category.CategoryName}'.");


            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Job category has been deactivated.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // REACTIVATE

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(int id)
        {
            var category =
                await _context.JobCategories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            category.CategoryStatus = "Active";
            category.UpdatedAt = DateTime.UtcNow;

            AddUserActivity(
                "Reactivate Category",
                "JobCategory",
                category.CategoryId.ToString(),
                $"Reactivated job category '{category.CategoryName}'.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Job category has been reactivated.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // DELETE - GET
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.JobCategories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        //  DELETE - POST

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.JobCategories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            // Do not delete a category which still has subcategories
            if (category.SubCategories.Any())
            {
                TempData["ErrorMessage"] =
                    "This category cannot be deleted because it contains subcategories.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            string categoryName =
                category.CategoryName;

            _context.JobCategories.Remove(category);

            AddUserActivity(
                "Delete Category",
                "JobCategory",
                category.CategoryId.ToString(),
                $"Deleted job category '{category.CategoryName}'.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Job category deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadParentCategories(
            int? excludeCategoryId = null)
        {
            var categories =
                _context.JobCategories
                    .Where(c =>
                        c.CategoryStatus == "Active");

            if (excludeCategoryId.HasValue)
            {
                categories = categories.Where(c =>
                    c.CategoryId != excludeCategoryId.Value);
            }

            ViewBag.ParentCategories =
                await categories
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();
        }

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