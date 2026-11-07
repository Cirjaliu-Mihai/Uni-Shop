using UniShop.Api.Contracts;
using UniShop.Api.Domain;

namespace UniShop.Api.Services;

public interface ITokenService
{
    AuthResponse CreateToken(ApplicationUser user, IReadOnlyCollection<string> roles);
}