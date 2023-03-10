using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Spur.Model;

namespace Spur.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(ILogger<AuthenticationService> logger)
    {
        _logger = logger;
    }

    public async Task LoginAsync(HttpContext httpContext, Athlete athlete, CancellationToken ct = default)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, athlete.Name),
            new Claim(ClaimTypes.NameIdentifier, athlete.Id.ToString()),
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
            IsPersistent = true,
            RedirectUri = "/",
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation($"User ID {athlete.Id} logged in at {DateTime.Now}.");
    }
}
