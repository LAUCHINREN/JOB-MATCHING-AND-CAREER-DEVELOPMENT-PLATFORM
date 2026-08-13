using JobCareerPlatform.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "SystemAdmin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers =
                await _userManager.Users.CountAsync();

            ViewBag.JobPostingCount =
                await _context.JobPostings.CountAsync(j => j.ModerationStatus == "Approved");

            ViewBag.PendingModerationCount =
                await _context.JobPostings.CountAsync(j => j.ModerationStatus == "Pending");

            ViewBag.ApplicationCount =
                await _context.JobApplications.CountAsync();

            return View();
        }
    }
}