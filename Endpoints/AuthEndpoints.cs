using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using _10x_cards.Data;
using _10x_cards.Services;

namespace _10x_cards.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth");

        auth.MapPost("/login", async (LoginRequest request, ApplicationDbContext db, IEmailSender emailSender, HttpContext httpContext) =>
        {
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

            var magicLink = new MagicLinkToken
            {
                Id = Guid.NewGuid(),
                Token = token,
                Email = request.Email,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            };

            db.MagicLinkTokens.Add(magicLink);
            await db.SaveChangesAsync();

            var callbackUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api/auth/callback?token={token}";
            await emailSender.SendMagicLinkAsync(request.Email, callbackUrl);

            return Results.Ok(new { message = "If this email is registered, you will receive a magic link." });
        })
        .AllowAnonymous()
        .WithName("Login");

        auth.MapGet("/callback", async (string token, ApplicationDbContext db, HttpContext httpContext) =>
        {
            var magicLinkToken = await db.MagicLinkTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (magicLinkToken is null || magicLinkToken.UsedAt is not null || magicLinkToken.ExpiresAt < DateTime.UtcNow)
            {
                return Results.Json(
                    new { error = "This link is no longer valid. Please request a new one." },
                    statusCode: 400);
            }

            magicLinkToken.UsedAt = DateTime.UtcNow;

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == magicLinkToken.Email);
            if (user is null)
            {
                user = new User { Id = Guid.NewGuid(), Email = magicLinkToken.Email };
                db.Users.Add(user);
            }

            await db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });

            return Results.Redirect("/");
        })
        .AllowAnonymous()
        .WithName("AuthCallback");

        auth.MapPost("/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok(new { message = "Logged out." });
        })
        .WithName("Logout");

        auth.MapGet("/me", (HttpContext httpContext) =>
        {
            var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = httpContext.User.FindFirstValue(ClaimTypes.Email);
            return Results.Ok(new { id = userId, email });
        })
        .WithName("Me");

        return app;
    }
}

public record LoginRequest(string Email);
