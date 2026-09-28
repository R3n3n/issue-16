using Gateway.Services;
using ITELECTIVE_SSO.Data;
using ITElectiveSSO.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests
{
    public class ReturnUrlValidatorTests
    {
        private static SsoDbContext BuildContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<SsoDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new SsoDbContext(options);
        }

        [Fact]
        public async Task ValidateAsync_ReturnsApp_WhenUrlIsRegistered()
        {
            var context = BuildContext(Guid.NewGuid().ToString());
            context.TenantApps.Add(new TenantApp
            {
                Name = "SalesApp",
                ReturnUrl = "https://salesapp.example.com/callback",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var validator = new ReturnUrlValidator(context);

            var result = await validator.ValidateAsync("https://salesapp.example.com/callback");

            Assert.NotNull(result);
            Assert.Equal("SalesApp", result!.Name);
        }

        [Fact]
        public async Task ValidateAsync_ReturnsNull_WhenUrlIsNotRegistered()
        {
            var context = BuildContext(Guid.NewGuid().ToString());
            var validator = new ReturnUrlValidator(context);

            var result = await validator.ValidateAsync("https://not-registered.example.com/callback");

            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateAsync_LogsAuditEntry_WhenUrlIsNotRegistered()
        {
            var context = BuildContext(Guid.NewGuid().ToString());
            var validator = new ReturnUrlValidator(context);

            await validator.ValidateAsync("https://not-registered.example.com/callback", "127.0.0.1");

            var logEntry = await context.AuditLogs
                .SingleOrDefaultAsync(a => a.Action == "InvalidReturnUrl");

            Assert.NotNull(logEntry);
            Assert.Contains("not-registered.example.com", logEntry!.Details);
            Assert.Equal("127.0.0.1", logEntry.IpAddress);
        }
    }
}