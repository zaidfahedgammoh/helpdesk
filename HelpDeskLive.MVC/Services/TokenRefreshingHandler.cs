using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HelpDeskLive.MVC.Services;

public sealed class TokenRefreshingHandler : DelegatingHandler
{
    public const string RefreshClientName = "RefreshTokenClient";
    public const string RetryHeaderName = "X-HelpDesk-Retry";

    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TokenRefreshingHandler> _logger;

    public TokenRefreshingHandler(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<TokenRefreshingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 1. Buffer request body if present and not already retrying, so we can re-send on 401
        byte[]? bufferedContent = null;
        MediaTypeHeaderValue? contentType = null;
        if (request.Content != null && !request.Headers.Contains(RetryHeaderName))
        {
            bufferedContent = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            contentType = request.Content.Headers.ContentType;
            request.Content = new ByteArrayContent(bufferedContent);
            if (contentType != null)
            {
                request.Content.Headers.ContentType = contentType;
            }
        }

        // 2. Capture the initial authorization token
        var initialToken = request.Headers.Authorization?.Parameter;

        // 3. Send initial request
        var response = await base.SendAsync(request, cancellationToken);

        // 4. If not 401 Unauthorized, or if already a retry attempt, return immediately
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized ||
            request.Headers.Contains(RetryHeaderName))
        {
            return response;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return response;
        }

        string? refreshedAccessToken = null;

        // 5. Concurrency protection: serialize refresh attempts
        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Check if another concurrent thread already refreshed the token
            var currentSessionToken = httpContext.Session.GetString("AccessToken");
            if (!string.IsNullOrWhiteSpace(currentSessionToken) &&
                !string.Equals(currentSessionToken, initialToken, StringComparison.Ordinal))
            {
                refreshedAccessToken = currentSessionToken;
            }
            else
            {
                // Attempt refresh via dedicated non-recursive named client
                if (!httpContext.Request.Cookies.TryGetValue("helpdesk_refresh", out var refreshToken) ||
                    string.IsNullOrWhiteSpace(refreshToken))
                {
                    _logger.LogInformation("No refresh token cookie available. Session expired.");
                    return response;
                }

                var baseUrl = _configuration["ApiSettings:BaseUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    _logger.LogError("ApiSettings:BaseUrl is not configured.");
                    return response;
                }

                var refreshClient = _httpClientFactory.CreateClient(RefreshClientName);
                var refreshUri = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), "api/auth/refresh");

                using var refreshReq = new HttpRequestMessage(HttpMethod.Post, refreshUri);
                refreshReq.Headers.Add("Cookie", $"helpdesk_refresh={refreshToken}");

                using var refreshRes = await refreshClient.SendAsync(refreshReq, cancellationToken);

                if (refreshRes.IsSuccessStatusCode)
                {
                    using var doc = await refreshRes.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
                    var newAccessToken = doc?.RootElement.GetProperty("accessToken").GetString();

                    if (!string.IsNullOrWhiteSpace(newAccessToken))
                    {
                        refreshedAccessToken = newAccessToken;
                        httpContext.Session.SetString("AccessToken", newAccessToken);

                        // Propagate rotated refresh cookie to the user's browser
                        if (refreshRes.Headers.TryGetValues("Set-Cookie", out var setCookies))
                        {
                            foreach (var cookie in setCookies.Where(c => c.StartsWith("helpdesk_refresh=", StringComparison.OrdinalIgnoreCase)))
                            {
                                httpContext.Response.Headers.Append("Set-Cookie", cookie);
                            }
                        }

                        _logger.LogInformation("Successfully refreshed and rotated access token.");
                    }
                }
                else
                {
                    _logger.LogWarning("Refresh token request failed with status {StatusCode}. Terminating session.", refreshRes.StatusCode);
                    httpContext.Session.Clear();
                    httpContext.Response.Cookies.Delete("helpdesk_refresh");
                    return response;
                }
            }
        }
        finally
        {
            RefreshLock.Release();
        }

        // 6. Safe retry of original request with updated token
        if (!string.IsNullOrWhiteSpace(refreshedAccessToken))
        {
            // Dispose the 401 response before retrying
            response.Dispose();

            using var retryReq = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
            {
                if (!string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(header.Key, RetryHeaderName, StringComparison.OrdinalIgnoreCase))
                {
                    retryReq.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            retryReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedAccessToken);
            retryReq.Headers.Add(RetryHeaderName, "1");

            if (bufferedContent != null)
            {
                retryReq.Content = new ByteArrayContent(bufferedContent);
                if (contentType != null)
                {
                    retryReq.Content.Headers.ContentType = contentType;
                }
            }

            return await base.SendAsync(retryReq, cancellationToken);
        }

        return response;
    }
}
