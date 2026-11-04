using HelpDeskLive.Application.Common.Models;
using MessagePack;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

var configuredApiKey = app.Configuration["NotificationApi:ApiKey"] ?? "HelpDeskLive-Notification-Secret-Key-2026";
var maxBodySize = app.Configuration.GetValue<long>("NotificationApi:MaxRequestBodySizeBytes", 65536);

app.MapPost("/api/notifications/ticket-closed", async (
    HttpRequest request,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    // 1. Authenticate caller via API Key
    if (!request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
        !string.Equals(apiKey.ToString(), configuredApiKey, StringComparison.Ordinal))
    {
        logger.LogWarning("Unauthorized attempt to access Notification API. Missing or invalid X-Api-Key.");
        return Results.Unauthorized();
    }

    // 2. Enforce maximum request body size
    if (request.ContentLength.HasValue && request.ContentLength.Value > maxBodySize)
    {
        logger.LogWarning("Request body exceeded maximum size: {Length} bytes.", request.ContentLength.Value);
        return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
    }

    var syncIOFeature = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
    if (syncIOFeature != null && !syncIOFeature.IsReadOnly)
    {
        syncIOFeature.MaxRequestBodySize = maxBodySize;
    }

    // 3. Read binary MessagePack body
    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms, ct);
    var bytes = ms.ToArray();

    if (bytes.Length == 0)
    {
        return Results.BadRequest(new { error = "Request payload cannot be empty." });
    }

    if (bytes.Length > maxBodySize)
    {
        return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
    }

    // 4. MessagePack Deserialization
    TicketClosedNotificationDto payload;
    try
    {
        payload = MessagePackSerializer.Deserialize<TicketClosedNotificationDto>(bytes, cancellationToken: ct);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to deserialize MessagePack payload.");
        return Results.BadRequest(new { error = "Malformed binary MessagePack payload." });
    }

    // 5. Payload Validation
    if (payload.TicketId <= 0 || string.IsNullOrWhiteSpace(payload.Title) || payload.ClosedAt == default)
    {
        logger.LogWarning("Invalid payload data: TicketId={TicketId}, Title='{Title}'", payload.TicketId, payload.Title);
        return Results.BadRequest(new { error = "Payload contains invalid ticket closure data." });
    }

    // 6. Meaningful notification delivery simulation & logging
    logger.LogInformation(
        "Successfully delivered ticket closure notification. Ticket #{TicketId} ('{Title}') closed at {ClosedAt}. Notified Customer #{CustomerId} and Agent #{AgentId}.",
        payload.TicketId,
        payload.Title,
        payload.ClosedAt,
        payload.CustomerId,
        payload.AgentId);

    // 7. Return JSON response confirming delivery
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

public partial class NotificationApiMarker { }
