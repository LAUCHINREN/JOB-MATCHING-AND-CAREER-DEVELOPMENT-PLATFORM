using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "CareerAdvisor")]
    public class JobSeekerReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JobSeekerReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: JobSeekerReview — every job seeker, with an assessment summary
        public async Task<IActionResult> Index(string? search)
        {
            var jobSeekersQuery = _userManager.Users.Where(u => u.UserRole == "Job Seeker");

            if (!string.IsNullOrWhiteSpace(search))
            {
                jobSeekersQuery = jobSeekersQuery.Where(u =>
                    u.FullName.Contains(search) ||
                    (u.Email != null && u.Email.Contains(search)));
            }

            List<ApplicationUser> jobSeekers = await jobSeekersQuery
                .OrderBy(u => u.FullName)
                .ToListAsync();

            List<string> userIds = jobSeekers.Select(u => u.Id).ToList();

            List<AssessmentResult> allResults = await _context.AssessmentResults
                .Where(r => userIds.Contains(r.UserId))
                .ToListAsync();

            Dictionary<string, JobSeekerProfile> profiles = await _context.JobSeekerProfiles
                .Where(p => userIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId);

            List<JobSeekerAssessmentSummaryViewModel> summaries = jobSeekers.Select(u =>
            {
                var results = allResults.Where(r => r.UserId == u.Id).ToList();
                profiles.TryGetValue(u.Id, out var profile);

                return new JobSeekerAssessmentSummaryViewModel
                {
                    UserId = u.Id,
                    FullName = profile?.FullName ?? u.FullName,
                    Email = u.Email,
                    PreferredJobCategory = profile?.PreferredJobCategory,
                    AssessmentsTaken = results.Count,
                    AssessmentsPassed = results.Count(r => r.IsPassed),
                    AverageScore = results.Count > 0 ? results.Average(r => r.Score) : 0
                };
            }).ToList();

            ViewBag.Search = search;
            return View(summaries);
        }

        // GET: JobSeekerReview/Details/{userId} — full assessment history for one job seeker
        public async Task<IActionResult> Details(string userId)
        {
            ApplicationUser? jobSeeker = await _userManager.FindByIdAsync(userId);

            if (jobSeeker == null || jobSeeker.UserRole != "Job Seeker")
            {
                return NotFound();
            }

            JobSeekerProfile? profile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            List<AssessmentResult> results = await _context.AssessmentResults
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CompletedDate)
                .ToListAsync();

            List<int> assessmentIds = results.Select(r => r.SkillAssessmentId).Distinct().ToList();

            Dictionary<int, SkillAssessment> assessments = await _context.SkillAssessments
                .Where(a => assessmentIds.Contains(a.SkillAssessmentId))
                .ToDictionaryAsync(a => a.SkillAssessmentId);

            JobSeekerAssessmentDetailViewModel model = new JobSeekerAssessmentDetailViewModel
            {
                Profile = profile,
                UserId = jobSeeker.Id,
                FullName = profile?.FullName ?? jobSeeker.FullName,
                Email = jobSeeker.Email,
                Attempts = results.Select(r =>
                {
                    assessments.TryGetValue(r.SkillAssessmentId, out var assessment);
                    return new JobSeekerAssessmentAttempt
                    {
                        AssessmentTitle = assessment?.Title ?? "Unknown Assessment",
                        SkillName = assessment?.SkillName ?? "-",
                        Score = r.Score,
                        IsPassed = r.IsPassed,
                        CompletedDate = r.CompletedDate
                    };
                }).ToList()
            };

            // Resources automatically matched to this job seeker (category / field of study /
            // applied-job required skills) — replaces the old manual "recommend a resource" picker.
            List<CareerResource> publishedResources = await _context.CareerResources
                .Where(r => r.IsPublished)
                .OrderBy(r => r.Title)
                .ToListAsync();

            List<string> appliedSkills = await _context.JobApplications
                .Include(a => a.JobPosting)
                .Where(a => a.UserId == userId && a.JobPosting != null && a.JobPosting.RequiredSkills != null)
                .Select(a => a.JobPosting!.RequiredSkills!)
                .ToListAsync();

            List<string> flatAppliedSkills = appliedSkills
                .SelectMany(ResourceMatcher.SplitTags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            ViewBag.MatchedResources = publishedResources
                .Select(r => ResourceMatcher.Evaluate(r, profile, flatAppliedSkills))
                .Where(m => m != null)
                .ToList();

            return View(model);
        }
    }
}
