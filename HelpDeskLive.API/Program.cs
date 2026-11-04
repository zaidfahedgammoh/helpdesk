using System.Text;
using HelpDeskLive.API.Authentication;
using HelpDeskLive.API.Hubs;
using HelpDeskLive.API.Middleware;
using HelpDeskLive.API.Services;
using HelpDeskLive.Application;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Infrastructure;
using HelpDeskLive.Infrastructure.Data;
using HelpDeskLive.Infrastructure.Data.Seeding;
using MessagePack;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSignalR()
    .AddMessagePackProtocol();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length == 0)
    throw new InvalidOperationException("At least one CORS origin must be configured in Cors:AllowedOrigins.");

builder.Services.AddCors(options => options.AddPolicy("MvcClient", policy => policy
    .WithOrigins(allowedOrigins)
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
    ?? throw new InvalidOperationException("JWT issuer is not configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is not configured.");
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("JWT signing key is not configured. Set Jwt__SigningKey through a secret store or environment variable.");

var signingKeyBytes = Encoding.UTF8.GetBytes(jwtSigningKey);
if (signingKeyBytes.Length < 32)
    throw new InvalidOperationException("JWT signing key must be at least 32 bytes long.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();

var applyMigrations = app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup", app.Environment.IsDevelopment());
if (applyMigrations)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
    await dbContext.Database.MigrateAsync();
    await ReferenceDataSeeder.SeedAsync(dbContext);
    if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("SeedTestUsers:Enabled"))
        await TestUserSeeder.SeedAsync(dbContext, builder.Configuration);
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("MvcClient");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<TicketChatHub>("/hubs/chat").RequireAuthorization();

app.MapPost("/api/notifications/ticket-closed", async (HttpRequest request) =>
{
    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms);
    var bytes = ms.ToArray();
    if (bytes.Length == 0) return Results.BadRequest(new { error = "Empty payload" });
    var notification = MessagePackSerializer.Deserialize<HelpDeskLive.Application.Common.Models.TicketClosedNotificationDto>(bytes);
    return Results.Ok(notification);
});

app.Run();

namespace HelpDeskLive.API
{
    public class ApiMarker { }
}
