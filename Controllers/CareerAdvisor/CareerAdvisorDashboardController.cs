using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "CareerAdvisor")]
    public class CareerAdvisorDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CareerAdvisorDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            string userId = _userManager.GetUserId(User)!;

            List<CareerResource> publishedResources = await _context.CareerResources
                .Where(r => r.CreatedByUserId == userId && r.IsPublished)
                .ToListAsync();

            ViewBag.PublishedResourceCount = publishedResources.Count;

            ViewBag.DraftResourceCount = await _context.CareerResources
                .CountAsync(r => r.CreatedByUserId == userId && !r.IsPublished);

            ViewBag.AutoMatchCount = await CountAutoMatchesAsync(publishedResources);

            ViewBag.UpcomingSessionCount = await _context.CounsellingSessions
                .CountAsync(s => s.CareerAdvisorUserId == userId
                    && s.Status == "Approved"
                    && s.ScheduledAt >= DateTime.Now);

            ViewBag.PendingSessionCount = await _context.CounsellingSessions
                .CountAsync(s => s.Status == "Pending");

            ViewBag.JobSeekerCount = await _userManager.Users
                .CountAsync(u => u.UserRole == "Job Seeker");

            ViewBag.AssessmentCount = await _context.SkillAssessments
                .CountAsync(a => a.CreatedByUserId == userId);

            return View();
        }

        // Total (resource, job seeker) pairs this advisor's published resources are automatically
        // matched to right now — the "reach" of their published content.
        private async Task<int> CountAutoMatchesAsync(List<CareerResource> publishedResources)
        {
            if (!publishedResources.Any())
            {
                return 0;
            }

            List<ApplicationUser> jobSeekers = await _userManager.Users
                .Where(u => u.UserRole == "Job Seeker")
                .ToListAsync();

            List<string> jobSeekerIds = jobSeekers.Select(u => u.Id).ToList();

            Dictionary<string, JobSeekerProfile> profiles = await _context.JobSeekerProfiles
                .Where(p => jobSeekerIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId);

            List<JobApplication> applications = await _context.JobApplications
                .Include(a => a.JobPosting)
                .Where(a => jobSeekerIds.Contains(a.UserId))
                .ToListAsync();

            Dictionary<string, List<string>> appliedSkillsByJobSeeker = applications
                .Where(a => a.JobPosting != null && !string.IsNullOrWhiteSpace(a.JobPosting.RequiredSkills))
                .GroupBy(a => a.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(a => ResourceMatcher.SplitTags(a.JobPosting!.RequiredSkills))
                          .Distinct(StringComparer.OrdinalIgnoreCase)
                          .ToList());

            int total = 0;
            foreach (CareerResource resource in publishedResources)
            {
                foreach (ApplicationUser jobSeeker in jobSeekers)
                {
                    profiles.TryGetValue(jobSeeker.Id, out var profile);
                    List<string> appliedSkills = appliedSkillsByJobSeeker.GetValueOrDefault(jobSeeker.Id) ?? new List<string>();

                    if (ResourceMatcher.Evaluate(resource, profile, appliedSkills) != null)
                    {
                        total++;
                    }
                }
            }

            return total;
        }
    }
}
