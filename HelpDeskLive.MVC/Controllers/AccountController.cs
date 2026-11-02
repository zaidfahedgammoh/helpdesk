
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        var supportedCultures = new[] { "en", "ar" };

        if (!supportedCultures.Contains(culture))
        {
            culture = "en";
        }

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax
            });

        if (Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return RedirectToAction("Login", "Account");
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

            // Read the user's role from the JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(accessToken);

            var role = jwtToken.Claims
                .FirstOrDefault(c =>
                    c.Type == ClaimTypes.Role ||
                    c.Type == "role" ||
                    c.Type == "roles")
                ?.Value;

            if (string.IsNullOrWhiteSpace(role))
            {
                ViewBag.Error = "The access token does not contain a user role.";
                return View();
            }

            // Store the access token in the session
            HttpContext.Session.SetString("AccessToken", accessToken);
            HttpContext.Session.SetString("UserRole", role);

            // Redirect according to the user's role
            return role switch
            {
                "Admin" => RedirectToAction("Index", "Admin"),
                "Agent" => RedirectToAction("Index", "Agent"),
                "Customer" => RedirectToAction("Index", "Customer"),
                _ => RedirectToAction("Index", "Home")
            };
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
        catch (ArgumentException)
        {
            ViewBag.Error = "The API returned an invalid access token.";
            return View();
        }
    }
}
