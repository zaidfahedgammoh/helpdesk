
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HelpDeskLive.MVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.MVC.Controllers;

public class AgentController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AgentController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var token = HttpContext.Session.GetString("AccessToken");

        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction("Login", "Account");
        }

        var role = HttpContext.Session.GetString("UserRole");

        if (role != "Agent")
        {
            return Forbid();
        }

        var apiBaseUrl = _configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            return StatusCode(500, "API base URL is not configured.");
        }

        var client = _httpClientFactory.CreateClient();

        client.BaseAddress = new Uri(apiBaseUrl);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(
            "api/agent/dashboard",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var dashboard = await response.Content
            .ReadFromJsonAsync<AgentDashboardViewModel>(
                cancellationToken: cancellationToken);

        if (dashboard is null)
        {
            return StatusCode(
                500,
                "Could not load Agent dashboard statistics.");
        }

        return View(dashboard);
    }
}
