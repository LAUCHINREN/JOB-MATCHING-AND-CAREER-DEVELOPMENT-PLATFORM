using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobCareerPlatform.Models.Admin
{
    public class JobCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public string CategoryStatus { get; set; } = "Active";

        // Optional parent category
        public int? ParentCategoryId { get; set; }

        [ForeignKey(nameof(ParentCategoryId))]
        public JobCategory? ParentCategory { get; set; }

        public ICollection<JobCategory> SubCategories { get; set; }
            = new List<JobCategory>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}