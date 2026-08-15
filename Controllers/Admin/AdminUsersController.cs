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
    public class AdminUsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminUsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // LIST / SEARCH / FILTER
        public async Task<IActionResult> Index(
            string? search,
            string? role,
            string? status)
        {
            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(u =>
                    u.FullName.Contains(search) ||
                    (u.Email != null &&
                     u.Email.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                string[] matchingRoles = role switch
                {
                    "JobSeeker" =>
                        new[] { "JobSeeker", "Job Seeker" },

                    "CareerAdvisor" =>
                        new[] { "CareerAdvisor", "Career Advisor" },

                    "SystemAdmin" =>
                        new[] { "SystemAdmin", "System Admin" },

                    "Employer" =>
                        new[] { "Employer" },

                    _ =>
                        new[] { role }
                };

                users = users.Where(u =>
                    matchingRoles.Contains(u.UserRole));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                users = users.Where(u =>
                    u.AccountStatus == status);
            }

            ViewBag.Search = search;
            ViewBag.Role = role;
            ViewBag.Status = status;

            return View(
                await users
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync());
        }

        // DETAILS
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // CREATE - GET
        [HttpGet]
        public IActionResult Create()
        {
            LoadRoles();
            return View();
        }

        // CREATE - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AdminCreateUserViewModel model)
        {
            LoadRoles();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser =
                await _userManager.FindByEmailAsync(
                    model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email address is already registered.");

                return View(model);
            }

            if (!await _roleManager
                .RoleExistsAsync(model.UserRole))
            {
                ModelState.AddModelError(
                    "UserRole",
                    "The selected role is invalid.");

                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                FullName = model.FullName,
                UserRole = model.UserRole,
                AccountStatus = "Active",
                CreatedAt = DateTime.UtcNow
            };

            var result =
                await _userManager.CreateAsync(
                    user,
                    model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(
                    user,
                    model.UserRole);

                AddUserActivity(
                    "Create User",
                    "ApplicationUser",
                    user.Id,
                    $"Created user account '{user.Email}' with role '{user.UserRole}'.");

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "User account created successfully.";

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }

        // EDIT - GET
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            LoadRoles();

            var model = new AdminEditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                UserRole = user.UserRole
            };

            return View(model);
        }

        // EDIT - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            AdminEditUserViewModel model)
        {
            LoadRoles();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user =
                await _userManager.FindByIdAsync(
                    model.Id);

            if (user == null)
            {
                return NotFound();
            }

            var emailOwner =
                await _userManager.FindByEmailAsync(
                    model.Email);

            if (emailOwner != null &&
                emailOwner.Id != user.Id)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email address is already used by another account.");

                return View(model);
            }

            if (!await _roleManager
                .RoleExistsAsync(model.UserRole))
            {
                ModelState.AddModelError(
                    "UserRole",
                    "The selected role is invalid.");

                return View(model);
            }

            var oldRole = user.UserRole;

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.UserRole = model.UserRole;

            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var error
                    in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            // Synchronize ASP.NET Identity role
            if (oldRole != model.UserRole)
            {
                var currentRoles =
                    await _userManager
                        .GetRolesAsync(user);

                if (currentRoles.Any())
                {
                    await _userManager
                        .RemoveFromRolesAsync(
                            user,
                            currentRoles);
                }

                await _userManager.AddToRoleAsync(
                    user,
                    model.UserRole);
            }

            AddUserActivity(
                "Update User",
                "ApplicationUser",
                user.Id,
                $"Updated user account '{user.Email}'.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "User account updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new { id = user.Id });
        }

        // DEACTIVATE - GET
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var currentUserId =
                _userManager.GetUserId(User);

            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] =
                    "You cannot deactivate your own administrator account.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var model =
                new AdminAccountActionViewModel
                {
                    UserId = user.Id,
                    UserName = user.FullName,
                    Email = user.Email ?? string.Empty
                };

            return View(model);
        }

        // DEACTIVATE - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            AdminAccountActionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Delete", model);
            }

            var user =
                await _userManager.FindByIdAsync(
                    model.UserId);

            if (user == null)
            {
                return NotFound();
            }

            var currentUserId =
                _userManager.GetUserId(User);

            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] =
                    "You cannot deactivate your own administrator account.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = user.Id });
            }

            user.AccountStatus = "Deactivated";
            user.LockoutEnabled = true;
            user.LockoutEnd =
                DateTimeOffset.MaxValue;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to deactivate this account.");

                return View("Delete", model);
            }

            AddUserActivity(
                "Deactivate User",
                "ApplicationUser",
                user.Id,
                $"Deactivated user account '{user.Email}'. Reason: {model.Reason}");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "User account has been deactivated.";

            return RedirectToAction(nameof(Index));
        }

        // SUSPEND - GET
        [HttpGet]
        public async Task<IActionResult> Suspend(string id)
        {
            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var currentUserId =
                _userManager.GetUserId(User);

            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] =
                    "You cannot suspend your own administrator account.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var model =
                new AdminAccountActionViewModel
                {
                    UserId = user.Id,
                    UserName = user.FullName,
                    Email = user.Email ?? string.Empty
                };

            return View(model);
        }

        // SUSPEND - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(
            AdminAccountActionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user =
                await _userManager.FindByIdAsync(
                    model.UserId);

            if (user == null)
            {
                return NotFound();
            }

            var currentUserId =
                _userManager.GetUserId(User);

            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] =
                    "You cannot suspend your own administrator account.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = user.Id });
            }

            user.AccountStatus = "Suspended";
            user.LockoutEnabled = true;
            user.LockoutEnd =
                DateTimeOffset.MaxValue;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to suspend this account.");

                return View(model);
            }

            AddUserActivity(
                "Suspend User",
                "ApplicationUser",
                user.Id,
                $"Suspended user account '{user.Email}'. Reason: {model.Reason}");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "User account has been suspended.";

            return RedirectToAction(
                nameof(Details),
                new { id = user.Id });
        }

        // REACTIVATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reactivate(
            string id)
        {
            var user =
                await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.AccountStatus = "Active";

            user.LockoutEnabled = false;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    "Unable to reactivate this account.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            AddUserActivity(
                "Reactivate User",
                "ApplicationUser",
                user.Id,
                $"Reactivated user account '{user.Email}'.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "User account has been reactivated.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        // LOAD ROLES
        private void LoadRoles()
        {
            ViewBag.Roles = new[]
            {
                "JobSeeker",
                "Employer",
                "CareerAdvisor",
                "SystemAdmin"
            };
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