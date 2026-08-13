namespace JobCareerPlatform.Models
{
    public class ResourceMatch
    {
        public CareerResource Resource { get; set; } = null!;
        public List<string> Reasons { get; set; } = new();
    }

    // Advisor-facing view: for one resource, which job seekers it was auto-matched to and why.
    public class ResourceReach
    {
        public CareerResource Resource { get; set; } = null!;
        public List<MatchedJobSeeker> MatchedJobSeekers { get; set; } = new();
    }

    public class MatchedJobSeeker
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<string> Reasons { get; set; } = new();
    }

    // Automatically matches published CareerResources to a job seeker — replaces manual,
    // per-job-seeker recommending. A resource matches if its RelatedCategory/RelatedSkill tags
    // (set by the advisor when publishing) line up with the job seeker's preferred category,
    // field of study, or the required skills of jobs they've applied to.
    public static class ResourceMatcher
    {
        public static ResourceMatch? Evaluate(CareerResource resource, JobSeekerProfile? profile, List<string> appliedJobRequiredSkills)
        {
            List<string> reasons = new();
            List<string> resourceTags = SplitTags(resource.RelatedSkill);

            if (profile != null
                && !string.IsNullOrWhiteSpace(profile.PreferredJobCategory)
                && !string.IsNullOrWhiteSpace(resource.RelatedCategory)
                && string.Equals(profile.PreferredJobCategory.Trim(), resource.RelatedCategory.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add($"Matches your preferred category ({resource.RelatedCategory})");
            }

            if (profile != null && !string.IsNullOrWhiteSpace(profile.FieldOfStudy) && resourceTags.Count > 0)
            {
                string fieldOfStudy = profile.FieldOfStudy.Trim();
                bool fieldMatches = resourceTags.Any(tag =>
                    tag.Contains(fieldOfStudy, StringComparison.OrdinalIgnoreCase) ||
                    fieldOfStudy.Contains(tag, StringComparison.OrdinalIgnoreCase));

                if (fieldMatches)
                {
                    reasons.Add($"Related to your field of study ({profile.FieldOfStudy})");
                }
            }

            if (resourceTags.Count > 0 && appliedJobRequiredSkills.Count > 0)
            {
                string? matchedSkill = appliedJobRequiredSkills.FirstOrDefault(skill =>
                    resourceTags.Any(tag => tag.Equals(skill, StringComparison.OrdinalIgnoreCase)));

                if (matchedSkill != null)
                {
                    reasons.Add($"Matches a skill required by a job you applied to ({matchedSkill})");
                }
            }

            if (reasons.Count == 0)
            {
                return null;
            }

            return new ResourceMatch { Resource = resource, Reasons = reasons };
        }

        public static List<string> SplitTags(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<string>();
            }

            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();
        }
    }
}
