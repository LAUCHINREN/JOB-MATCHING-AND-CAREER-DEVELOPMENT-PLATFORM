using System.ComponentModel.DataAnnotations;

namespace JobCareerPlatform.Models
{
    public class CareerResourceInput
    {
        public int CareerResourceId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150, MinimumLength = 3)]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(2000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a resource type.")]
        [Display(Name = "Resource Type")]
        public string ResourceType { get; set; } = "Guide";

        [Url(ErrorMessage = "Enter a valid URL, e.g. https://example.com")]
        [Display(Name = "Link (optional)")]
        public string? ContentUrl { get; set; }

        [Display(Name = "Attachment (optional, e.g. PDF worksheet)")]
        public IFormFile? AttachmentFile { get; set; }

        public string? AttachmentUrl { get; set; }

        [StringLength(50)]
        [Display(Name = "Related Job Category (optional)")]
        public string? RelatedCategory { get; set; }

        [StringLength(200)]
        [Display(Name = "Related Skills (optional, comma-separated)")]
        public string? RelatedSkill { get; set; }

        [Display(Name = "Publish immediately")]
        public bool IsPublished { get; set; }
    }
}
