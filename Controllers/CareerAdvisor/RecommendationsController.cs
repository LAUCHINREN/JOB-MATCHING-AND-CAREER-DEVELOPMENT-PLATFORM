using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "CareerAdvisor")]
    public class RecommendationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RecommendationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Recommendations — read-only reach report. Resources are no longer manually
        // recommended to individual job seekers; instead ResourceMatcher automatically matches
        // this advisor's published resources to job seekers by preferred category, field of
        // study, and the required skills of jobs they've applied to.
        public async Task<IActionResult> Index()
        {
            string userId = _userManager.GetUserId(User)!;

            List<CareerResource> resources = await _context.CareerResources
                .Where(r => r.CreatedByUserId == userId && r.IsPublished)
                .OrderByDescending(r => r.PublishedAt)
                .ToListAsync();

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

            List<ResourceReach> reach = resources.Select(resource =>
            {
                List<MatchedJobSeeker> matched = jobSeekers
                    .Select(js =>
                    {
                        profiles.TryGetValue(js.Id, out var profile);
                        List<string> appliedSkills = appliedSkillsByJobSeeker.GetValueOrDefault(js.Id) ?? new List<string>();
                        ResourceMatch? match = ResourceMatcher.Evaluate(resource, profile, appliedSkills);

                        return match == null ? null : new MatchedJobSeeker
                        {
                            UserId = js.Id,
                            FullName = profile?.FullName ?? js.FullName,
                            Reasons = match.Reasons
                        };
                    })
                    .Where(m => m != null)
                    .Select(m => m!)
                    .ToList();

                return new ResourceReach { Resource = resource, MatchedJobSeekers = matched };
            }).ToList();

            return View(reach);
        }
    }
}
