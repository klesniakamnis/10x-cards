using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using _10x_cards.Auth;
using _10x_cards.Data;
using _10x_cards.Endpoints;
using _10x_cards.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "db", "10xcards.db");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();
builder.Services.AddSingleton<ITicketStore, DbSessionStore>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<ITicketStore>((options, store) =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.SessionStore = store;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

var dbDir = Path.Combine(app.Environment.ContentRootPath, "db");
Directory.CreateDirectory(dbDir);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/health", () => Results.Ok("healthy"))
    .WithName("HealthCheck")
    .AllowAnonymous();

app.MapGet("/db-health", async (ApplicationDbContext db, ILogger<Program> logger) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        return canConnect
            ? Results.Ok(new { status = "healthy", database = "connected" })
            : Results.Json(new { status = "unhealthy", database = "cannot connect" }, statusCode: 503);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database health check failed");
        return Results.Json(new { status = "unhealthy", database = "error" }, statusCode: 503);
    }
})
.WithName("DbHealthCheck")
.AllowAnonymous();

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapAuthEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
