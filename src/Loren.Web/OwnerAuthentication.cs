using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Loren.Web;

public static class OwnerAuthentication
{
    public const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;

    public static IServiceCollection AddLorenOwnerAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(
            new OwnerPasswordAuthenticator(configuration["LOREN_OWNER_PASSWORD"]));

        services
            .AddAuthentication(Scheme)
            .AddCookie(
                Scheme,
                options =>
                {
                    options.Cookie.Name = "loren-owner";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(12);
                    options.SlidingExpiration = true;
                    options.Events.OnRedirectToLogin = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }

                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    };
                    options.Events.OnRedirectToAccessDenied = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return Task.CompletedTask;
                        }

                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    };
                });

        services.AddAuthorization();
        return services;
    }

    public static IEndpointRouteBuilder MapLorenOwnerEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(
            "/login",
            (HttpContext context) =>
                context.User.Identity?.IsAuthenticated == true
                    ? Results.Redirect("/")
                    : Results.Content(OwnerPages.Login, "text/html; charset=utf-8"));

        endpoints.MapPost(
            "/auth/login",
            async (
                OwnerLoginRequest request,
                OwnerPasswordAuthenticator authenticator,
                HttpContext context) =>
            {
                if (!authenticator.IsConfigured)
                {
                    return Results.Problem(
                        title: "Owner authentication is not configured.",
                        detail: "Set LOREN_OWNER_PASSWORD in the Loren host environment.",
                        statusCode: StatusCodes.Status503ServiceUnavailable);
                }

                if (!authenticator.Verify(request.Password))
                {
                    return Results.Unauthorized();
                }

                ClaimsIdentity identity = new(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "owner"),
                        new Claim(ClaimTypes.Name, "Owner"),
                    ],
                    Scheme);

                ClaimsPrincipal principal = new(identity);
                AuthenticationProperties properties = new()
                {
                    IsPersistent = false,
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12),
                };

                await context.SignInAsync(Scheme, principal, properties);
                return Results.Ok(new { authenticated = true });
            });

        endpoints
            .MapPost(
                "/auth/logout",
                async (HttpContext context) =>
                {
                    await context.SignOutAsync(Scheme);
                    return Results.Ok(new { authenticated = false });
                })
            .RequireAuthorization();

        endpoints
            .MapGet(
                "/api/session",
                () => Results.Ok(new { authenticated = true, owner = "Owner" }))
            .RequireAuthorization();

        return endpoints;
    }
}

public sealed class OwnerPasswordAuthenticator
{
    private readonly byte[]? _configuredPasswordDigest;

    public OwnerPasswordAuthenticator(string? configuredPassword)
    {
        if (!string.IsNullOrEmpty(configuredPassword))
        {
            _configuredPasswordDigest = Digest(configuredPassword);
        }
    }

    public bool IsConfigured => _configuredPasswordDigest is not null;

    public bool Verify(string? candidatePassword)
    {
        if (_configuredPasswordDigest is null || string.IsNullOrEmpty(candidatePassword))
        {
            return false;
        }

        byte[] candidateDigest = Digest(candidatePassword);
        return CryptographicOperations.FixedTimeEquals(
            _configuredPasswordDigest,
            candidateDigest);
    }

    private static byte[] Digest(string value) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(value));
}

public sealed record OwnerLoginRequest(string Password);
