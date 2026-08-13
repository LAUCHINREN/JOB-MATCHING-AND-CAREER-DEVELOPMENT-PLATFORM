using JobCareerPlatform.Models;
using JobCareerPlatform.Models.Admin;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JobCareerPlatform.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Job Category Management
        public DbSet<JobCategory> JobCategories { get; set; } = default!;

        // Job Posting Moderation
        public DbSet<JobPosting> JobPostings { get; set; } = default!;

        // Moderation History
        public DbSet<JobModeration> JobModerations { get; set; } = default!;

        // Monitor User Activity
        public DbSet<UserActivityLog> UserActivityLogs { get; set; } = default!;

        // Job Seeker Profile & Skills
        public DbSet<JobSeekerProfile> JobSeekerProfiles { get; set; } = default!;

        public DbSet<JobSeekerSkill> JobSeekerSkills { get; set; } = default!;

        // Job Applications
        public DbSet<JobApplication> JobApplications { get; set; } = default!;

        // Skill Assessments
        public DbSet<SkillAssessment> SkillAssessments { get; set; } = default!;

        public DbSet<AssessmentQuestion> AssessmentQuestions { get; set; } = default!;

        public DbSet<AssessmentResult> AssessmentResults { get; set; } = default!;

        // Employer Company Profiles
        public DbSet<CompanyProfile> CompanyProfileTable { get; set; } = default!;

        // Employer Applicant Fit-Score Preferences
        public DbSet<FitScoreSettings> FitScoreSettings { get; set; } = default!;

        // Career Advisor Resources & Guidance
        public DbSet<CareerResource> CareerResources { get; set; } = default!;

        public DbSet<CounsellingSession> CounsellingSessions { get; set; } = default!;
    }
}
