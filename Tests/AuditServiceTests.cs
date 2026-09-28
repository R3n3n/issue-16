using Gateway.Services;
using ITELECTIVE_SSO.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using Xunit;

namespace Tests
{
    public class AuditServiceTests
    {
        private static SsoDbContext BuildContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<SsoDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new SsoDbContext(options);
        }

        [Fact]
        public async Task LogLoginAsync_CreatesAuditLog_OnSuccessfulLogin()
        {
            var context = BuildContext(Guid.NewGuid().ToString());
            var service = new AuditService(context);

            await service.LogLoginAsync("user-123", "test@itelectivesso.local", success: true, ipAddress: "127.0.0.1");

            var log = await context.AuditLogs.SingleOrDefaultAsync(a => a.Action == "LoginSuccess");

            Assert.NotNull(log);
            Assert.Equal("user-123", log!.UserId);
            Assert.Contains("test@itelectivesso.local", log.Details);
        }

        [Fact]
        public async Task LogActionAsync_CreatesAuditLog_OnAdminAction()
        {
            var context = BuildContext(Guid.NewGuid().ToString());
            var service = new AuditService(context);

            await service.LogActionAsync("admin-1", "ToggleActive", "Toggled user 'test@itelectivesso.local' to inactive.");

            var log = await context.AuditLogs.SingleOrDefaultAsync(a => a.Action == "ToggleActive");

            Assert.NotNull(log);
            Assert.Equal("admin-1", log!.UserId);
            Assert.Contains("test@itelectivesso.local", log.Details);
        }

    }
}