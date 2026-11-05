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
using Microsoft.AspNetCore.Http.Features;
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

var notificationApiKey = app.Configuration["NotificationApi:ApiKey"] ?? "HelpDeskLive-Notification-Secret-Key-2026";
var maxNotificationBodySize = app.Configuration.GetValue<long>("NotificationApi:MaxRequestBodySizeBytes", 65536);

app.MapPost("/api/notifications/ticket-closed", async (
    HttpRequest request,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    if (!request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
        !string.Equals(apiKey.ToString(), notificationApiKey, StringComparison.Ordinal))
    {
        logger.LogWarning("Unauthorized attempt to access Notification endpoint. Missing or invalid X-Api-Key.");
        return Results.Unauthorized();
    }

    if (request.ContentLength.HasValue && request.ContentLength.Value > maxNotificationBodySize)
    {
        logger.LogWarning("Request body exceeded maximum size: {Length} bytes.", request.ContentLength.Value);
        return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
    }

    var syncIOFeature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (syncIOFeature != null && !syncIOFeature.IsReadOnly)
    {
        syncIOFeature.MaxRequestBodySize = maxNotificationBodySize;
    }

    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms, ct);
    var bytes = ms.ToArray();

    if (bytes.Length == 0)
    {
        return Results.BadRequest(new { error = "Request payload cannot be empty." });
    }

    if (bytes.Length > maxNotificationBodySize)
    {
        return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
    }

    HelpDeskLive.Application.Common.Models.TicketClosedNotificationDto payload;
    try
    {
        payload = MessagePackSerializer.Deserialize<HelpDeskLive.Application.Common.Models.TicketClosedNotificationDto>(bytes, cancellationToken: ct);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to deserialize MessagePack payload.");
        return Results.BadRequest(new { error = "Malformed binary MessagePack payload." });
    }

    if (payload.TicketId <= 0 || string.IsNullOrWhiteSpace(payload.Title) || payload.ClosedAt == default)
    {
        logger.LogWarning("Invalid payload data: TicketId={TicketId}, Title='{Title}'", payload.TicketId, payload.Title);
        return Results.BadRequest(new { error = "Payload contains invalid ticket closure data." });
    }

    logger.LogInformation(
        "Successfully delivered ticket closure notification. Ticket #{TicketId} ('{Title}') closed at {ClosedAt}. Notified Customer #{CustomerId} and Agent #{AgentId}.",
        payload.TicketId,
        payload.Title,
        payload.ClosedAt,
        payload.CustomerId,
        payload.AgentId);

    return Results.Ok(new
    {
        status = "Delivered",
        ticketId = payload.TicketId,
        title = payload.Title,
        recipientCustomerId = payload.CustomerId,
        channel = "InApp & Dispatch Email Notification Service",
        deliveredAt = DateTime.UtcNow
    });
});

app.Run();

namespace HelpDeskLive.API
{
    public class ApiMarker { }
}
