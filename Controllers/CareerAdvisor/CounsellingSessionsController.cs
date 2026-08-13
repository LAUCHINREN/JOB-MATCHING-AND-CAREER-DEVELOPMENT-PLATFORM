using JobCareerPlatform.Data;
using JobCareerPlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobCareerPlatform.Controllers
{
    [Authorize(Roles = "CareerAdvisor")]
    public class CounsellingSessionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CounsellingSessionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: CounsellingSessions?tab=Pending
        // "Pending" is a shared queue — every job seeker's request, unclaimed by any advisor.
        // Every other tab only shows sessions THIS advisor has approved/rejected.
        public async Task<IActionResult> Index(string tab = "Pending")
        {
            string userId = _userManager.GetUserId(User)!;

            IQueryable<CounsellingSession> query = tab switch
            {
                "Approved" => _context.CounsellingSessions.Where(s => s.CareerAdvisorUserId == userId && s.Status == "Approved"),
                "Completed" => _context.CounsellingSessions.Where(s => s.CareerAdvisorUserId == userId && s.Status == "Completed"),
                "Rejected" => _context.CounsellingSessions.Where(s => s.CareerAdvisorUserId == userId && s.Status == "Rejected"),
                "Cancelled" => _context.CounsellingSessions.Where(s => s.CareerAdvisorUserId == userId && s.Status == "Cancelled"),
                _ => _context.CounsellingSessions.Where(s => s.Status == "Pending")
            };

            List<CounsellingSession> sessions = await query
                .OrderBy(s => s.ScheduledAt)
                .ToListAsync();

            List<string> jobSeekerIds = sessions.Select(s => s.JobSeekerUserId).Distinct().ToList();

            Dictionary<string, JobSeekerProfile> profiles = await _context.JobSeekerProfiles
                .Where(p => jobSeekerIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId);

            Dictionary<string, string> names = await _userManager.Users
                .Where(u => jobSeekerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            ViewBag.Profiles = profiles;
            ViewBag.Names = names;
            ViewBag.CurrentTab = tab;
            return View(sessions);
        }

        // POST: CounsellingSessions/Approve/5 — claims a pending request for this advisor.
        // A meeting link is mandatory: the job seeker needs somewhere to actually join the session.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? meetingLink)
        {
            CounsellingSession? session = await _context.CounsellingSessions
                .FirstOrDefaultAsync(s => s.CounsellingSessionId == id && s.Status == "Pending");

            if (session == null) return NotFound();

            bool isValidUrl = !string.IsNullOrWhiteSpace(meetingLink)
                && Uri.TryCreate(meetingLink, UriKind.Absolute, out Uri? parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);

            if (!isValidUrl)
            {
                TempData["ErrorMessage"] = "Please provide a valid online meeting link (e.g. https://meet.google.com/xyz) before approving.";
                return RedirectToAction(nameof(Index));
            }

            session.CareerAdvisorUserId = _userManager.GetUserId(User)!;
            session.Status = "Approved";
            session.MeetingLink = meetingLink;

            AddUserActivity(
                "Approve Counselling Session",
                "CounsellingSession",
                session.CounsellingSessionId.ToString(),
                "Approved a counselling session request.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Session request approved.";
            return RedirectToAction(nameof(Index));
        }

        // POST: CounsellingSessions/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? rejectionNote)
        {
            CounsellingSession? session = await _context.CounsellingSessions
                .FirstOrDefaultAsync(s => s.CounsellingSessionId == id && s.Status == "Pending");

            if (session == null) return NotFound();

            if (string.IsNullOrWhiteSpace(rejectionNote))
            {
                TempData["ErrorMessage"] = "Please provide a reason for rejecting this request.";
                return RedirectToAction(nameof(Index));
            }

            session.CareerAdvisorUserId = _userManager.GetUserId(User)!;
            session.Status = "Rejected";
            session.RejectionNote = rejectionNote;

            AddUserActivity(
                "Reject Counselling Session",
                "CounsellingSession",
                session.CounsellingSessionId.ToString(),
                "Rejected a counselling session request.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Session request rejected.";
            return RedirectToAction(nameof(Index));
        }

        // GET: CounsellingSessions/Edit/5 — adjust an approved session's date/duration/notes
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            CounsellingSession? session = await GetOwnedApprovedSessionAsync(id);
            if (session == null) return NotFound();

            return View(new CounsellingSessionInput
            {
                CounsellingSessionId = session.CounsellingSessionId,
                ScheduledAt = session.ScheduledAt,
                DurationMinutes = session.DurationMinutes,
                Notes = session.Notes,
                MeetingLink = session.MeetingLink
            });
        }

        // POST: CounsellingSessions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CounsellingSessionInput input)
        {
            CounsellingSession? session = await GetOwnedApprovedSessionAsync(id);
            if (session == null) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            session.ScheduledAt = input.ScheduledAt;
            session.DurationMinutes = input.DurationMinutes;
            session.Notes = input.Notes;
            session.MeetingLink = input.MeetingLink;

            AddUserActivity(
                "Update Counselling Session",
                "CounsellingSession",
                session.CounsellingSessionId.ToString(),
                "Updated a counselling session.");

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Counselling session updated successfully.";
            return RedirectToAction(nameof(Index), new { tab = "Approved" });
        }

        // POST: CounsellingSessions/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            CounsellingSession? session = await GetOwnedApprovedSessionAsync(id);
            if (session == null) return NotFound();

            string[] validTransitions = { "Completed", "Cancelled" };

            if (validTransitions.Contains(newStatus))
            {
                session.Status = newStatus;

                AddUserActivity(
                    newStatus == "Completed" ? "Complete Counselling Session" : "Cancel Counselling Session",
                    "CounsellingSession",
                    session.CounsellingSessionId.ToString(),
                    newStatus == "Completed" ? "Marked a counselling session as completed." : "Cancelled a counselling session.");

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Session marked as {newStatus}.";
            }

            return RedirectToAction(nameof(Index), new { tab = "Approved" });
        }

        private async Task<CounsellingSession?> GetOwnedApprovedSessionAsync(int id)
        {
            string userId = _userManager.GetUserId(User)!;

            return await _context.CounsellingSessions
                .FirstOrDefaultAsync(s => s.CounsellingSessionId == id
                    && s.CareerAdvisorUserId == userId
                    && s.Status == "Approved");
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
