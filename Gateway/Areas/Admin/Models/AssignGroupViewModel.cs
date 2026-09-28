using System.ComponentModel.DataAnnotations;

namespace Gateway.Areas.Admin.Models
{
    public class AssignGroupViewModel
    {
        [Required]
        public int GroupId { get; set; }
    }
}