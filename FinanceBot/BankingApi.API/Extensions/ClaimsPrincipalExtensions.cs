using System.Security.Claims;

namespace BankingApi.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new UnauthorizedAccessException("Пользователь не авторизован");

        return Guid.Parse(value);
    }
}