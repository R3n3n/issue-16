using ITElectiveSSO.Models;
using ITELECTIVE_SSO.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests
{
    public class SeedDataTests
    {
        private static UserManager<ApplicationUser> BuildUserManager(string dbName)
        {
            var services = new ServiceCollection();

            services.AddLogging();

            services.AddDbContext<SsoDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<SsoDbContext>()
            .AddDefaultTokenProviders();

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<UserManager<ApplicationUser>>();
        }

        [Fact]
        public async Task SeedAdminAsync_CreatesAdmin_WhenMissing()
        {
            var userManager = BuildUserManager(Guid.NewGuid().ToString());

            await SeedData.SeedAdminAsync(userManager, "admin@itelectivesso.local", "Admin123");

            var admin = await userManager.FindByEmailAsync("admin@itelectivesso.local");

            Assert.NotNull(admin);
            Assert.True(admin!.IsActive);
        }

        [Fact]
        public async Task SeedAdminAsync_SkipsSeeding_WhenAdminAlreadyExists()
        {
            var userManager = BuildUserManager(Guid.NewGuid().ToString());

            await SeedData.SeedAdminAsync(userManager, "admin@itelectivesso.local", "Admin123");
            var countAfterFirstSeed = await userManager.Users.CountAsync();

            await SeedData.SeedAdminAsync(userManager, "admin@itelectivesso.local", "Admin123");
            var countAfterSecondSeed = await userManager.Users.CountAsync();

            Assert.Equal(1, countAfterFirstSeed);
            Assert.Equal(countAfterFirstSeed, countAfterSecondSeed);
        }
    }
}