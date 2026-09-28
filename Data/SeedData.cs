using ITElectiveSSO.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ITELECTIVE_SSO.Data
{
    public static class SeedData
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var adminEmail = configuration["AdminSeed:Email"];
            var adminPassword = configuration["AdminSeed:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException(
                    "AdminSeed:Email and AdminSeed:Password must be set in appsettings.json.");
            }

            await SeedAdminAsync(userManager, adminEmail, adminPassword);
        }

        public static async Task SeedAdminAsync(
            UserManager<ApplicationUser> userManager,
            string adminEmail,
            string adminPassword)
        {
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin != null)
            {
                return;
            }
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to seed admin user: {errors}");
            }
        }
    }
}