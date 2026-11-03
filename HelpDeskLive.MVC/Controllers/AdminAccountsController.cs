using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.MVC.Controllers;

public sealed class AdminAccountsController : Controller
{
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;

    public AdminAccountsController(IHttpClientFactory clients, IConfiguration configuration)
    {
        _clients = clients;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string role = "Customer", CancellationToken cancellationToken = default)
    {
        if (role is not ("Customer" or "Agent"))
            role = "Customer";

        var client = CreateClient();
        if (client is null)
            return RedirectToAction("Login", "Account");

        var response = await client.GetAsync($"api/admin/accounts/{role}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode);

        var accounts = await response.Content.ReadFromJsonAsync<List<AdminAccountRow>>(
            cancellationToken: cancellationToken) ?? [];

        return View(new AdminAccountsPageModel { Role = role, Accounts = accounts });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminCreateAccountModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["AccountError"] = "Please complete all fields. Password must be at least 8 characters.";
            return RedirectToAction(nameof(Index), new { role = model.Role });
        }

        var client = CreateClient();
        if (client is null)
            return RedirectToAction("Login", "Account");

        var response = await client.PostAsJsonAsync("api/admin/accounts", new
        {
            model.Name,
            model.Email,
            model.Password,
            model.Role
        }, cancellationToken);

        if (response.IsSuccessStatusCode)
            TempData["AccountSuccess"] = $"{model.Role} account created.";
        else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            TempData["AccountError"] = "That email address is already registered.";
        else
            TempData["AccountError"] = "Account could not be created. Check the entered details.";

        return RedirectToAction(nameof(Index), new { role = model.Role });
    }

    private HttpClient? CreateClient()
    {
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token) ||
            HttpContext.Session.GetString("UserRole") != "Admin")
            return null;

        var baseUrl = _configuration["ApiSettings:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        var client = _clients.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public sealed class AdminAccountsPageModel
{
    public string Role { get; init; } = "Customer";
    public List<AdminAccountRow> Accounts { get; init; } = [];
}

public sealed class AdminAccountRow
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public sealed class AdminCreateAccountModel
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.EmailAddress]
    [System.ComponentModel.DataAnnotations.StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public string Role { get; set; } = "Customer";
}
