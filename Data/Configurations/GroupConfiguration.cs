using ITElectiveSSO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITELECTIVE_SSO.Data
{
    public class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.Property(g => g.Name).IsRequired().HasMaxLength(150);
            builder.Property(g => g.PowerLevel).IsRequired();

            builder.HasOne(g => g.TenantApp)
                   .WithMany(t => t.Groups)
                   .HasForeignKey(g => g.TenantAppId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(g => new { g.TenantAppId, g.Name }).IsUnique();
        }
    }
}