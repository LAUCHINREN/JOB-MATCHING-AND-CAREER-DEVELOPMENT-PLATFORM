using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class CounsellingSessionInput
    {
        public int CounsellingSessionId { get; set; }

        [Required(ErrorMessage = "Please choose a date and time.")]
        [Display(Name = "Scheduled Date & Time")]
        public DateTime ScheduledAt { get; set; } = DateTime.Now.AddDays(1);

        [Range(15, 240, ErrorMessage = "Duration must be between 15 and 240 minutes.")]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; } = 30;

        [StringLength(500)]
        [Display(Name = "Notes / Agenda")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Please provide an online meeting link.")]
        [Url(ErrorMessage = "Enter a valid URL, e.g. https://meet.google.com/xyz")]
        [StringLength(500)]
        [Display(Name = "Meeting Link")]
        public string? MeetingLink { get; set; }
    }
}
