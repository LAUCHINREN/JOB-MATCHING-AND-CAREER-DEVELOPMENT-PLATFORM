using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "JobSeeker")]
    public class JobSeekerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JobSeekerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Home()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            // =========================
            // PROFILE
            // =========================
            var profile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var userSkills = await _context.JobSeekerSkills
                .Where(s => s.UserId == user.Id)
                .ToListAsync();

            // =========================
            // PROFILE COMPLETION
            // =========================
            int completion = 0;

            if (profile != null)
            {
                int completedFields = 0;
                int totalFields = 9;

                if (!string.IsNullOrWhiteSpace(profile.FullName))
                    completedFields++;

                if (!string.IsNullOrWhiteSpace(profile.ContactNumber))
                    completedFields++;

                if (!string.IsNullOrWhiteSpace(profile.CareerObjective))
                    completedFields++;

                if (!string.IsNullOrWhiteSpace(profile.EducationLevel))
                    completedFields++;

                if (!string.IsNullOrWhiteSpace(profile.FieldOfStudy))
                    completedFields++;

                if (!string.IsNullOrWhiteSpace(profile.PreferredJobCategory))
                    completedFields++;

                if (!string.IsNullOrWhiteSpace(profile.PreferredLocation))
                    completedFields++;

                if (profile.ExpectedSalary.HasValue)
                    completedFields++;

                if (userSkills.Any())
                    completedFields++;

                completion = (int)Math.Round(
                    (double)completedFields / totalFields * 100
                );
            }

            ViewBag.ProfileCompletion = completion;


            // =========================
            // APPLICATION COUNT
            // =========================
            var applicationCount = await _context.JobApplications
                .CountAsync(a => a.UserId == user.Id);

            ViewBag.ApplicationCount = applicationCount;


            // =========================
            // ACTIVE JOBS
            // =========================
            var jobs = await _context.JobPostings
                .Include(j => j.Employer)
                .Include(j => j.JobCategory)
                .Where(j => j.ModerationStatus == "Approved")
                .ToListAsync();


            // =========================
            // RECOMMENDATION LOGIC
            // =========================
            var skillNames = userSkills
                .Select(s => s.SkillName.Trim().ToLower())
                .ToList();

            var recommendations = jobs
                .Select(job =>
                {
                    int score = 0;

                    // Preferred category = 40%
                    if (profile != null &&
                        !string.IsNullOrWhiteSpace(profile.PreferredJobCategory) &&
                        job.JobCategory != null &&
                        job.JobCategory.CategoryName.Equals(
                            profile.PreferredJobCategory,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        score += 40;
                    }

                    // Preferred location = 20%
                    if (profile != null &&
                        !string.IsNullOrWhiteSpace(profile.PreferredLocation) &&
                        !string.IsNullOrWhiteSpace(job.Location) &&
                        job.Location.Contains(
                            profile.PreferredLocation,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        score += 20;
                    }

                    // Skills = 40%
                    if (!string.IsNullOrWhiteSpace(job.RequiredSkills))
                    {
                        var requiredSkills = job.RequiredSkills
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().ToLower())
                            .ToList();

                        if (requiredSkills.Any())
                        {
                            int matchedSkills = requiredSkills
                                .Count(required =>
                                    skillNames.Any(userSkill =>
                                        userSkill.Equals(
                                            required,
                                            StringComparison.OrdinalIgnoreCase)));

                            int skillScore = (int)Math.Round(
                                (double)matchedSkills /
                                requiredSkills.Count * 40
                            );

                            score += skillScore;
                        }
                    }

                    return new
                    {
                        Job = job,
                        Score = score
                    };
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ToList();


            //ViewBag.RecommendedJobCount = recommendations.Count;
            var totalJobCount = await _context.JobPostings
                .CountAsync(j => j.ModerationStatus == "Approved");

            ViewBag.TotalJobCount = totalJobCount;

            // Only show top 3 on dashboard
            ViewBag.RecommendedJobs = recommendations
                .Take(3)
                .Select(x => x.Job)
                .ToList();

            ViewBag.RecommendationScores = recommendations
                .Take(3)
                .ToDictionary(
                    x => x.Job.JobId,
                    x => x.Score
                );

            // Assessment Count
            var assessmentCount = await _context.AssessmentResults
                .CountAsync(r => r.UserId == user.Id);

            ViewBag.AssessmentCount = assessmentCount;

            return View();
        }


        // PROFILE CRUD
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var profile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                return RedirectToAction(nameof(CreateProfile));
            }

            var skills = await _context.JobSeekerSkills
                .Where(s => s.UserId == user.Id)
                .OrderBy(s => s.SkillName)
                .ToListAsync();

            ViewBag.Skills = skills;

            return View(profile);
        }


        [HttpGet]
        public async Task<IActionResult> CreateProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var existingProfile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (existingProfile != null)
            {
                return RedirectToAction(nameof(Profile));
            }

            var profile = new JobSeekerProfile
            {
                FullName = user.FullName
            };

            return View(profile);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfile(JobSeekerProfile profile)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var existingProfile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (existingProfile != null)
            {
                return RedirectToAction(nameof(Profile));
            }

            profile.UserId = user.Id;

            if (!ModelState.IsValid)
            {
                return View(profile);
            }

            _context.JobSeekerProfiles.Add(profile);

            AddUserActivity(
                user.Id,
                "Create Profile",
                "JobSeekerProfile",
                user.Id,
                "Created job seeker profile.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile created successfully.";

            return RedirectToAction(nameof(Profile));
        }


        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var profile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                return RedirectToAction(nameof(CreateProfile));
            }

            return View(profile);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(JobSeekerProfile profile)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var existingProfile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (existingProfile == null)
            {
                return RedirectToAction(nameof(CreateProfile));
            }

            if (!ModelState.IsValid)
            {
                return View(profile);
            }

            existingProfile.FullName = profile.FullName;
            existingProfile.ContactNumber = profile.ContactNumber;
            existingProfile.CareerObjective = profile.CareerObjective;
            existingProfile.EducationLevel = profile.EducationLevel;
            existingProfile.FieldOfStudy = profile.FieldOfStudy;
            existingProfile.ExperienceYears = profile.ExperienceYears;
            existingProfile.PreferredJobCategory = profile.PreferredJobCategory;
            existingProfile.PreferredLocation = profile.PreferredLocation;
            existingProfile.ExpectedSalary = profile.ExpectedSalary;

            AddUserActivity(
                user.Id,
                "Update Profile",
                "JobSeekerProfile",
                user.Id,
                "Updated job seeker profile.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully.";

            return RedirectToAction(nameof(Profile));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var profile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "Profile not found.";
                return RedirectToAction(nameof(Home));
            }

            AddUserActivity(
                user.Id,
                "Delete Profile",
                "JobSeekerProfile",
                user.Id,
                "Deleted job seeker profile.");

            _context.JobSeekerProfiles.Remove(profile);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile deleted successfully.";

            return RedirectToAction(nameof(Home));
        }

        // =========================
        // SKILL CRUD
        // =========================

        [HttpGet]
        public IActionResult AddSkill()
        {
            return View(new AddSkillsViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSkill(AddSkillsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int addedSkillCount = 0;

            foreach (var item in model.Skills)
            {
                if (string.IsNullOrWhiteSpace(item.SkillName))
                {
                    continue;
                }

                var alreadyExists = await _context.JobSeekerSkills
                    .AnyAsync(s =>
                        s.UserId == user.Id &&
                        s.SkillName.ToLower() == item.SkillName.Trim().ToLower());

                if (alreadyExists)
                {
                    continue;
                }

                var skill = new JobSeekerSkill
                {
                    UserId = user.Id,
                    SkillName = item.SkillName.Trim(),
                    ProficiencyLevel = item.ProficiencyLevel
                };

                _context.JobSeekerSkills.Add(skill);
                addedSkillCount++;
            }

            if (addedSkillCount > 0)
            {
                AddUserActivity(
                    user.Id,
                    "Add Skills",
                    "JobSeekerSkill",
                    user.Id,
                    $"Added {addedSkillCount} skill(s) to job seeker profile.");
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Skills added successfully.";

            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> EditSkill(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var skill = await _context.JobSeekerSkills
                .FirstOrDefaultAsync(s =>
                    s.JobSeekerSkillId == id &&
                    s.UserId == user.Id);

            if (skill == null)
            {
                return NotFound();
            }

            return View(skill);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSkill(JobSeekerSkill skill)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var existingSkill = await _context.JobSeekerSkills
                .FirstOrDefaultAsync(s =>
                    s.JobSeekerSkillId == skill.JobSeekerSkillId &&
                    s.UserId == user.Id);

            if (existingSkill == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(skill);
            }

            existingSkill.SkillName = skill.SkillName;
            existingSkill.ProficiencyLevel = skill.ProficiencyLevel;

            AddUserActivity(
                user.Id,
                "Update Skill",
                "JobSeekerSkill",
                existingSkill.JobSeekerSkillId.ToString(),
                $"Updated skill '{existingSkill.SkillName}'.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Skill updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSkill(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var skill = await _context.JobSeekerSkills
                .FirstOrDefaultAsync(s =>
                    s.JobSeekerSkillId == id &&
                    s.UserId == user.Id);

            if (skill == null)
            {
                TempData["ErrorMessage"] = "Skill not found.";

                return RedirectToAction(nameof(Profile));
            }

            AddUserActivity(
                user.Id,
                "Delete Skill",
                "JobSeekerSkill",
                skill.JobSeekerSkillId.ToString(),
                $"Deleted skill '{skill.SkillName}'.");

            _context.JobSeekerSkills.Remove(skill);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Skill deleted successfully.";

            return RedirectToAction(nameof(Profile));
        }

        // =========================
        // JOBS
        // =========================

        public async Task<IActionResult> Jobs(string? searchString)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var jobsQuery = _context.JobPostings
                .Include(j => j.Employer)
                .Include(j => j.JobCategory)
                .Where(j => j.ModerationStatus == "Approved")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Trim();

                jobsQuery = jobsQuery.Where(j =>
                    j.JobTitle.Contains(searchString) ||
                    (j.Employer != null && j.Employer.FullName.Contains(searchString)) ||
                    (j.JobCategory != null && j.JobCategory.CategoryName.Contains(searchString)) ||
                    j.Location.Contains(searchString));
            }

            var jobs = await jobsQuery
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            var appliedJobIds = await _context.JobApplications
                .Where(a => a.UserId == user.Id)
                .Select(a => a.JobId)
                .ToListAsync();

            ViewBag.AppliedJobIds = appliedJobIds;
            ViewBag.SearchString = searchString;

            return View(jobs);
        }

        public async Task<IActionResult> JobDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var job = await _context.JobPostings
                .Include(j => j.Employer)
                .Include(j => j.JobCategory)
                .FirstOrDefaultAsync(j =>
                    j.JobId == id &&
                    j.ModerationStatus == "Approved");

            if (job == null)
            {
                return NotFound();
            }

            var alreadyApplied = await _context.JobApplications
                .AnyAsync(a =>
                    a.UserId == user.Id &&
                    a.JobId == id);

            ViewBag.AlreadyApplied = alreadyApplied;

            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyJob(JobApplication application)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var job = await _context.JobPostings
                .FirstOrDefaultAsync(j =>
                    j.JobId == application.JobId &&
                    j.ModerationStatus == "Approved");

            if (job == null)
            {
                return NotFound();
            }

            var alreadyApplied = await _context.JobApplications
                .AnyAsync(a =>
                    a.UserId == user.Id &&
                    a.JobId == application.JobId);

            if (alreadyApplied)
            {
                TempData["ErrorMessage"] =
                    "You have already applied for this job.";

                return RedirectToAction(
                    nameof(JobDetails),
                    new { id = application.JobId }
                );
            }

            application.UserId = user.Id;
            application.Status = "Submitted";
            application.AppliedDate = DateTime.Now;

            ModelState.Remove(nameof(JobApplication.UserId));
            ModelState.Remove(nameof(JobApplication.Status));
            ModelState.Remove(nameof(JobApplication.AppliedDate));

            if (!ModelState.IsValid)
            {
                ViewBag.Job = job;
                return View(application);
            }

            // Save first so JobApplicationId is generated
            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();

            AddUserActivity(
                user.Id,
                "Apply Job",
                "JobApplication",
                application.JobApplicationId.ToString(),
                $"Submitted an application for job '{job.JobTitle}'.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Application submitted successfully.";

            return RedirectToAction(nameof(Applications));
        }

        [HttpGet]
        public async Task<IActionResult> ApplyJob(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var job = await _context.JobPostings
                .FirstOrDefaultAsync(j =>
                    j.JobId == id &&
                    j.ModerationStatus == "Approved");

            if (job == null)
            {
                return NotFound();
            }

            var alreadyApplied = await _context.JobApplications
                .AnyAsync(a =>
                    a.UserId == user.Id &&
                    a.JobId == id);

            if (alreadyApplied)
            {
                TempData["ErrorMessage"] =
                    "You have already applied for this job.";

                return RedirectToAction(
                    nameof(JobDetails),
                    new { id }
                );
            }

            ViewBag.Job = job;

            var application = new JobApplication
            {
                JobId = id
            };

            return View(application);
        }

        public async Task<IActionResult> RecommendedJobs()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var profile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var userSkills = await _context.JobSeekerSkills
                .Where(s => s.UserId == user.Id)
                .Select(s => s.SkillName.ToLower())
                .ToListAsync();

            var jobs = await _context.JobPostings
                .Include(j => j.Employer)
                .Include(j => j.JobCategory)
                .Where(j => j.ModerationStatus == "Approved")
                .ToListAsync();

            var recommendedJobs = jobs
                .Select(job =>
                {
                    int score = 0;

                    // Category match
                    if (profile != null &&
                        !string.IsNullOrWhiteSpace(profile.PreferredJobCategory) &&
                        job.JobCategory != null &&
                        job.JobCategory.CategoryName.Equals(
                            profile.PreferredJobCategory,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        score += 40;
                    }

                    // Location match
                    if (profile != null &&
                        !string.IsNullOrWhiteSpace(profile.PreferredLocation) &&
                        job.Location.Contains(
                            profile.PreferredLocation,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        score += 20;
                    }

                    // Skill match
                    if (!string.IsNullOrWhiteSpace(job.RequiredSkills))
                    {
                        var requiredSkills = job.RequiredSkills
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().ToLower())
                            .ToList();

                        if (requiredSkills.Count > 0)
                        {
                            var matchedSkills = requiredSkills
                                .Count(required =>
                                    userSkills.Any(userSkill =>
                                        userSkill.Equals(
                                            required,
                                            StringComparison.OrdinalIgnoreCase)));

                            var skillScore = (int)Math.Round(
                                (double)matchedSkills /
                                requiredSkills.Count * 40);

                            score += skillScore;
                        }
                    }

                    return new
                    {
                        Job = job,
                        Score = score
                    };
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ToList();

            ViewBag.RecommendationScores =
                recommendedJobs.ToDictionary(
                    x => x.Job.JobId,
                    x => x.Score);

            return View(
                recommendedJobs
                    .Select(x => x.Job)
                    .ToList()
            );
        }

        // =========================
        // APPLICATIONS
        // =========================

        public async Task<IActionResult> Applications()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var applications = await _context.JobApplications
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            ViewBag.Jobs = await _context.JobPostings
                .ToDictionaryAsync(j => j.JobId);

            return View(applications);
        }

        [HttpGet]
        public async Task<IActionResult> EditApplication(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a =>
                    a.JobApplicationId == id &&
                    a.UserId == user.Id);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != "Submitted")
            {
                TempData["ErrorMessage"] =
                    "This application can no longer be edited.";

                return RedirectToAction(nameof(Applications));
            }

            var job = await _context.JobPostings
                .FirstOrDefaultAsync(j =>
                    j.JobId == application.JobId);

            ViewBag.Job = job;

            return View(application);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditApplication(JobApplication application)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var existingApplication = await _context.JobApplications
                .FirstOrDefaultAsync(a =>
                    a.JobApplicationId == application.JobApplicationId &&
                    a.UserId == user.Id);

            if (existingApplication == null)
            {
                return NotFound();
            }

            if (existingApplication.Status != "Submitted")
            {
                TempData["ErrorMessage"] =
                    "This application can no longer be edited.";

                return RedirectToAction(nameof(Applications));
            }

            ModelState.Remove(nameof(JobApplication.UserId));
            ModelState.Remove(nameof(JobApplication.Status));
            ModelState.Remove(nameof(JobApplication.AppliedDate));
            ModelState.Remove(nameof(JobApplication.JobId));

            if (!ModelState.IsValid)
            {
                ViewBag.Job = await _context.JobPostings
                    .FirstOrDefaultAsync(j =>
                        j.JobId == existingApplication.JobId);

                return View(application);
            }

            existingApplication.CoverMessage =
                application.CoverMessage?.Trim();

            AddUserActivity(
                user.Id,
                "Update Job Application",
                "JobApplication",
                existingApplication.JobApplicationId.ToString(),
                "Updated a submitted job application.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Application updated successfully.";

            return RedirectToAction(nameof(Applications));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawApplication(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a =>
                    a.JobApplicationId == id &&
                    a.UserId == user.Id);

            if (application == null)
            {
                return NotFound();
            }

            if (application.Status != "Submitted")
            {
                TempData["ErrorMessage"] =
                    "This application can no longer be withdrawn.";

                return RedirectToAction(nameof(Applications));
            }

            AddUserActivity(
                user.Id,
                "Withdraw Job Application",
                "JobApplication",
                application.JobApplicationId.ToString(),
                "Withdrew a submitted job application.");

            _context.JobApplications.Remove(application);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Application withdrawn successfully.";

            return RedirectToAction(nameof(Applications));
        }

        // =========================
        // SKILLS ASSESSMENTS
        // =========================

        public async Task<IActionResult> Assessments()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var assessments = await _context.SkillAssessments
                .Where(a => a.IsActive)
                .OrderBy(a => a.Title)
                .ToListAsync();

            var results = await _context.AssessmentResults
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CompletedDate)
                .ToListAsync();

            ViewBag.Results = results;

            return View(assessments);
        }

        [HttpGet]
        public async Task<IActionResult> StartAssessment(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var assessment = await _context.SkillAssessments
                .FirstOrDefaultAsync(a =>
                    a.SkillAssessmentId == id &&
                    a.IsActive);

            if (assessment == null)
            {
                return NotFound();
            }

            var questions = await _context.AssessmentQuestions
                .Where(q => q.SkillAssessmentId == id)
                .OrderBy(q => q.AssessmentQuestionId)
                .ToListAsync();

            if (!questions.Any())
            {
                TempData["ErrorMessage"] =
                    "This assessment does not contain any questions.";

                return RedirectToAction(nameof(Assessments));
            }

            var model = new TakeAssessmentViewModel
            {
                SkillAssessmentId = assessment.SkillAssessmentId,
                Title = assessment.Title,
                SkillName = assessment.SkillName,
                PassingScore = assessment.PassingScore,
                DurationMinutes = assessment.DurationMinutes,

                Questions = questions.Select(q =>
                    new AssessmentAnswerViewModel
                    {
                        AssessmentQuestionId = q.AssessmentQuestionId,
                        QuestionText = q.QuestionText,
                        OptionA = q.OptionA,
                        OptionB = q.OptionB,
                        OptionC = q.OptionC,
                        OptionD = q.OptionD
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAssessment(
    TakeAssessmentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var assessment = await _context.SkillAssessments
                .FirstOrDefaultAsync(a =>
                    a.SkillAssessmentId == model.SkillAssessmentId &&
                    a.IsActive);

            if (assessment == null)
            {
                return NotFound();
            }

            var questions = await _context.AssessmentQuestions
                .Where(q =>
                    q.SkillAssessmentId == model.SkillAssessmentId)
                .ToListAsync();

            if (!questions.Any())
            {
                return RedirectToAction(nameof(Assessments));
            }

            int correctAnswers = 0;

            foreach (var question in questions)
            {
                var submittedAnswer = model.Questions
                    .FirstOrDefault(q =>
                        q.AssessmentQuestionId ==
                        question.AssessmentQuestionId);

                if (submittedAnswer != null &&
                    !string.IsNullOrWhiteSpace(
                        submittedAnswer.SelectedAnswer) &&
                    submittedAnswer.SelectedAnswer.Equals(
                        question.CorrectAnswer,
                        StringComparison.OrdinalIgnoreCase))
                {
                    correctAnswers++;
                }
            }

            int totalQuestions = questions.Count;

            int score = (int)Math.Round(
                (double)correctAnswers /
                totalQuestions * 100
            );

            bool isPassed =
                score >= assessment.PassingScore;

            var result = new AssessmentResult
            {
                UserId = user.Id,
                SkillAssessmentId =
                    assessment.SkillAssessmentId,

                CorrectAnswers = correctAnswers,
                TotalQuestions = totalQuestions,

                Score = score,
                IsPassed = isPassed,

                CompletedDate = DateTime.Now
            };

            // Save first so AssessmentResultId is generated
            _context.AssessmentResults.Add(result);

            await _context.SaveChangesAsync();

            AddUserActivity(
                user.Id,
                "Complete Assessment",
                "SkillAssessment",
                result.AssessmentResultId.ToString(),
                $"Completed assessment '{assessment.Title}'.");

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(AssessmentResult),
                new { id = result.AssessmentResultId }
            );
        }

        [HttpGet]
        public async Task<IActionResult> AssessmentResult(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var result = await _context.AssessmentResults
                .FirstOrDefaultAsync(r =>
                    r.AssessmentResultId == id &&
                    r.UserId == user.Id);

            if (result == null)
            {
                return NotFound();
            }

            var assessment = await _context.SkillAssessments
                .FirstOrDefaultAsync(a =>
                    a.SkillAssessmentId ==
                    result.SkillAssessmentId);

            ViewBag.Assessment = assessment;

            return View(result);
        }

        // =========================
        // CAREER ADVISOR RESOURCES (read-only)
        // =========================

        public async Task<IActionResult> CareerResources()
        {
            var resources = await _context.CareerResources
                .Where(r => r.IsPublished)
                .OrderByDescending(r => r.PublishedAt)
                .ToListAsync();

            return View(resources);
        }

        public async Task<IActionResult> MyRecommendations()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var profile = await _context.JobSeekerProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var appliedSkillLists = await _context.JobApplications
                .Include(a => a.JobPosting)
                .Where(a => a.UserId == user.Id && a.JobPosting != null && a.JobPosting.RequiredSkills != null)
                .Select(a => a.JobPosting!.RequiredSkills!)
                .ToListAsync();

            var appliedSkills = appliedSkillLists
                .SelectMany(ResourceMatcher.SplitTags)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var resources = await _context.CareerResources
                .Where(r => r.IsPublished)
                .ToListAsync();

            var matches = resources
                .Select(r => ResourceMatcher.Evaluate(r, profile, appliedSkills))
                .Where(m => m != null)
                .Select(m => m!)
                .ToList();

            return View(matches);
        }

        public async Task<IActionResult> MyCounsellingSessions()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var sessions = await _context.CounsellingSessions
                .Where(s => s.JobSeekerUserId == user.Id)
                .OrderByDescending(s => s.ScheduledAt)
                .ToListAsync();

            return View(sessions);
        }

        [HttpGet]
        public IActionResult RequestCounsellingSession()
        {
            return View(new CounsellingSessionRequestInput());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestCounsellingSession(CounsellingSessionRequestInput input)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            if (input.ScheduledAt <= DateTime.Now)
            {
                ModelState.AddModelError(nameof(input.ScheduledAt), "Please choose a future date and time.");
            }

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var session = new CounsellingSession
            {
                JobSeekerUserId = user.Id,
                ScheduledAt = input.ScheduledAt,
                DurationMinutes = input.DurationMinutes,
                Notes = input.Notes,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            // Save first so CounsellingSessionId is generated
            _context.CounsellingSessions.Add(session);
            await _context.SaveChangesAsync();

            AddUserActivity(
                user.Id,
                "Request Counselling Session",
                "CounsellingSession",
                session.CounsellingSessionId.ToString(),
                "Requested a counselling session.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your counselling session request has been submitted. A career advisor will review it soon.";
            return RedirectToAction(nameof(MyCounsellingSessions));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WithdrawCounsellingSession(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            var session = await _context.CounsellingSessions
                .FirstOrDefaultAsync(s => s.CounsellingSessionId == id && s.JobSeekerUserId == user.Id);

            if (session == null)
            {
                return NotFound();
            }

            if (session.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Only pending requests can be withdrawn.";
                return RedirectToAction(nameof(MyCounsellingSessions));
            }

            AddUserActivity(
                user.Id,
                "Withdraw Counselling Session",
                "CounsellingSession",
                session.CounsellingSessionId.ToString(),
                "Withdrew a counselling session request.");

            _context.CounsellingSessions.Remove(session);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your session request has been withdrawn.";
            return RedirectToAction(nameof(MyCounsellingSessions));
        }

        // =========================
        // USER ACTIVITY LOG
        // =========================
        private void AddUserActivity(
            string userId,
            string activityType,
            string? entityType,
            string? entityId,
            string? description)
        {
            _context.UserActivityLogs.Add(
                new UserActivityLog
                {
                    UserId = userId,
                    UserRole = "JobSeeker",
                    ActivityType = activityType,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                });
        }
    }
}
