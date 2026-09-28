using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Gateway.Areas.Admin.Models
{
    public class GroupViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select an app.")]
        [Display(Name = "Tenant App")]
        public int TenantAppId { get; set; }

        [Required(ErrorMessage = "Group name is required.")]
        [StringLength(100, ErrorMessage = "Group name cannot exceed 100 characters.")]
        [Display(Name = "Group Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Power level is required.")]
        [Range(0, 100, ErrorMessage = "Power level must be 0 (highest) or greater.")]
        [Display(Name = "Power Level")]
        public int PowerLevel { get; set; }
        public List<SelectListItem>? TenantApps { get; set; }
    }
}