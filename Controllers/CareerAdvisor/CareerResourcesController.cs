using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            AddUserActivity(
                "Create Career Resource",
                "CareerResource",
                resource.CareerResourceId.ToString(),
                $"Created career resource '{resource.Title}'.");

            await _context.SaveChangesAsync();

            // Future cloud step (held off for now): broadcast a notification to subscribed
            // job seekers via AWS SNS when a resource is published. See the commented-out
            // PublishNotificationAsync() helper at the bottom of this file for the ready-to-use
            // implementation, following the same AmazonSimpleNotificationServiceClient pattern
            // used elsewhere in this course (Subscribe/Publish against an SNS Topic ARN).
            // if (resource.IsPublished) { await PublishNotificationAsync(resource); }

            TempData["SuccessMessage"] = "Career resource created successfully.";
            return RedirectToAction(nameof(Index));
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
                IsPublished = resource.IsPublished
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
                // if (resource.IsPublished) { await PublishNotificationAsync(resource); }
            }

            AddUserActivity(
                "Update Career Resource",
                "CareerResource",
                resource.CareerResourceId.ToString(),
                $"Updated career resource '{resource.Title}'.");

            await _context.SaveChangesAsync();

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

        // ---------------------------------------------------------------------------
        // FUTURE CLOUD IMPLEMENTATION (held off for now) — AWS SNS broadcast on publish
        // ---------------------------------------------------------------------------
        // Mirrors Lab 9.1's "Integrate AWS SNS in ASP.NET Core for broadcasting the message":
        // create an SNS Topic in the AWS Console, let job seekers subscribe their email to it,
        // then call PublishAsync whenever a new resource goes live. Left commented out —
        // requires AWSSDK.SimpleNotificationService (already referenced) plus a real Topic ARN
        // and credentials in "AWS:CareerResourceTopicArn" once cloud deployment resumes.
        //
        // private async Task PublishNotificationAsync(CareerResource resource)
        // {
        //     string? topicArn = _configuration["AWS:CareerResourceTopicArn"];
        //     if (string.IsNullOrWhiteSpace(topicArn)) return;
        //
        //     var credentials = new Amazon.Runtime.SessionAWSCredentials(
        //         _configuration["AWS:AccessKey"],
        //         _configuration["AWS:SecretKey"],
        //         _configuration["AWS:SessionToken"]);
        //
        //     using var snsClient = new Amazon.SimpleNotificationService.AmazonSimpleNotificationServiceClient(
        //         credentials, Amazon.RegionEndpoint.USEast1);
        //
        //     var publishRequest = new Amazon.SimpleNotificationService.Model.PublishRequest(
        //         topicArn, $"New career resource published: {resource.Title}");
        //
        //     await snsClient.PublishAsync(publishRequest);
        // }

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
