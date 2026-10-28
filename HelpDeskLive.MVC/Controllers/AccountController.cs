
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskLive.MVC.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AccountController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Please enter your email and password.";
            return View();
        }

        var baseUrl = _configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            ViewBag.Error = "API URL is not configured.";
            return View();
        }

        var client = _httpClientFactory.CreateClient();

        try
        {
            var response = await client.PostAsJsonAsync(
                $"{baseUrl.TrimEnd('/')}/api/auth/login",
                new { Email = email, Password = password },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Invalid email or password.";
                return View();
            }

            using var result = await response.Content.ReadFromJsonAsync<JsonDocument>(
                cancellationToken: cancellationToken);

            var accessToken = result?
                .RootElement
                .GetProperty("accessToken")
                .GetString();

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                ViewBag.Error = "The API did not return an access token.";
                return View();
            }

            HttpContext.Session.SetString("AccessToken", accessToken);

            return RedirectToAction("Index", "Home");
        }
        catch (HttpRequestException)
        {
            ViewBag.Error = "Could not connect to the API. Make sure it is running.";
            return View();
        }
        catch (JsonException)
        {
            ViewBag.Error = "The API returned an invalid response.";
            return View();
        }
    }
}
