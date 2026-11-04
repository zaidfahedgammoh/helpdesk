using System.Net.Http.Headers;
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Common.Models;
using HelpDeskLive.Domain.Entities;
using MessagePack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HelpDeskLive.Infrastructure.Services;

public sealed class TicketNotificationService : ITicketNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly string _notificationUrl;
    private readonly string _apiKey;
    private readonly ILogger<TicketNotificationService> _logger;

    public TicketNotificationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TicketNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _notificationUrl = configuration["NotificationApi:Url"] ?? "http://localhost:5005/api/notifications/ticket-closed";
        _apiKey = configuration["NotificationApi:ApiKey"] ?? "HelpDeskLive-Notification-Secret-Key-2026";
    }

    public async Task NotifyTicketClosedAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = new TicketClosedNotificationDto(
                ticket.Id,
                ticket.Title,
                ticket.CustomerId,
                ticket.AssignedAgentId,
                ticket.ClosedAt ?? DateTime.UtcNow);

            var bytes = MessagePackSerializer.Serialize(dto);

            using var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-msgpack");

            using var request = new HttpRequestMessage(HttpMethod.Post, _notificationUrl);
            request.Content = content;
            request.Headers.Add("X-Api-Key", _apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Successfully notified Notification API for ticket #{TicketId}. Response: {Response}", ticket.Id, responseBody);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Notification API returned unsuccessful status {StatusCode} for ticket #{TicketId}. Details: {Details}",
                    response.StatusCode,
                    ticket.Id,
                    errorBody);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Notification delivery for ticket #{TicketId} was cancelled.", ticket.Id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to external Notification API for ticket #{TicketId}.", ticket.Id);
        }
    }
}
