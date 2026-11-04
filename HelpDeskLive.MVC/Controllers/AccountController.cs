
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
    private readonly IStringLocalizer<HelpDeskLive.MVC.SharedResource> _localizer;

    public AccountController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IStringLocalizer<HelpDeskLive.MVC.SharedResource> localizer)
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
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["ApiSettings:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(baseUrl) &&
            Request.Cookies.TryGetValue("helpdesk_refresh", out var refreshToken))
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/auth/logout");
                request.Headers.Add("Cookie", $"helpdesk_refresh={refreshToken}");
                await client.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException)
            {
                // Local session is still cleared; the refresh cookie expires naturally if the API is unavailable.
            }
        }

        HttpContext.Session.Clear();
        Response.Cookies.Delete("helpdesk_refresh", new CookieOptions { Path = "/", HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Strict });
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

            // Forward the API's HttpOnly refresh cookie to the browser. The API remains
            // the only component that can read or rotate the refresh token.
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var setCookie in setCookies.Where(value => value.StartsWith("helpdesk_refresh=", StringComparison.Ordinal)))
                    Response.Headers.Append("Set-Cookie", setCookie);
            }

            // Store the short-lived access token server-side in the MVC session.
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
