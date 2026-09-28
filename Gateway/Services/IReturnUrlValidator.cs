using ITElectiveSSO.Models;

namespace Gateway.Services
{
    public interface IReturnUrlValidator
    {
        Task<TenantApp?> ValidateAsync(string? returnUrl, string? ipAddress = null);
    }
}