using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.MVC.Controllers;

public sealed class AdminTicketApprovalsController : Controller
{
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;
    public AdminTicketApprovalsController(IHttpClientFactory clients, IConfiguration configuration) { _clients = clients; _configuration = configuration; }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var client = Client();
        if (client is null) return RedirectToAction("Login", "Account");
        var pending = await client.GetAsync("api/admin/ticket-approvals/pending", ct);
        var deps = await client.GetAsync("api/admin/departments", ct);
        if (!pending.IsSuccessStatusCode || !deps.IsSuccessStatusCode) return StatusCode(502);
        return View(new AdminTicketApprovalsPageModel
        {
            Requests = await pending.Content.ReadFromJsonAsync<List<PendingTicketRequest>>(cancellationToken: ct) ?? [],
            Departments = await deps.Content.ReadFromJsonAsync<List<ApprovalDepartment>>(cancellationToken: ct) ?? []
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, int priority, int departmentId, CancellationToken ct)
    {
        var client = Client();
        if (client is null) return RedirectToAction("Login", "Account");
        var response = await client.PostAsJsonAsync($"api/admin/ticket-approvals/{id}/approve", new { Priority = priority, DepartmentId = departmentId }, ct);
        TempData[response.IsSuccessStatusCode ? "ApprovalSuccess" : "ApprovalError"] = response.IsSuccessStatusCode ? "Request approved." : "Could not approve the request.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, CancellationToken ct)
    {
        var client = Client();
        if (client is null) return RedirectToAction("Login", "Account");
        var response = await client.PostAsync($"api/admin/ticket-approvals/{id}/reject", null, ct);
        TempData[response.IsSuccessStatusCode ? "ApprovalSuccess" : "ApprovalError"] = response.IsSuccessStatusCode ? "Request rejected." : "Could not reject the request.";
        return RedirectToAction(nameof(Index));
    }

    private HttpClient? Client()
    {
        var token = HttpContext.Session.GetString("AccessToken");
        var url = _configuration["ApiSettings:BaseUrl"];
        if (string.IsNullOrWhiteSpace(token) || HttpContext.Session.GetString("UserRole") != "Admin" || string.IsNullOrWhiteSpace(url)) return null;
        var client = _clients.CreateClient();
        client.BaseAddress = new Uri(url);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public sealed class AdminTicketApprovalsPageModel
{
    public List<PendingTicketRequest> Requests { get; init; } = [];
    public List<ApprovalDepartment> Departments { get; init; } = [];
}
public sealed class PendingTicketRequest
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public int CustomerId { get; init; }
    public DateTime CreatedAt { get; init; }
}
public sealed class ApprovalDepartment
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}
