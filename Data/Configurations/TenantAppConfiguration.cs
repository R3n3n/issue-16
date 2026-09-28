using ITElectiveSSO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITELECTIVE_SSO.Data
{
    public class TenantAppConfiguration : IEntityTypeConfiguration<TenantApp>
    {
        public void Configure(EntityTypeBuilder<TenantApp> builder)
        {
            builder.HasIndex(t => t.Name).IsUnique();
            builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
            builder.Property(t => t.ReturnUrl).IsRequired().HasMaxLength(500);
        }
    }
}