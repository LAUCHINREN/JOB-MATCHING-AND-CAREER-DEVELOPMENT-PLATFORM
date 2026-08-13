namespace JobCareerPlatform.Models
{
    public class AddSkillsViewModel
    {
        public List<SkillInput> Skills { get; set; } = new()
        {
            new SkillInput()
        };
    }
}