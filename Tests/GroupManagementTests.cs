using Gateway.Areas.Admin.Controllers;
using Gateway.Areas.Admin.Models;
using ITELECTIVE_SSO.Data;
using ITElectiveSSO.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace Tests
{
    public class GroupManagementTests
    {
        private static SsoDbContext BuildContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<SsoDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new SsoDbContext(options);
        }

        private static void AttachTempData(Controller controller)
        {
            controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                new FakeTempDataProvider());
        }

        private class FakeTempDataProvider : ITempDataProvider
        {
            public IDictionary<string, object> LoadTempData(HttpContext context)
                => new Dictionary<string, object>();

            public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
        }

        private static async Task<TenantApp> SeedApp(SsoDbContext context, string name = "SalesApp")
        {
            var app = new TenantApp { Name = name, ReturnUrl = "https://example.com/callback", IsActive = true };
            context.TenantApps.Add(app);
            await context.SaveChangesAsync();
            return app;
        }

        [Fact]
        public async Task Create_WithValidData_AutoPrefixesGroupName()
        {
            var context = BuildContext(Guid.NewGuid().ToString());
            var app = await SeedApp(context);
            var controller = new GroupsController(context);
            AttachTempData(controller);

            var model = new GroupViewModel { TenantAppId = app.Id, Name = "Admin", PowerLevel = 0 };
            var result = await controller.Create(model);
            var savedGroup = await context.Groups.SingleOrDefaultAsync();

            Assert.IsType<RedirectToActionResult>(result);
            Assert.NotNull(savedGroup);
            Assert.Equal("SalesApp-Admin", savedGroup!.Name);
        }

        [Fact]
        public async Task Create_WithDuplicateNameForSameApp_RejectsSecondGroup()
        {
            var context = BuildContext(Guid.NewGuid().ToString());
            var app = await SeedApp(context);
            var controller = new GroupsController(context);
            AttachTempData(controller);

            await controller.Create(new GroupViewModel { TenantAppId = app.Id, Name = "Manager", PowerLevel = 1 });
            var secondResult = await controller.Create(new GroupViewModel { TenantAppId = app.Id, Name = "Manager", PowerLevel = 2 });

            var totalGroups = await context.Groups.CountAsync(g => g.Name == "SalesApp-Manager");

            Assert.IsType<ViewResult>(secondResult);
            Assert.False(controller.ModelState.IsValid);
            Assert.Equal(1, totalGroups);
        }

        [Fact]
        public async Task Create_WithNegativePowerLevel_FailsModelValidation()
        {
            var context = BuildContext(Guid.NewGuid().ToString());
            var app = await SeedApp(context);
            var controller = new GroupsController(context);
            AttachTempData(controller);

            var model = new GroupViewModel { TenantAppId = app.Id, Name = "Viewer", PowerLevel = -1 };
            controller.ModelState.AddModelError("PowerLevel", "Power level must be 0 (highest) or greater.");

            var result = await controller.Create(model);

            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

    }
}