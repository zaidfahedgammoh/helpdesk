using System.Net.Http.Headers;
using System.Net.Http.Json;
using HelpDeskLive.MVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.MVC.Controllers;

public sealed class CustomerController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public CustomerController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("AccessToken");
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Account");

        if (HttpContext.Session.GetString("UserRole") != "Customer")
            return Forbid();

        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            return StatusCode(500, "API base URL is not configured.");

        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(apiBaseUrl);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

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
}