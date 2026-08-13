using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class JobVacancyInput
    {
        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(150, MinimumLength = 5)]
        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "Category")]
        public int JobCategoryId { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(2000)]
        [Display(Name = "Job Description")]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Required Skills")]
        public string? RequiredSkills { get; set; }

        [StringLength(200)]
        [Display(Name = "Qualification")]
        public string? Qualification { get; set; }

        [StringLength(100)]
        [Display(Name = "Employment Type")]
        public string? EmploymentType { get; set; }

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(150)]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        [Range(0, 1000000, ErrorMessage = "Salary must be between 0 and 1,000,000.")]
        [Display(Name = "Minimum Salary (RM)")]
        public decimal? SalaryMin { get; set; }

        [Range(0, 1000000, ErrorMessage = "Salary must be between 0 and 1,000,000.")]
        [Display(Name = "Maximum Salary (RM)")]
        public decimal? SalaryMax { get; set; }
    }
}
