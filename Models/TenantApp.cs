using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ITElectiveSSO.Models
{
    public class TenantApp
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Group> Groups { get; set; } = new List<Group>();
    }
}