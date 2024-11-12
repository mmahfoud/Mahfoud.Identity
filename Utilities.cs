using System.Security.Claims;

namespace Mahfoud.Identity;

public static class Utilities
{
    public static long? GetUserId(this ClaimsPrincipal cp)
    {
        var userIdString = cp.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (userIdString == null) return null;
        return long.Parse(userIdString);

    }
}
