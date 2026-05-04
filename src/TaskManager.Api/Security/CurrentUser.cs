using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace TaskManager.Api.Security;

public static class CurrentUser
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var id))
        {
            throw new InvalidOperationException("Authenticated user id is missing or invalid.");
        }

        return id;
    }
}
