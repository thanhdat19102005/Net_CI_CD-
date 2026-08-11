using ClothHub.Models;

namespace ClothHub.Service.Auth
{
    public interface IJwtTokenService
    {
        string CreateAccessToken(
            AppUserModel user,
            IEnumerable<string> roles,
            DateTime expiresAtUtc
        );
    }
}
