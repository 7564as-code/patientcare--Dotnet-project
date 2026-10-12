using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetCaregiverId(this ClaimsPrincipal user)
    {
        return user.TryGetCaregiverId()
            ?? throw new UnauthorizedAccessException("Token does not contain a valid caregiver identifier.");
    }

    /// <summary>מזהה בן המשפחה מהאסימון, או null אם אין (למשל בקשה אנונימית).</summary>
    public static int? TryGetCaregiverId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var id) ? id : null;
    }
}
