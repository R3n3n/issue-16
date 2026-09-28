using ITElectiveSSO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITELECTIVE_SSO.Data
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.HasIndex(u => u.NormalizedEmail)
                   .HasDatabaseName("EmailIndex")
                   .IsUnique();
        }
    }
}