using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class CounsellingSessionRequestInput
    {
        [Required(ErrorMessage = "Please choose a preferred date and time.")]
        [Display(Name = "Preferred Date & Time")]
        public DateTime ScheduledAt { get; set; } = DateTime.Now.AddDays(1);

        [Range(15, 240, ErrorMessage = "Duration must be between 15 and 240 minutes.")]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; } = 30;

        [Required(ErrorMessage = "Please tell your advisor why you'd like a session.")]
        [StringLength(500)]
        [Display(Name = "Reason for the session")]
        public string? Notes { get; set; }
    }
}
