using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITElectiveSSO.Models
{
    public class Group
    {
        public int Id { get; set; }
        public int TenantAppId { get; set; }
        public TenantApp TenantApp { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public int PowerLevel { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    }
}