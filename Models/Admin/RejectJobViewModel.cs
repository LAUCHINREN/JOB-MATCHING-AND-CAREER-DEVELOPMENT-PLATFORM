using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models.Admin
{
    public class RejectJobViewModel
    {
        public int JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public string EmployerName { get; set; } = string.Empty;

        [Required]
        [StringLength(500, MinimumLength = 5)]
        [Display(Name = "Rejection Reason")]
        public string Reason { get; set; } = string.Empty;
    }
}