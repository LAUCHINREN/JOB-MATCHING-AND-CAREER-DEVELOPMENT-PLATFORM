using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class CompanyProfile
    {
        [Key]
        public int CompanyProfileId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;   // FK -> ApplicationUser.Id, one-to-one

        public string? CompanyName { get; set; }
        public string? Industry { get; set; }
        public string? CompanySize { get; set; }
        public string? Website { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactNumber { get; set; }

        // Populated once S3 upload logic lands (deferred, D-004) — columns exist now.
        public string? LogoUrl { get; set; }
        public string? LogoS3Key { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
