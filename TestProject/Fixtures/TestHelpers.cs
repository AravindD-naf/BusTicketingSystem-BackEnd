using System.Security.Claims;

namespace BusTicketingSystem.Tests.Fixtures;

public static class TestHelpers
{
    public static ClaimsPrincipal BuildClaimsPrincipal(int userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }
}
