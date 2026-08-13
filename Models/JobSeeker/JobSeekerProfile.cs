using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class JobSeekerProfile
    {
        public int JobSeekerProfileId { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [Display(Name = "Career Objective")]
        [StringLength(500)]
        public string? CareerObjective { get; set; }

        [Display(Name = "Education Level")]
        public string? EducationLevel { get; set; }

        [Display(Name = "Field of Study")]
        public string? FieldOfStudy { get; set; }

        [Display(Name = "Years of Experience")]
        [Range(0, 50)]
        public int ExperienceYears { get; set; }

        [Display(Name = "Preferred Job Category")]
        public string? PreferredJobCategory { get; set; }

        [Display(Name = "Preferred Work Location")]
        public string? PreferredLocation { get; set; }

        [Precision(10, 2)]
        [Display(Name = "Expected Salary")]
        [Range(0, 1000000)]
        public decimal? ExpectedSalary { get; set; }

        [Display(Name = "Resume")]
        public string? ResumeS3Key { get; set; }
    }
}