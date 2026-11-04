using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using HelpDeskLive.MVC.Models;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace HelpDeskLive.MVC.Controllers;

public sealed class AgentTicketsController : Controller
{
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<HelpDeskLive.MVC.SharedResource> _localizer;
    public AgentTicketsController(IHttpClientFactory clients, IConfiguration configuration, IStringLocalizer<HelpDeskLive.MVC.SharedResource> localizer) { _clients = clients; _configuration = configuration; _localizer = localizer; }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var client = Client();
        if (client is null) return RedirectToAction("Login", "Account");
        var response = await client.GetAsync("api/agent/tickets", ct);
        if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);
        var rows = await response.Content.ReadFromJsonAsync<List<AgentTicketRow>>(cancellationToken: ct) ?? [];
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Start(int id, CancellationToken ct) => Send(id, "start", ct);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Resolve(int id, CancellationToken ct) => Send(id, "resolve", ct);
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Close(int id, CancellationToken ct) => Send(id, "close", ct);

    [HttpGet]
    public async Task<IActionResult> Chat(int id, CancellationToken ct)
    {
        var client = Client();
        if (client is null) return RedirectToAction("Login", "Account");
        var token = HttpContext.Session.GetString("AccessToken")!;
        var response = await client.GetAsync($"api/chat/{id}/messages", ct);
        if (!response.IsSuccessStatusCode)
        {
            TempData["AgentTicketError"] = _localizer["ChatLoadError"].Value;
            return RedirectToAction(nameof(Index));
        }

        return View("~/Views/Shared/TicketChat.cshtml", new ChatPageViewModel
        {
            TicketId = id,
            CurrentUserId = ReadUserId(token),
            AccessToken = token,
            ApiBaseUrl = _configuration["ApiSettings:BaseUrl"] ?? string.Empty,
            IsAgent = true
        });
    }

    private async Task<IActionResult> Send(int id, string action, CancellationToken ct)
    {
        var client = Client();
        if (client is null) return RedirectToAction("Login", "Account");
        var response = await client.PostAsync($"api/agent/tickets/{id}/{action}", null, ct);
        if (response.IsSuccessStatusCode && action == "start")
        {
            using var result = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: ct);
            if (result?.RootElement.TryGetProperty("isLiveChat", out var isLiveChat) == true && isLiveChat.GetBoolean())
                return RedirectToAction(nameof(Chat), new { id });
        }

        TempData[response.IsSuccessStatusCode ? "AgentTicketSuccess" : "AgentTicketError"] =
            response.IsSuccessStatusCode ? _localizer["TicketUpdatedShort"].Value : _localizer["TicketCouldNotBeUpdated"].Value;
        return RedirectToAction(nameof(Index));
    }

    private static int ReadUserId(string token)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var value = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid" || c.Type == "sub")?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    }

    private HttpClient? Client()
    {
        var token = HttpContext.Session.GetString("AccessToken");
        var url = _configuration["ApiSettings:BaseUrl"];
        if (string.IsNullOrWhiteSpace(token) || HttpContext.Session.GetString("UserRole") != "Agent" || string.IsNullOrWhiteSpace(url)) return null;
        var client = _clients.CreateClient();
        client.BaseAddress = new Uri(url);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public sealed class AgentTicketRow
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public int Status { get; init; }
    public int? Priority { get; init; }
    public int? DepartmentId { get; init; }
    public DateTime CreatedAt { get; init; }
    public int? AssignedAgentId { get; init; }
}
