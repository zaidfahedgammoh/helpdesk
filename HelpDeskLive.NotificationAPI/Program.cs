using MessagePack;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/api/notifications/ticket-closed", async (HttpRequest request) =>
{
    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms);
    var bytes = ms.ToArray();
    if (bytes.Length == 0)
    {
        return Results.BadRequest(new { error = "Payload cannot be empty." });
    }

    var notification = MessagePackSerializer.Deserialize<TicketClosedPayload>(bytes);
    return Results.Ok(notification);
});

app.MapGet("/", () => "Notification API is running.");

app.Run();

[MessagePackObject]
public sealed record TicketClosedPayload(
    [property: Key(0)] int TicketId,
    [property: Key(1)] string Title,
    [property: Key(2)] int CustomerId,
    [property: Key(3)] int? AgentId,
    [property: Key(4)] DateTime ClosedAt
);
