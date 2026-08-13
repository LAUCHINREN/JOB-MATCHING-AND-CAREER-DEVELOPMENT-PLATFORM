using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "Employer")]
    public class FitScoreSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FitScoreSettingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: FitScoreSettings
        public async Task<IActionResult> Index()
        {
            string userId = _userManager.GetUserId(User)!;
            CompanyProfile? company = await _context.CompanyProfileTable.FirstOrDefaultAsync(c => c.UserId == userId);
            if (company == null)
            {
                return RedirectToAction("Index", "CompanyProfiles");
            }

            FitScoreSettings? settings = await _context.FitScoreSettings
                .FirstOrDefaultAsync(s => s.CompanyProfileId == company.CompanyProfileId);

            FitScoreSettingsInput input = settings == null
                ? new FitScoreSettingsInput() // no row yet — property defaults (35/30/25/10) apply
                : new FitScoreSettingsInput
                {
                    SalaryWeight = settings.SalaryWeight,
                    CategoryWeight = settings.CategoryWeight,
                    LocationWeight = settings.LocationWeight,
                    EducationWeight = settings.EducationWeight
                };

            return View(input);
        }

        // POST: FitScoreSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FitScoreSettingsInput input)
        {
            string userId = _userManager.GetUserId(User)!;
            CompanyProfile? company = await _context.CompanyProfileTable.FirstOrDefaultAsync(c => c.UserId == userId);
            if (company == null)
            {
                return RedirectToAction("Index", "CompanyProfiles");
            }

            int sum = input.SalaryWeight + input.CategoryWeight + input.LocationWeight + input.EducationWeight;
            if (sum != 100)
            {
                ModelState.AddModelError(string.Empty, $"Weights must add up to 100 (currently {sum}).");
            }

            if (ModelState.IsValid)
            {
                FitScoreSettings? settings = await _context.FitScoreSettings
                    .FirstOrDefaultAsync(s => s.CompanyProfileId == company.CompanyProfileId);

                if (settings == null)
                {
                    settings = new FitScoreSettings { CompanyProfileId = company.CompanyProfileId };
                    _context.Add(settings);
                }

                settings.SalaryWeight = input.SalaryWeight;
                settings.CategoryWeight = input.CategoryWeight;
                settings.LocationWeight = input.LocationWeight;
                settings.EducationWeight = input.EducationWeight;

                await _context.SaveChangesAsync();
                TempData["SavedMessage"] = "Matching preferences updated.";
                return RedirectToAction(nameof(Index));
            }

            return View(input);
        }
    }
}
