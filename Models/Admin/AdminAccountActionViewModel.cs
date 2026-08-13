using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models.Admin
{
    public class AdminAccountActionViewModel
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(300, MinimumLength = 5)]
        [Display(Name = "Reason")]
        public string Reason { get; set; } = string.Empty;
    }
}