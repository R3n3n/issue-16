using System.Threading.Tasks;

namespace Gateway.Services
{
    public interface IAuditService
    {
        Task LogLoginAsync(string? userId, string email, bool success, string? reason = null, string? ipAddress = null);
        Task LogActionAsync(string? userId, string action, string details, string? ipAddress = null);
    }
}