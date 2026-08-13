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
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Text.Json;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "CareerAdvisor")]
    public class CareerResourcesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public CareerResourcesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        // GET: CareerResources — resources this advisor has authored
        public async Task<IActionResult> Index()
        {
            string userId = _userManager.GetUserId(User)!;

            List<CareerResource> resources = await _context.CareerResources
                .Where(r => r.CreatedByUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(resources);
        }

        // GET: CareerResources/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CareerResourceInput());
        }

        // POST: CareerResources/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CareerResourceInput input)
        {
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            string userId = _userManager.GetUserId(User)!;

            CareerResource resource = new CareerResource
            {
                Title = input.Title,
                Description = input.Description,
                ResourceType = input.ResourceType,
                ContentUrl = input.ContentUrl,
                RelatedCategory = input.RelatedCategory,
                RelatedSkill = input.RelatedSkill,
                CreatedByUserId = userId,
                IsPublished = input.IsPublished,
                CreatedAt = DateTime.UtcNow,
                PublishedAt = input.IsPublished ? DateTime.UtcNow : null
            };

            _context.CareerResources.Add(resource);
            await _context.SaveChangesAsync();
            // Step 1: INSERT happens here. resource.CareerResourceId is now a real number.

            AddUserActivity(
                "Create Career Resource",
                "CareerResource",
                resource.CareerResourceId.ToString(),
                $"Created career resource '{resource.Title}'.");

            await _context.SaveChangesAsync();

            if (input.AttachmentFile != null)
            {
                await UploadAttachmentAsync(resource, input.AttachmentFile);
                // Step 2: uses resource.CareerResourceId to build the S3 key, uploads the
                // file, and sets resource.AttachmentUrl / resource.AttachmentS3Key in memory.

                if (!ModelState.IsValid)
                {
                    // upload validation failed (file too big / wrong type) — re-show the form
                    return View(input);
                }

                await _context.SaveChangesAsync();
                // Step 3: UPDATE happens here, saving the AttachmentUrl/AttachmentS3Key Step 2 set.
            }

            if (resource.IsPublished)
            {
                await PublishNotificationAsync(resource);
            }

            TempData["SuccessMessage"] = "Career resource created successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task UploadAttachmentAsync(CareerResource resource, IFormFile attachmentFile)
        {
            if (attachmentFile.Length > 5_000_000)
            {
                ModelState.AddModelError("AttachmentFile", "Attachment must be 5 MB or smaller.");
                return;
            }

            string[] allowedTypes = { "application/pdf", "image/jpeg", "image/png" };
            if (!allowedTypes.Contains(attachmentFile.ContentType))
            {
                ModelState.AddModelError("AttachmentFile", "Only PDF, JPG, or PNG files are allowed.");
                return;
            }

            var credentials = new SessionAWSCredentials(
                _configuration["AWS:AccessKey"],
                _configuration["AWS:SecretKey"],
                _configuration["AWS:SessionToken"]);

            using var client = new AmazonS3Client(credentials, RegionEndpoint.USEast1);

            string bucketName = _configuration["AWS:ResourceAttachmentBucketName"]!;
            string key = $"career-resources/id-{resource.CareerResourceId}-{resource.Title}/{attachmentFile.FileName}";

            using var stream = attachmentFile.OpenReadStream();

            PutObjectRequest uploadRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = stream,
                CannedACL = S3CannedACL.PublicRead
            };

            await client.PutObjectAsync(uploadRequest);

            resource.AttachmentS3Key = key;
            resource.AttachmentUrl = $"https://{bucketName}.s3.amazonaws.com/{key}";
        }

        // GET: CareerResources/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            string userId = _userManager.GetUserId(User)!;

            CareerResource? resource = await _context.CareerResources
                .FirstOrDefaultAsync(r => r.CareerResourceId == id && r.CreatedByUserId == userId);

            if (resource == null)
            {
                return NotFound();
            }

            CareerResourceInput input = new CareerResourceInput
            {
                CareerResourceId = resource.CareerResourceId,
                Title = resource.Title,
                Description = resource.Description,
                ResourceType = resource.ResourceType,
                ContentUrl = resource.ContentUrl,
                RelatedCategory = resource.RelatedCategory,
                RelatedSkill = resource.RelatedSkill,
                IsPublished = resource.IsPublished,
                AttachmentUrl = resource.AttachmentUrl
            };

            return View(input);
        }

        // POST: CareerResources/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CareerResourceInput input)
        {
            string userId = _userManager.GetUserId(User)!;

            CareerResource? resource = await _context.CareerResources
                .FirstOrDefaultAsync(r => r.CareerResourceId == id && r.CreatedByUserId == userId);

            if (resource == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            bool wasPublished = resource.IsPublished;

            resource.Title = input.Title;
            resource.Description = input.Description;
            resource.ResourceType = input.ResourceType;
            resource.ContentUrl = input.ContentUrl;
            resource.RelatedCategory = input.RelatedCategory;
            resource.RelatedSkill = input.RelatedSkill;
            resource.IsPublished = input.IsPublished;

            if (!wasPublished && input.IsPublished)
            {
                resource.PublishedAt = DateTime.UtcNow;
            }

            AddUserActivity(
                "Update Career Resource",
                "CareerResource",
                resource.CareerResourceId.ToString(),
                $"Updated career resource '{resource.Title}'.");

            await _context.SaveChangesAsync();

            if (input.AttachmentFile != null)
            {
                await UploadAttachmentAsync(resource, input.AttachmentFile);
                if (!ModelState.IsValid)
                {
                    return View(input);
                }
                await _context.SaveChangesAsync();
            }

            if (!wasPublished && resource.IsPublished)
            {
                await PublishNotificationAsync(resource);
            }

            TempData["SuccessMessage"] = "Career resource updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: CareerResources/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            string userId = _userManager.GetUserId(User)!;

            CareerResource? resource = await _context.CareerResources
                .FirstOrDefaultAsync(r => r.CareerResourceId == id && r.CreatedByUserId == userId);

            if (resource == null)
            {
                return NotFound();
            }

            AddUserActivity(
                "Delete Career Resource",
                "CareerResource",
                resource.CareerResourceId.ToString(),
                $"Deleted career resource '{resource.Title}'.");

            _context.CareerResources.Remove(resource);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Career resource deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Only reaches job seekers this resource actually matches (same ResourceMatcher logic
        // used by JobSeeker/MyRecommendations). SNS delivers to a subscriber only if the
        // message's "jobseeker_id" attribute intersects with that subscriber's own FilterPolicy
        // (set at subscribe time in JobSeekerController.SubscribeToResourceUpdates) — this is
        // what makes a single shared topic behave like a per-recipient targeted notification.
        // No-ops if the topic ARN isn't configured, or if nobody matches, so it's safe to call unconditionally.
        private async Task PublishNotificationAsync(CareerResource resource)
        {
            string? topicArn = _configuration["AWS:CareerResourceTopicArn"];
            if (string.IsNullOrWhiteSpace(topicArn)) return;

            List<string> matchedJobSeekerIds = await GetMatchedJobSeekerIdsAsync(resource);
            if (matchedJobSeekerIds.Count == 0) return;

            var credentials = new SessionAWSCredentials(
                _configuration["AWS:AccessKey"],
                _configuration["AWS:SecretKey"],
                _configuration["AWS:SessionToken"]);

            using var snsClient = new AmazonSimpleNotificationServiceClient(
                credentials, RegionEndpoint.USEast1);

            var publishRequest = new PublishRequest
            {
                TopicArn = topicArn,
                Subject = $"New Career Resource: {resource.Title}",
                Message = $"New career resource published: {resource.Title}\n\n{resource.Description}",
                MessageAttributes = new Dictionary<string, MessageAttributeValue>
                {
                    ["jobseeker_id"] = new MessageAttributeValue
                    {
                        DataType = "String.Array",
                        StringValue = JsonSerializer.Serialize(matchedJobSeekerIds)
                    }
                }
            };

            await snsClient.PublishAsync(publishRequest);
        }

        // Runs the same category/field-of-study/applied-job-skill matching used on the job
        // seeker's "Recommended For You" page, but against every job seeker profile at once,
        // to find who this specific resource should notify.
        private async Task<List<string>> GetMatchedJobSeekerIdsAsync(CareerResource resource)
        {
            List<JobSeekerProfile> profiles = await _context.JobSeekerProfiles.ToListAsync();

            var applications = await _context.JobApplications
                .Include(a => a.JobPosting)
                .Where(a => a.JobPosting != null && a.JobPosting.RequiredSkills != null)
                .Select(a => new { a.UserId, a.JobPosting!.RequiredSkills })
                .ToListAsync();

            ILookup<string, string> skillsByUser = applications.ToLookup(a => a.UserId, a => a.RequiredSkills!);

            List<string> matchedIds = new();
            foreach (JobSeekerProfile profile in profiles)
            {
                List<string> appliedSkills = skillsByUser[profile.UserId]
                    .SelectMany(ResourceMatcher.SplitTags)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (ResourceMatcher.Evaluate(resource, profile, appliedSkills) != null)
                {
                    matchedIds.Add(profile.UserId);
                }
            }

            return matchedIds;
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
                    UserRole = "CareerAdvisor",
                    ActivityType = activityType,
                    EntityType = entityType,
                    EntityId = entityId,
                    Description = description,
                    CreatedAt = DateTime.UtcNow
                });
        }
    }
}
