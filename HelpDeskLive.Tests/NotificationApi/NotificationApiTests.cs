using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using HelpDeskLive.Application.Common.Models;
using MessagePack;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HelpDeskLive.Tests.NotificationApi;

public class NotificationApiTests : IClassFixture<WebApplicationFactory<NotificationApiMarker>>
{
    private readonly WebApplicationFactory<NotificationApiMarker> _factory;

    public NotificationApiTests(WebApplicationFactory<NotificationApiMarker> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_WithValidMessagePack_And_ValidApiKey_Returns200Json()
    {
        var client = _factory.CreateClient();

        var dto = new TicketClosedNotificationDto(
            TicketId: 101,
            Title: "VPN Connection Error",
            CustomerId: 55,
            AgentId: 7,
            ClosedAt: DateTime.UtcNow);

        var bytes = MessagePackSerializer.Serialize(dto);
        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/x-msgpack");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/notifications/ticket-closed");
        request.Content = content;
        request.Headers.Add("X-Api-Key", "HelpDeskLive-Notification-Secret-Key-2026");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetString().Should().Be("Delivered");
        doc.RootElement.GetProperty("ticketId").GetInt32().Should().Be(101);
        doc.RootElement.GetProperty("recipientCustomerId").GetInt32().Should().Be(55);
    }

    [Fact]
    public async Task Post_WithoutApiKey_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var dto = new TicketClosedNotificationDto(102, "Title", 1, 2, DateTime.UtcNow);
        var bytes = MessagePackSerializer.Serialize(dto);
        using var content = new ByteArrayContent(bytes);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/notifications/ticket-closed");
        request.Content = content;

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WithEmptyBody_Returns400BadRequest()
    {
        var client = _factory.CreateClient();

        using var content = new ByteArrayContent([]);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/notifications/ticket-closed");
        request.Content = content;
        request.Headers.Add("X-Api-Key", "HelpDeskLive-Notification-Secret-Key-2026");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
