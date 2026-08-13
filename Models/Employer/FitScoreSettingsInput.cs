using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class FitScoreSettingsInput
    {
        [Range(0, 100, ErrorMessage = "Weight must be between 0 and 100.")]
        [Display(Name = "Salary Match")]
        public int SalaryWeight { get; set; } = 35;

        [Range(0, 100, ErrorMessage = "Weight must be between 0 and 100.")]
        [Display(Name = "Category Match")]
        public int CategoryWeight { get; set; } = 30;

        [Range(0, 100, ErrorMessage = "Weight must be between 0 and 100.")]
        [Display(Name = "Location Match")]
        public int LocationWeight { get; set; } = 25;

        [Range(0, 100, ErrorMessage = "Weight must be between 0 and 100.")]
        [Display(Name = "Education Match")]
        public int EducationWeight { get; set; } = 10;
    }
}
