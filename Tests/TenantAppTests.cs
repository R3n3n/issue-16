using Gateway.Areas.Admin.Controllers;
using Gateway.Areas.Admin.Models;
using ITELECTIVE_SSO.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace Tests
{
    public class TenantAppTests
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

        [Fact]
        public async Task Create_WithValidData_CreatesAppSuccessfully()
        {
            var context = BuildContext(Guid.NewGuid().ToString());
            var controller = new TenantAppsController(context);
            AttachTempData(controller);

            var model = new TenantAppViewModel
            {
                Name = "Student Portal",
                ReturnUrl = "https://example.com/callback"
            };

            var result = await controller.Create(model);
            var savedApp = await context.TenantApps.SingleOrDefaultAsync();

            Assert.IsType<RedirectToActionResult>(result);
            Assert.NotNull(savedApp);
            Assert.Equal("Student Portal", savedApp!.Name);
            Assert.Equal("https://example.com/callback", savedApp.ReturnUrl);
            Assert.True(savedApp.IsActive);
        }

        [Fact]

        public async Task Create_WithDuplicateAppName_RejectsAndDoesNotSaveSecondApp()

        {


            var context = BuildContext(Guid.NewGuid().ToString());

            var controller = new TenantAppsController(context);

            AttachTempData(controller);


            var firstApp = new TenantAppViewModel
            {

                Name = "Library System",

                ReturnUrl = "https://library.example.com/callback"
            };

            var duplicateApp = new TenantAppViewModel
            {

                Name = "Library System",

                ReturnUrl = "https://library-mirror.example.com/callback"
            };


            await controller.Create(firstApp);

            var secondResult = await controller.Create(duplicateApp);

            var totalAppsWithName = await context.TenantApps

                .Where(a => a.Name == "Library System")

                .CountAsync();


            Assert.IsType<ViewResult>(secondResult);

            Assert.False(controller.ModelState.IsValid);

            Assert.Equal(1, totalAppsWithName);

        }

    }
}