using ITElectiveSSO.Models;
using ITELECTIVE_SSO.Data;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Services
{
    public class ReturnUrlValidator : IReturnUrlValidator
    {
        private readonly SsoDbContext _context;

        public ReturnUrlValidator(SsoDbContext context)
        {
            _context = context;
        }

        public async Task<TenantApp?> ValidateAsync(string? returnUrl, string? ipAddress = null)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return null;
            }

            var app = await _context.TenantApps
                .FirstOrDefaultAsync(a => a.ReturnUrl == returnUrl && a.IsActive);

            if (app == null)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = null,
                    Action = "InvalidReturnUrl",
                    Details = $"Rejected unapproved return URL: {returnUrl}",
                    IpAddress = ipAddress ?? "unknown",
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            return app;
        }
    }
}