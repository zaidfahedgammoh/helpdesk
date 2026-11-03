using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.MVC.Controllers;

public sealed class AgentTicketsController : Controller
{
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;
    public AgentTicketsController(IHttpClientFactory clients, IConfiguration configuration) { _clients = clients; _configuration = configuration; }

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

    private async Task<IActionResult> Send(int id, string action, CancellationToken ct)
    {
        var client = Client();
        if (client is null) return RedirectToAction("Login", "Account");
        var response = await client.PostAsync($"api/agent/tickets/{id}/{action}", null, ct);
        TempData[response.IsSuccessStatusCode ? "AgentTicketSuccess" : "AgentTicketError"] =
            response.IsSuccessStatusCode ? "Ticket updated." : "Ticket could not be updated. Check its current status and your workload limit.";
        return RedirectToAction(nameof(Index));
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
}
