using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class FitScoreSettings
    {
        [Key]
        public int FitScoreSettingsId { get; set; }

        public int CompanyProfileId { get; set; }
        public CompanyProfile? CompanyProfile { get; set; }

        public int SalaryWeight { get; set; } = 35;
        public int CategoryWeight { get; set; } = 30;
        public int LocationWeight { get; set; } = 25;
        public int EducationWeight { get; set; } = 10;
    }
}
