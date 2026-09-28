using ITElectiveSSO.Models;
using ITELECTIVE_SSO.Data;
using System.Threading.Tasks;
using System;

namespace Gateway.Services
{
    public class AuditService : IAuditService
    {
        private readonly SsoDbContext _context;

        public AuditService(SsoDbContext context)
        {
            _context = context;
        }

        public async Task LogLoginAsync(string? userId, string email, bool success, string? reason = null, string? ipAddress = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = success ? "LoginSuccess" : "LoginFailed",
                Details = success
                    ? $"User '{email}' logged in successfully."
                    : $"Login failed for '{email}'. Reason: {reason ?? "Unknown"}",
                IpAddress = ipAddress ?? "unknown",
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogActionAsync(string? userId, string action, string details, string? ipAddress = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                Details = details,
                IpAddress = ipAddress ?? "unknown",
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}