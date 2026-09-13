using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using _10x_cards.Data;

namespace _10x_cards.Auth;

public class DbSessionStore(IServiceProvider serviceProvider) : ITicketStore
{
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var session = new AuthSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(ticket.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)!),
            ExpiresAt = ticket.Properties.ExpiresUtc?.UtcDateTime ?? DateTime.UtcNow.AddDays(30),
            TicketData = TicketSerializer.Default.Serialize(ticket)
        };

        db.AuthSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id.ToString();
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        if (!Guid.TryParse(key, out var sessionId)) return;

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var session = await db.AuthSessions.FindAsync(sessionId);
        if (session is null) return;

        session.ExpiresAt = ticket.Properties.ExpiresUtc?.UtcDateTime ?? DateTime.UtcNow.AddDays(30);
        session.TicketData = TicketSerializer.Default.Serialize(ticket);
        await db.SaveChangesAsync();
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (!Guid.TryParse(key, out var sessionId)) return null;

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var session = await db.AuthSessions.FindAsync(sessionId);
        if (session is null || session.ExpiresAt < DateTime.UtcNow) return null;

        return TicketSerializer.Default.Deserialize(session.TicketData);
    }

    public async Task RemoveAsync(string key)
    {
        if (!Guid.TryParse(key, out var sessionId)) return;

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var session = await db.AuthSessions.FindAsync(sessionId);
        if (session is null) return;

        db.AuthSessions.Remove(session);
        await db.SaveChangesAsync();
    }
}
