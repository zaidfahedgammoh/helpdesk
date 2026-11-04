
using System.Text;
using HelpDeskLive.API.Authentication;
using HelpDeskLive.API.Services;
using HelpDeskLive.Application;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Infrastructure;
using HelpDeskLive.Infrastructure.Data;
using HelpDeskLive.Infrastructure.Data.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using HelpDeskLive.API.Hubs;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options => options.AddPolicy("MvcClient", policy => policy
    .WithOrigins("https://localhost:7082", "http://localhost:5098")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<PasswordService>();

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience is not configured.");

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException(
        "JWT signing key is not configured.");

var signingKeyBytes = Encoding.UTF8.GetBytes(jwtSigningKey);

if (signingKeyBytes.Length < 32)
{
    throw new InvalidOperationException(
        "JWT signing key must be at least 32 bytes long.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(signingKeyBytes),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<HelpDeskDbContext>();

    await dbContext.Database.MigrateAsync();

    await ReferenceDataSeeder.SeedAsync(dbContext);
    await TestUserSeeder.SeedAsync(dbContext);
}

app.UseHttpsRedirection();

app.UseCors("MvcClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TicketChatHub>("/hubs/chat").RequireAuthorization();

var summaries = new[]
{
    "Freezing",
    "Bracing",
    "Chilly",
    "Cool",
    "Mild",
    "Warm",
    "Balmy",
    "Hot",
    "Sweltering",
    "Scorching"
};

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
});

app.Run();

internal record WeatherForecast(
    DateOnly Date,
    int TemperatureC,
    string? Summary)
{
    public int TemperatureF =>
        32 + (int)(TemperatureC / 0.5556);
}
