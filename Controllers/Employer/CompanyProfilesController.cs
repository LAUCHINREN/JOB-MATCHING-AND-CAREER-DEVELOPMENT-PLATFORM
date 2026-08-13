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
    public class CompanyProfilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IConfiguration _configuration;

        public CompanyProfilesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        // GET: CompanyProfiles
        // Not a list — an employer has at most one profile.
        public async Task<IActionResult> Index()
        {
            string userId = _userManager.GetUserId(User)!;

            CompanyProfile? profile = await _context.CompanyProfileTable
                .FirstOrDefaultAsync(c => c.UserId == userId);

            return View(profile); // null -> view shows empty state + Create button
        }

        // GET: CompanyProfiles/AddData
        public async Task<IActionResult> AddData()
        {
            string userId = _userManager.GetUserId(User)!;

            bool alreadyExists = await _context.CompanyProfileTable.AnyAsync(c => c.UserId == userId);
            if (alreadyExists)
            {
                return RedirectToAction(nameof(Index)); // one profile per employer
            }

            return View();
        }

        // POST: CompanyProfiles/AddData
        [HttpPost]
        public async Task<IActionResult> AddData(CompanyProfileInput input)
        {
            string userId = _userManager.GetUserId(User)!;

            if (ModelState.IsValid)
            {
                CompanyProfile profile = new CompanyProfile
                {
                    UserId = userId,
                    CompanyName = input.CompanyName,
                    Industry = input.Industry,
                    CompanySize = input.CompanySize,
                    Website = input.Website,
                    Description = input.Description,
                    Address = input.Address,
                    ContactEmail = input.ContactEmail,
                    ContactNumber = input.ContactNumber,
                    CreatedDate = DateTime.Now
                };

                _context.Add(profile);
                await _context.SaveChangesAsync();
                // Step 1: INSERT happens here. profile.CompanyProfileId is now a real number.

                AddUserActivity(
                    "Create Company Profile",
                    "CompanyProfile",
                    profile.CompanyProfileId.ToString(),
                    $"Created company profile '{profile.CompanyName}'.");

                await _context.SaveChangesAsync();

                if (input.LogoFile != null)
                {
                    await UploadLogoAsync(profile, input.LogoFile);
                    // Step 2: uses profile.CompanyProfileId to build the S3 key,
                    // uploads the file, and sets profile.LogoUrl / profile.LogoS3Key in memory.

                    if (!ModelState.IsValid)
                    {
                        // upload validation failed (file too big / wrong type) — re-show the form
                        return View(input);
                    }

                    await _context.SaveChangesAsync();
                    // Step 3: UPDATE happens here, saving the LogoUrl/LogoS3Key that Step 2 set.
                }

                return RedirectToAction(nameof(Index));
            }

            return View(input);
        }

        private async Task UploadLogoAsync(CompanyProfile profile, IFormFile logoFile)
        {
            if (logoFile.Length > 1_000_000)
            {
                ModelState.AddModelError("LogoFile", "Logo must be 1 MB or smaller.");
                return;
            }

            string[] allowedTypes = { "image/jpeg", "image/png" };
            if (!allowedTypes.Contains(logoFile.ContentType))
            {
                ModelState.AddModelError("LogoFile", "Only JPG, PNG images are allowed.");
                return;
            }

            var credentials = new SessionAWSCredentials(
                _configuration["AWS:AccessKey"],
                _configuration["AWS:SecretKey"],
                _configuration["AWS:SessionToken"]);

            using var client = new AmazonS3Client(credentials, RegionEndpoint.USEast1);

            string bucketName = _configuration["AWS:LogoBucketName"]!;
            string key = $"logos/id-{profile.CompanyProfileId}-company-{profile.CompanyName}/{logoFile.FileName}";

            using var stream = logoFile.OpenReadStream();

            PutObjectRequest uploadRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = stream,
                CannedACL = S3CannedACL.PublicRead
            };

            await client.PutObjectAsync(uploadRequest);

            profile.LogoS3Key = key;
            profile.LogoUrl = $"https://{bucketName}.s3.amazonaws.com/{key}";
        }

        // GET: CompanyProfiles/EditCompany/5
        public async Task<IActionResult> EditCompany(int id)
        {
            string userId = _userManager.GetUserId(User)!;

            CompanyProfile? profile = await _context.CompanyProfileTable
                .FirstOrDefaultAsync(c => c.CompanyProfileId == id && c.UserId == userId);

            if (profile == null)
            {
                return NotFound(); // also blocks employer A editing employer B's profile
            }

            CompanyProfileInput input = new CompanyProfileInput
            {
                CompanyProfileId = profile.CompanyProfileId,
                CompanyName = profile.CompanyName,
                Industry = profile.Industry,
                CompanySize = profile.CompanySize,
                Website = profile.Website,
                Description = profile.Description,
                Address = profile.Address,
                ContactEmail = profile.ContactEmail,
                ContactNumber = profile.ContactNumber,
                LogoUrl = profile.LogoUrl,
                LogoS3Key = profile.LogoS3Key
            };

            return View(input);
        }

        // POST: CompanyProfiles/EditCompany/5
        [HttpPost]
        public async Task<IActionResult> EditCompany(int id, CompanyProfileInput input)
        {
            string userId = _userManager.GetUserId(User)!;

            CompanyProfile? profile = await _context.CompanyProfileTable
                .FirstOrDefaultAsync(c => c.CompanyProfileId == id && c.UserId == userId);

            if (profile == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                profile.CompanyName = input.CompanyName;
                profile.Industry = input.Industry;
                profile.CompanySize = input.CompanySize;
                profile.Website = input.Website;
                profile.Description = input.Description;
                profile.Address = input.Address;
                profile.ContactEmail = input.ContactEmail;
                profile.ContactNumber = input.ContactNumber;

                AddUserActivity(
                    "Update Company Profile",
                    "CompanyProfile",
                    profile.CompanyProfileId.ToString(),
                    $"Updated company profile '{profile.CompanyName}'.");

                await _context.SaveChangesAsync();

                if (input.LogoFile != null)
                {
                    await UploadLogoAsync(profile, input.LogoFile);
                    if (!ModelState.IsValid)
                    {
                        return View(input);
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            return View(input);
        }

        // POST: CompanyProfiles/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            string userId = _userManager.GetUserId(User)!;

            CompanyProfile? profile = await _context.CompanyProfileTable
                .FirstOrDefaultAsync(c => c.CompanyProfileId == id && c.UserId == userId);

            if (profile == null)
            {
                return NotFound();
            }

            AddUserActivity(
                "Delete Company Profile",
                "CompanyProfile",
                profile.CompanyProfileId.ToString(),
                $"Deleted company profile '{profile.CompanyName}'.");

            _context.CompanyProfileTable.Remove(profile);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), "CompanyProfiles");
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
