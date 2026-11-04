using System.Net.Http.Headers;
using System.Net.Http.Json;
using HelpDeskLive.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace HelpDeskLive.MVC.Controllers;

public sealed class CustomerController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CustomerController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IStringLocalizer<SharedResource> localizer)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

        if (HttpContext.Session.GetString("UserRole") != "Customer")
            return Forbid();

        var client = CreateApiClient(token);
        if (client is null)
            return StatusCode(500, _localizer["ApiBaseUrlMissing"].Value);

        var ticketsResponse = await client.GetAsync("api/customer/tickets", cancellationToken);
        if (!ticketsResponse.IsSuccessStatusCode)
            return StatusCode((int)ticketsResponse.StatusCode);

        var tickets = await ticketsResponse.Content
            .ReadFromJsonAsync<List<CustomerTicketViewModel>>(cancellationToken: cancellationToken)
            ?? new List<CustomerTicketViewModel>();

        var issueTypesResponse = await client.GetAsync("api/issue-types", cancellationToken);
        if (!issueTypesResponse.IsSuccessStatusCode)
            return StatusCode((int)issueTypesResponse.StatusCode);

        var issueTypes = await issueTypesResponse.Content
            .ReadFromJsonAsync<List<CustomerIssueTypeViewModel>>(cancellationToken: cancellationToken)
            ?? new List<CustomerIssueTypeViewModel>();

        var model = new CustomerDashboardViewModel
        {
            Tickets = tickets,
            IssueTypes = issueTypes
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTicket(
        CreateCustomerTicketViewModel model,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(HttpContext.Session.GetString("AccessToken")))
            return RedirectToAction("Login", "Account");

        if (HttpContext.Session.GetString("UserRole") != "Customer")
            return Forbid();

        if ((model.IssueTypeId is null && string.IsNullOrWhiteSpace(model.CustomTitle)) || (model.IssueTypeId is not null && model.IssueTypeId <= 0) || string.IsNullOrWhiteSpace(model.Description))
        {
            TempData["TicketError"] = _localizer["ChooseIssueAndDescription"].Value;
            return RedirectToAction(nameof(Index));
        }

        if (model.Description.Length > 5000 || (model.CustomTitle?.Length ?? 0) > 200)
        {
            TempData["TicketError"] = _localizer["TitleDescriptionTooLong"].Value;
            return RedirectToAction(nameof(Index));
        }

        var client = CreateApiClient(HttpContext.Session.GetString("AccessToken")!);
        if (client is null)
            return StatusCode(500, "API base URL is not configured.");

        var response = await client.PostAsJsonAsync(
            "api/tickets",
            new { model.IssueTypeId, model.CustomTitle, model.Description },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            TempData["TicketError"] = response.StatusCode == System.Net.HttpStatusCode.BadRequest
                ? _localizer["CheckIssueDescription"].Value
                : _localizer["TicketCreateFailed"].Value;
            return RedirectToAction(nameof(Index));
        }

        TempData["TicketSuccess"] = model.IssueTypeId is null ? _localizer["CustomRequestPending"].Value : _localizer["TicketCreatedSuccessfully"].Value;
        return RedirectToAction(nameof(Index));
    }

    private HttpClient? CreateApiClient(string token)
    {
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