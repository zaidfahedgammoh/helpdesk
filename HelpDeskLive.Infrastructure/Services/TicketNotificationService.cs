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
    private readonly ILogger<TicketNotificationService> _logger;

    public TicketNotificationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TicketNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _notificationUrl = configuration["NotificationApi:Url"] ?? "http://localhost:5005/api/notifications/ticket-closed";
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

            using var response = await _httpClient.PostAsync(_notificationUrl, content, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Notified external Notification API of ticket #{TicketId} closure.", ticket.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not reach Notification API for ticket #{TicketId}.", ticket.Id);
        }
    }
}
