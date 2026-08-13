namespace JobCareerPlatform.Models
{
    public class FitScoreAspect
    {
        public string Aspect { get; set; } = string.Empty;
        public int PointsEarned { get; set; }
        public int MaxPoints { get; set; }
    }

    public static class FitScoreCalculator
    {
        // profile is passed explicitly (rather than via a JobApplication.JobSeekerProfile nav property)
        // since JobApplication.UserId isn't guaranteed to have a matching JobSeekerProfile row yet.
        public static List<FitScoreAspect> GetBreakdown(JobApplication app, JobSeekerProfile? profile, FitScoreSettings settings)
        {
            JobSeekerProfile? p = profile;
            JobPosting? v = app.JobPosting;
            var breakdown = new List<FitScoreAspect>();

            // Salary fit
            int salaryPoints = 0;
            if (p != null && v != null && p.ExpectedSalary.HasValue && v.SalaryMax.HasValue)
            {
                if (p.ExpectedSalary <= v.SalaryMax)
                {
                    salaryPoints = settings.SalaryWeight;
                }
                else if (p.ExpectedSalary <= v.SalaryMax * 1.1m)
                {
                    salaryPoints = (int)(settings.SalaryWeight * 0.5);
                }
            }
            breakdown.Add(new FitScoreAspect { Aspect = "Salary Match", PointsEarned = salaryPoints, MaxPoints = settings.SalaryWeight });

            // Category fit
            bool categoryMatched = p != null && v?.JobCategory != null && !string.IsNullOrEmpty(p.PreferredJobCategory)
                && p.PreferredJobCategory.Equals(v.JobCategory.CategoryName, StringComparison.OrdinalIgnoreCase);
            breakdown.Add(new FitScoreAspect { Aspect = "Category Match", PointsEarned = categoryMatched ? settings.CategoryWeight : 0, MaxPoints = settings.CategoryWeight });

            // Location fit
            bool locationMatched = p != null && v != null && !string.IsNullOrEmpty(p.PreferredLocation)
                && p.PreferredLocation.Equals(v.Location, StringComparison.OrdinalIgnoreCase);
            breakdown.Add(new FitScoreAspect { Aspect = "Location Match", PointsEarned = locationMatched ? settings.LocationWeight : 0, MaxPoints = settings.LocationWeight });

            // Education fit
            bool educationMatched = p != null && v != null && !string.IsNullOrEmpty(p.EducationLevel) && !string.IsNullOrEmpty(v.Qualification)
                && v.Qualification.Contains(p.EducationLevel, StringComparison.OrdinalIgnoreCase);
            breakdown.Add(new FitScoreAspect { Aspect = "Education Match", PointsEarned = educationMatched ? settings.EducationWeight : 0, MaxPoints = settings.EducationWeight });

            return breakdown;
        }

        public static int CalculateFitScore(JobApplication app, JobSeekerProfile? profile, FitScoreSettings settings)
        {
            return GetBreakdown(app, profile, settings).Sum(a => a.PointsEarned);
        }
    }
}
