using Gateway.Areas.Admin.Controllers;
using Gateway.Areas.Admin.Models;
using ITElectiveSSO.Models;
using ITELECTIVE_SSO.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests
{
    public class UserGroupsControllerTests
    {
        private static (UserManager<ApplicationUser> userManager, SsoDbContext context) BuildServices(string dbName)
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

            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = provider.GetRequiredService<SsoDbContext>();

            return (userManager, context);
        }

        private sealed class NoopTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
            public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
        }

        private static UserGroupsController CreateController(UserManager<ApplicationUser> userManager, SsoDbContext context)
        {
            return new UserGroupsController(userManager, context)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), new NoopTempDataProvider())
            };
        }

        private static async Task<(ApplicationUser user, Group group1, Group group2)> SeedUserAndGroups(
            UserManager<ApplicationUser> userManager, SsoDbContext context)
        {
            var user = new ApplicationUser
            {
                UserName = "member@itelectivesso.local",
                Email = "member@itelectivesso.local",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await userManager.CreateAsync(user, "Password1");

            var app1 = new TenantApp { Name = "SalesApp", ReturnUrl = "https://sales.example.com/callback", IsActive = true, CreatedAt = DateTime.UtcNow };
            var app2 = new TenantApp { Name = "SupportApp", ReturnUrl = "https://support.example.com/callback", IsActive = true, CreatedAt = DateTime.UtcNow };
            context.TenantApps.AddRange(app1, app2);
            await context.SaveChangesAsync();

            var group1 = new Group { TenantAppId = app1.Id, Name = "SalesApp-Admin", PowerLevel = 0, CreatedAt = DateTime.UtcNow };
            var group2 = new Group { TenantAppId = app2.Id, Name = "SupportApp-Agent", PowerLevel = 1, CreatedAt = DateTime.UtcNow };
            context.Groups.AddRange(group1, group2);
            await context.SaveChangesAsync();

            return (user, group1, group2);
        }

        [Fact]
        public async Task Assign_AllowsUserToBeAssignedToMultipleGroups()
        {
            var (userManager, context) = BuildServices(Guid.NewGuid().ToString());
            var (user, group1, group2) = await SeedUserAndGroups(userManager, context);
            var controller = CreateController(userManager, context);

            await controller.Assign(user.Id, new AssignGroupViewModel { GroupId = group1.Id });
            await controller.Assign(user.Id, new AssignGroupViewModel { GroupId = group2.Id });

            var assignedCount = await context.UserGroups.CountAsync(ug => ug.UserId == user.Id);

            Assert.Equal(2, assignedCount);
        }

        [Fact]
        public async Task Unassign_RemovesRelationship()
        {
            var (userManager, context) = BuildServices(Guid.NewGuid().ToString());
            var (user, group1, _) = await SeedUserAndGroups(userManager, context);
            var controller = CreateController(userManager, context);

            await controller.Assign(user.Id, new AssignGroupViewModel { GroupId = group1.Id });
            await controller.Unassign(user.Id, group1.Id);

            var stillAssigned = await context.UserGroups
                .AnyAsync(ug => ug.UserId == user.Id && ug.GroupId == group1.Id);

            Assert.False(stillAssigned);
        }

        [Fact]
        public async Task Assign_PreventsDuplicateAssignment()
        {
            var (userManager, context) = BuildServices(Guid.NewGuid().ToString());
            var (user, group1, _) = await SeedUserAndGroups(userManager, context);
            var controller = CreateController(userManager, context);

            await controller.Assign(user.Id, new AssignGroupViewModel { GroupId = group1.Id });
            await controller.Assign(user.Id, new AssignGroupViewModel { GroupId = group1.Id });

            var assignedCount = await context.UserGroups
                .CountAsync(ug => ug.UserId == user.Id && ug.GroupId == group1.Id);

            Assert.Equal(1, assignedCount);
        }
    }
}