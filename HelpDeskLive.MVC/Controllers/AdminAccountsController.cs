using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.MVC.Controllers;

public sealed class AdminAccountsController : Controller
{
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;
    public AdminAccountsController(IHttpClientFactory clients, IConfiguration configuration) { _clients = clients; _configuration = configuration; }

    [HttpGet]
    public async Task<IActionResult> Index(string role = "Customer", CancellationToken ct = default)
    {
        if (role is not ("Customer" or "Agent")) role = "Customer";
        var client = CreateClient();
        if (client is null) return RedirectToAction("Login", "Account");
        var response = await client.GetAsync($"api/admin/accounts/{role}", ct);
        if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);
        var accounts = await response.Content.ReadFromJsonAsync<List<AdminAccountRow>>(cancellationToken: ct) ?? [];
        var deptResponse = await client.GetAsync("api/admin/departments", ct);
        var departments = deptResponse.IsSuccessStatusCode
            ? await deptResponse.Content.ReadFromJsonAsync<List<AdminDepartmentOption>>(cancellationToken: ct) ?? []
            : [];
        return View(new AdminAccountsPageModel { Role = role, Accounts = accounts, Departments = departments });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminCreateAccountModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid || (model.Role == "Agent" && model.DepartmentId is null))
        {
            TempData["AccountError"] = "Complete all fields and choose a department for the Agent.";
            return RedirectToAction(nameof(Index), new { role = model.Role });
        }
        var client = CreateClient();
        if (client is null) return RedirectToAction("Login", "Account");
        var response = await client.PostAsJsonAsync("api/admin/accounts", new
        { model.Name, model.Email, model.Password, model.Role, model.DepartmentId }, ct);
        TempData[response.IsSuccessStatusCode ? "AccountSuccess" : "AccountError"] =
            response.IsSuccessStatusCode ? $"{model.Role} account created." :
            response.StatusCode == System.Net.HttpStatusCode.Conflict ? "That email address is already registered." :
            "Account could not be created. Check the entered details and department.";
        return RedirectToAction(nameof(Index), new { role = model.Role });
    }

    private HttpClient? CreateClient()
    {
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token) || HttpContext.Session.GetString("UserRole") != "Admin") return null;
        var url = _configuration["ApiSettings:BaseUrl"];
        if (string.IsNullOrWhiteSpace(url)) return null;
        var client = _clients.CreateClient();
        client.BaseAddress = new Uri(url);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public sealed class AdminAccountsPageModel
{
    public string Role { get; init; } = "Customer";
    public List<AdminAccountRow> Accounts { get; init; } = [];
    public List<AdminDepartmentOption> Departments { get; init; } = [];
}
public sealed class AdminAccountRow
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string Role { get; init; } = "";
    public int? DepartmentId { get; init; }
}
public sealed class AdminDepartmentOption { public int Id { get; init; } public string Name { get; init; } = ""; }
public sealed class AdminCreateAccountModel
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.StringLength(100)]
    public string Name { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress, System.ComponentModel.DataAnnotations.StringLength(256)]
    public string Email { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MinLength(8)]
    public string Password { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Required]
    public string Role { get; set; } = "Customer";
    public int? DepartmentId { get; set; }
}
