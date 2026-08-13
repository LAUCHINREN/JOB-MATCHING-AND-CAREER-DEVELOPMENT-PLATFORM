using Microsoft.AspNetCore.Identity;

namespace JobCareerPlatform.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string UserRole { get; set; } = "JobSeeker";

        public string AccountStatus { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}