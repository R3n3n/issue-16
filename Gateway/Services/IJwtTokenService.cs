using ITElectiveSSO.Models;

namespace Gateway.Services
{
    public interface IJwtTokenService
    {
        Task<string> CreateTokenAsync(ApplicationUser user, TenantApp app);
    }
}