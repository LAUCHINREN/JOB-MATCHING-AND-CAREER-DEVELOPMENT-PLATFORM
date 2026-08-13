using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class CounsellingSession
    {
        [Key]
        public int CounsellingSessionId { get; set; }

        // Plain strings (no FK/nav to ApplicationUser) — same convention as JobApplication.UserId.
        [Required]
        public string JobSeekerUserId { get; set; } = string.Empty;

        // Null while Pending — nobody has claimed the request yet. Set to the advisor who
        // approves or rejects it, so it's clear who's handling the session going forward.
        public string? CareerAdvisorUserId { get; set; }

        // The job seeker's requested date & time when applying; unchanged by approval unless
        // the advisor edits it afterwards.
        [Required]
        public DateTime ScheduledAt { get; set; }

        [Range(15, 240)]
        public int DurationMinutes { get; set; } = 30;

        // Pending / Approved / Rejected / Completed / Cancelled
        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        // The job seeker's reason for requesting the session.
        [StringLength(500)]
        public string? Notes { get; set; }

        // The advisor's reason when rejecting a request.
        [StringLength(500)]
        public string? RejectionNote { get; set; }

        // Online meeting link (Zoom/Google Meet/Teams/etc.) — required before a session can be approved.
        [StringLength(500)]
        public string? MeetingLink { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
