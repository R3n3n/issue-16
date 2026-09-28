using System.ComponentModel.DataAnnotations;

namespace Gateway.Areas.Admin.Models
{
    public class TenantAppViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "App name is required.")]
        [StringLength(100, ErrorMessage = "App name cannot exceed 100 characters.")]
        [Display(Name = "App Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Return URL is required.")]
        [StringLength(500, ErrorMessage = "Return URL cannot exceed 500 characters.")]
        [Url(ErrorMessage = "Return URL must be a valid URL (e.g. https://example.com/callback).")]
        [Display(Name = "Return URL")]
        public string ReturnUrl { get; set; } = string.Empty;
    }
}