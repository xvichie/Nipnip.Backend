using System.Security.Claims;

namespace NipNip.Shared.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetClerkUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst("sub")?.Value
            ?? throw new InvalidOperationException("Authenticated user has no sub claim.");
    }
}
