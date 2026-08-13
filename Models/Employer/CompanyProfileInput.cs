using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class CompanyProfileInput
    {
        public int CompanyProfileId { get; set; }

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(150, MinimumLength = 2,
            ErrorMessage = "Company name must be between 2 and 150 characters.")]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required(ErrorMessage = "Industry is required.")]
        [StringLength(100)]
        [Display(Name = "Industry")]
        public string? Industry { get; set; }

        [Required(ErrorMessage = "Company size is required.")]
        [RegularExpression(@"^\d+(-\d+|\+)$",ErrorMessage = "Company size must be in a format such as 1-10 or 1000+.")]
        [Display(Name = "Company Size")]
        public string? CompanySize { get; set; }

        [Url(ErrorMessage = "Enter a valid URL, e.g. https://example.com")]
        [Display(Name = "Website")]
        public string? Website { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        [Display(Name = "Company Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(300)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Contact email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Contact Email")]
        public string? ContactEmail { get; set; }

        [Required(ErrorMessage = "Contact number is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [Display(Name = "Company Logo")]
        public IFormFile? LogoFile { get; set; }
        public string? LogoUrl { get; set; }
        public string? LogoS3Key { get; set; }
    }
}
