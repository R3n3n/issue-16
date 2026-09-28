using ITElectiveSSO.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITELECTIVE_SSO.Data
{
    public class SsoDbContext : IdentityDbContext<ApplicationUser>
    {
        public SsoDbContext(DbContextOptions<SsoDbContext> options) : base(options) { }

        public DbSet<TenantApp> TenantApps { get; set; } = null!;
        public DbSet<Group> Groups { get; set; } = null!;
        public DbSet<UserGroup> UserGroups { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new ApplicationUserConfiguration());
            builder.ApplyConfiguration(new TenantAppConfiguration());
            builder.ApplyConfiguration(new GroupConfiguration());
            builder.ApplyConfiguration(new UserGroupConfiguration());
            builder.ApplyConfiguration(new AuditLogConfiguration());
        }
    }
}
