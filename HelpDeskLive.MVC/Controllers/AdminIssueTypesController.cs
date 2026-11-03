using System.Net.Http.Headers;
using System.Net.Http.Json;
using HelpDeskLive.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.MVC.Controllers;

public sealed class AdminIssueTypesController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AdminIssueTypesController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var client = CreateAdminClient();
        if (client is null)
            return StatusCode(500, "API base URL is not configured.");

        var issueTypesResponse = await client.GetAsync("api/issue-types", cancellationToken);
        if (!issueTypesResponse.IsSuccessStatusCode)
            return StatusCode((int)issueTypesResponse.StatusCode);

        var departmentsResponse = await client.GetAsync("api/admin/departments", cancellationToken);
        if (!departmentsResponse.IsSuccessStatusCode)
            return StatusCode((int)departmentsResponse.StatusCode);

        var model = new AdminIssueTypesViewModel
        {
            IssueTypes = await issueTypesResponse.Content.ReadFromJsonAsync<List<AdminIssueTypeItem>>(cancellationToken: cancellationToken) ?? [],
            Departments = await departmentsResponse.Content.ReadFromJsonAsync<List<AdminDepartmentItem>>(cancellationToken: cancellationToken) ?? []
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AdminCreateIssueTypeModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Index));

        var client = CreateAdminClient();
        if (client is null)
            return StatusCode(500, "API base URL is not configured.");

        var response = await client.PostAsJsonAsync(
            "api/issue-types",
            new
            {
                model.Name,
                model.Description,
                model.DepartmentId,
                DefaultPriority = model.DefaultPriority
            },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            TempData["IssueTypeError"] = "The issue type could not be created. Check the details and try again.";
        else
            TempData["IssueTypeSuccess"] = "Issue type created successfully.";

        return RedirectToAction(nameof(Index));
    }

    private HttpClient? CreateAdminClient()
    {
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token))
            return null;

        if (HttpContext.Session.GetString("UserRole") != "Admin")
            return null;

        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            return null;

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(apiBaseUrl);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}

public sealed class AdminIssueTypesViewModel
{
    public List<AdminIssueTypeItem> IssueTypes { get; init; } = [];
    public List<AdminDepartmentItem> Departments { get; init; } = [];
}

public sealed class AdminIssueTypeItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int DepartmentId { get; init; }
    public int DefaultPriority { get; init; }
}

public sealed class AdminDepartmentItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class AdminCreateIssueTypeModel
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    [System.ComponentModel.DataAnnotations.Range(1, 4)]
    public int DefaultPriority { get; set; } = 2;
}
