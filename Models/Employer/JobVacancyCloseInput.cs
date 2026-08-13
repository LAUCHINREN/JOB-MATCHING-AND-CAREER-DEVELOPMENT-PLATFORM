using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class JobVacancyCloseInput
    {
        public int JobVacancyId { get; set; }

        [Required(ErrorMessage = "Please provide a reason for closing this job post.")]
        [StringLength(500)]
        [Display(Name = "Reason for Closing")]
        public string CloseReason { get; set; } = string.Empty;
    }
}
