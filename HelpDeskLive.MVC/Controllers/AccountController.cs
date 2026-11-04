
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace HelpDeskLive.MVC.Controllers;

public class AccountController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AccountController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IStringLocalizer<SharedResource> localizer)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _localizer = localizer;
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
            ViewBag.Error = _localizer["PleaseEnterCredentials"].Value;
            return View();
        }

        var baseUrl = _configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            ViewBag.Error = _localizer["ApiNotConfigured"].Value;
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
                ViewBag.Error = _localizer["LoginInvalidCredentials"].Value;
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
                ViewBag.Error = _localizer["ApiDidNotReturnToken"].Value;
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
                ViewBag.Error = _localizer["TokenMissingRole"].Value;
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
            ViewBag.Error = _localizer["ApiConnectionFailed"].Value;
            return View();
        }
        catch (JsonException)
        {
            ViewBag.Error = _localizer["ApiInvalidResponse"].Value;
            return View();
        }
        catch (ArgumentException)
        {
            ViewBag.Error = _localizer["ApiReturnedInvalidToken"].Value;
            return View();
        }
    }
}
