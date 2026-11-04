using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using HelpDeskLive.Application.Chat;
using MessagePack;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BenchmarkController : ControllerBase
{
    [HttpGet("messagepack")]
    public IActionResult CompareSerialization(int messageCount = 100)
    {
        var messages = Enumerable.Range(1, messageCount)
            .Select(i => new ChatMessageDto(
                i,
                101,
                (i % 2 == 0) ? 10 : 20,
                (i % 2 == 0) ? "Customer User" : "Support Agent",
                $"Support message #{i}: Detailed diagnosis regarding ticket lifecycle and SignalR real-time synchronization.",
                DateTime.UtcNow.AddMinutes(-messageCount + i)
            ))
            .ToList();

        var jsonTimer = Stopwatch.StartNew();
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(messages);
        jsonTimer.Stop();

        var msgPackTimer = Stopwatch.StartNew();
        var msgPackBytes = MessagePackSerializer.Serialize(messages);
        msgPackTimer.Stop();

        using var brotliMs = new MemoryStream();
        using (var brotliStream = new BrotliStream(brotliMs, CompressionLevel.Optimal, leaveOpen: true))
        {
            brotliStream.Write(msgPackBytes, 0, msgPackBytes.Length);
        }
        var brotliBytes = brotliMs.ToArray();

        var reductionPercentage = Math.Round((1.0 - ((double)msgPackBytes.Length / jsonBytes.Length)) * 100, 2);
        var brotliReductionPercentage = Math.Round((1.0 - ((double)brotliBytes.Length / jsonBytes.Length)) * 100, 2);

        return Ok(new
        {
            MessageCount = messageCount,
            JsonSizeBytes = jsonBytes.Length,
            JsonElapsedMs = jsonTimer.Elapsed.TotalMilliseconds,
            MessagePackSizeBytes = msgPackBytes.Length,
            MessagePackElapsedMs = msgPackTimer.Elapsed.TotalMilliseconds,
            MessagePackSizeReductionPercent = reductionPercentage,
            BrotliCompressedSizeBytes = brotliBytes.Length,
            BrotliSizeReductionPercent = brotliReductionPercentage
        });
    }
}
