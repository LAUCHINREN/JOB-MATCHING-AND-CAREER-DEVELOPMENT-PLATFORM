using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace JobCareerPlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // "/" is the app's entry point. Signed-out visitors land on Login (not a public
        // landing page); signed-in visitors go straight to their role's dashboard.
        public async Task<IActionResult> Index()
        {
            if (!(User.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            ApplicationUser? user = await _userManager.GetUserAsync(User);

            return (user?.UserRole) switch
            {
                "Job Seeker" => RedirectToAction("Home", "JobSeeker"),
                "Employer" => RedirectToAction("Index", "CompanyProfiles"),
                "Career Advisor" => RedirectToAction("Index", "CareerAdvisorDashboard"),
                "SystemAdmin" => RedirectToAction("Index", "AdminDashboard"),
                _ => View()
            };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
