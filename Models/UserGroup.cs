using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace ITElectiveSSO.Models
{
    public class UserGroup
    {
        /// <summary>
        /// gets/sets the foreign key ID of the user
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// navigation property to said user
        /// the red error line can be ignored for now :3
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// gets/sets the foreign key ID of the group
        /// </summary>
        [Required]
        public int GroupId { get; set; }

        /// <summary>
        /// navigation property to the associated group
        /// </summary>
        [ForeignKey(nameof(GroupId))]
        public Group Group { get; set; } = null!;
    }
}