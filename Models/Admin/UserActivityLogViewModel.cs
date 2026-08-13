namespace JobCareerPlatform.Models
{
    public class UserActivityLogViewModel
    {
        public int ActivityId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        public string UserRole { get; set; } = string.Empty;

        public string ActivityType { get; set; } = string.Empty;

        public string EntityType { get; set; } = string.Empty;

        public string EntityId { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}