using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models.Admin
{
    public class AdminJobCategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Parent Category")]
        public int? ParentCategoryId { get; set; }
    }
}