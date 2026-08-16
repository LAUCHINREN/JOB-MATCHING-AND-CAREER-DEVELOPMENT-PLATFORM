using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "Employer")]
    public class JobApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        private static readonly string[] ActiveStatuses =
            { "Submitted", "Under Review", "Shortlisted", "Interview", "Offered" };

        private static readonly string[] EmployerSettableStatuses =
            { "Submitted", "Under Review", "Shortlisted", "Interview", "Offered", "Rejected" };
        // "Withdrawn" is deliberately excluded — only the job seeker can set that

        public JobApplicationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        // GET: JobApplications?vacancyId=3&status=Shortlisted&search=...&sortBy=fit
        public async Task<IActionResult> Index(int? vacancyId, string? status, string? search, string? sortBy)
        {
            string userId = _userManager.GetUserId(User)!;
            bool hasCompany = await _context.CompanyProfileTable.AnyAsync(c => c.UserId == userId);
            if (!hasCompany)
            {
                return RedirectToAction("Index", "CompanyProfiles");
            }

            List<JobPosting> myVacancies = await _context.JobPostings
                .Where(v => v.EmployerId == userId && v.VacancyStatus == "Open")
                .OrderBy(v => v.JobTitle)
                .ToListAsync();

            List<int> myVacancyIds = myVacancies.Select(v => v.JobId).ToList();

            IQueryable<JobApplication> baseQuery = _context.JobApplications
                .Include(a => a.JobPosting)
                    .ThenInclude(v => v!.JobCategory)
                .Where(a => myVacancyIds.Contains(a.JobId));

            if (vacancyId.HasValue)
            {
                baseQuery = baseQuery.Where(a => a.JobId == vacancyId);
            }

            // Tiles: vacancy filter applied, status filter NOT applied — keeps every tile clickable
            Dictionary<string, int> statusCounts = await baseQuery
                .GroupBy(a => a.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            IQueryable<JobApplication> listQuery = baseQuery;

            if (!string.IsNullOrWhiteSpace(status))
            {
                listQuery = listQuery.Where(a => a.Status == status);
            }

            List<JobApplication> applications = await listQuery
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            // Look up job seeker profiles for the applicants shown (manual lookup — no hard FK
            // from JobApplication to JobSeekerProfile, since a profile isn't guaranteed to exist).
            List<string> applicantUserIds = applications.Select(a => a.UserId).Distinct().ToList();
            Dictionary<string, JobSeekerProfile> profiles = await _context.JobSeekerProfiles
                .Where(p => applicantUserIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId);

            // Fallback name for applicants who applied before ever creating a JobSeekerProfile.
            Dictionary<string, string> names = await _userManager.Users
                .Where(u => applicantUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            // Assessment results — scoped to applicantUserIds, i.e. only job seekers who applied
            // to one of THIS employer's own vacancies. An employer can never see the assessment
            // history of a job seeker who hasn't applied to them.
            List<AssessmentResult> assessmentResults = await _context.AssessmentResults
                .Where(r => applicantUserIds.Contains(r.UserId))
                .OrderByDescending(r => r.CompletedDate)
                .ToListAsync();

            Dictionary<string, List<AssessmentResult>> assessmentsByApplicant = assessmentResults
                .GroupBy(r => r.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<int> assessmentIds = assessmentResults.Select(r => r.SkillAssessmentId).Distinct().ToList();
            Dictionary<int, SkillAssessment> assessmentCatalog = await _context.SkillAssessments
                .Where(a => assessmentIds.Contains(a.SkillAssessmentId))
                .ToDictionaryAsync(a => a.SkillAssessmentId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                applications = applications.Where(a =>
                    (profiles.TryGetValue(a.UserId, out var prof) && prof.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (a.CoverMessage != null && a.CoverMessage.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                    (a.JobPosting != null && a.JobPosting.JobTitle.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Fit score: silent fallback to defaults if the employer hasn't visited Matching Preferences yet
            CompanyProfile? company = await _context.CompanyProfileTable.FirstOrDefaultAsync(c => c.UserId == userId);
            FitScoreSettings? savedSettings = company == null
                ? null
                : await _context.FitScoreSettings.FirstOrDefaultAsync(s => s.CompanyProfileId == company.CompanyProfileId);
            FitScoreSettings settings = savedSettings ?? new FitScoreSettings(); // property initializers = 35/30/25/10

            Dictionary<int, List<FitScoreAspect>> fitBreakdowns = applications.ToDictionary(
                a => a.JobApplicationId,
                a => FitScoreCalculator.GetBreakdown(a, profiles.GetValueOrDefault(a.UserId), settings));

            Dictionary<int, int> fitScores = fitBreakdowns.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Sum(b => b.PointsEarned));

            if (sortBy == "fit")
            {
                applications = applications.OrderByDescending(a => fitScores[a.JobApplicationId]).ToList();
            }

            ApplicantDashboardViewModel model = new ApplicantDashboardViewModel
            {
                Applications = applications,
                StatusCounts = statusCounts,
                MyVacancies = myVacancies,
                Profiles = profiles,
                SelectedVacancyId = vacancyId,
                SelectedStatus = status,
                Search = search,
                FitScores = fitScores,
                FitBreakdowns = fitBreakdowns,
                SortBy = sortBy,
                AssessmentsByApplicant = assessmentsByApplicant,
                AssessmentCatalog = assessmentCatalog,
                Names = names
            };

            return View(model);
        }

        // POST: JobApplications/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus, string? rejectionNote,
            int? vacancyId, string? status, string? search)
        {
            JobApplication? application = await GetOwnedApplicationAsync(id);
            if (application == null) return NotFound();

            bool currentIsActive = ActiveStatuses.Contains(application.Status);
            bool targetIsLegal = EmployerSettableStatuses.Contains(newStatus);

            if (currentIsActive && targetIsLegal)
            {
                if (newStatus == "Rejected" && string.IsNullOrWhiteSpace(rejectionNote))
                {
                    TempData["StatusError"] = "Please provide a reason for rejecting this candidate.";
                    return RedirectToAction(nameof(Index), new { vacancyId, status, search });
                }

                application.Status = newStatus;
                application.RejectionNote = newStatus == "Rejected" ? rejectionNote : null;

                AddUserActivity(
                    "Update Application Status",
                    "JobApplication",
                    application.JobApplicationId.ToString(),
                    $"Updated application status for '{application.JobPosting?.JobTitle}' to '{newStatus}'.");

                await _context.SaveChangesAsync();
            }
            // else: application is already terminal (Rejected/Withdrawn), or newStatus is illegal — silently ignore

            return RedirectToAction(nameof(Index), new { vacancyId, status, search });
        }

        // GET: JobApplications/DownloadResume/5
        public async Task<IActionResult> DownloadResume(int id)
        {
            JobApplication? application = await GetOwnedApplicationAsync(id);
            if (application == null) return NotFound();

            JobSeekerProfile? profile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == application.UserId);

            string? key = profile?.ResumeS3Key;
            if (string.IsNullOrEmpty(key))
            {
                return NotFound();
            }

            var credentials = new SessionAWSCredentials(
                _configuration["AWS:AccessKey"],
                _configuration["AWS:SecretKey"],
                _configuration["AWS:SessionToken"]);

            using var client = new AmazonS3Client(credentials, RegionEndpoint.USEast1);

            string url = client.GetPreSignedURL(new GetPreSignedUrlRequest
            {
                BucketName = _configuration["AWS:ResumeBucketName"],
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(5),
                Verb = HttpVerb.GET,
            });

            return Redirect(url);
        }

        private async Task<JobApplication?> GetOwnedApplicationAsync(int id)
        {
            string userId = _userManager.GetUserId(User)!;

            return await _context.JobApplications
                .Include(a => a.JobPosting)
                .FirstOrDefaultAsync(a => a.JobApplicationId == id
                    && a.JobPosting != null
                    && a.JobPosting.EmployerId == userId);
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
