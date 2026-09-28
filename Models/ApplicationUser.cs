using Microsoft.AspNetCore.Identity;

namespace ITElectiveSSO.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    }
}