using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace HelpDeskLive.MVC.Services;

public sealed class TokenRefreshingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public TokenRefreshingHandler(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null &&
                httpContext.Request.Cookies.TryGetValue("helpdesk_refresh", out var refreshToken) &&
                !string.IsNullOrWhiteSpace(refreshToken))
            {
                var baseUrl = _configuration["ApiSettings:BaseUrl"];
                if (!string.IsNullOrWhiteSpace(baseUrl))
                {
                    using var refreshClient = _httpClientFactory.CreateClient();
                    using var refreshReq = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/auth/refresh");
                    refreshReq.Headers.Add("Cookie", $"helpdesk_refresh={refreshToken}");

                    var refreshRes = await refreshClient.SendAsync(refreshReq, cancellationToken);
                    if (refreshRes.IsSuccessStatusCode)
                    {
                        using var doc = await refreshRes.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
                        var newAccessToken = doc?.RootElement.GetProperty("accessToken").GetString();
                        if (!string.IsNullOrWhiteSpace(newAccessToken))
                        {
                            httpContext.Session.SetString("AccessToken", newAccessToken);

                            if (refreshRes.Headers.TryGetValues("Set-Cookie", out var setCookies))
                            {
                                foreach (var cookie in setCookies.Where(c => c.StartsWith("helpdesk_refresh=", StringComparison.Ordinal)))
                                {
                                    httpContext.Response.Headers.Append("Set-Cookie", cookie);
                                }
                            }

                            var retryReq = new HttpRequestMessage(request.Method, request.RequestUri);
                            foreach (var header in request.Headers)
                            {
                                if (header.Key != "Authorization")
                                {
                                    retryReq.Headers.TryAddWithoutValidation(header.Key, header.Value);
                                }
                            }
                            retryReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
                            retryReq.Content = request.Content;

                            return await base.SendAsync(retryReq, cancellationToken);
                        }
                    }
                }
            }
        }

        return response;
    }
}
