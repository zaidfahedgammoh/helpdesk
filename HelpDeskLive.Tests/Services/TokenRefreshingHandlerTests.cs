using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HelpDeskLive.MVC.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace HelpDeskLive.Tests.Services;

public class TokenRefreshingHandlerTests
{
    private class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();
        public bool IsAvailable => true;
        public string Id => "test-session";
        public IEnumerable<string> Keys => _store.Keys;
        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Handler { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Handler(request));
        }
    }

    [Fact]
    public async Task SendAsync_On401_CallsRefreshWithNamedClient_And_RetriesWithNewToken()
    {
        var httpContext = new DefaultHttpContext();
        var session = new TestSession();
        httpContext.Session = session;
        httpContext.Session.SetString("AccessToken", "expired-token");
        httpContext.Request.Headers.Cookie = "helpdesk_refresh=valid-refresh-token";

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["ApiSettings:BaseUrl"]).Returns("http://localhost:5019");

        // Mock Refresh Client
        var refreshHandler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                req.RequestUri!.ToString().Should().Contain("/api/auth/refresh");
                var res = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { accessToken = "new-fresh-token" }), Encoding.UTF8, "application/json")
                };
                res.Headers.Add("Set-Cookie", "helpdesk_refresh=rotated-refresh-token; Path=/; HttpOnly");
                return res;
            }
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(TokenRefreshingHandler.RefreshClientName))
            .Returns(new HttpClient(refreshHandler));

        var attempts = 0;
        var backendHandler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                attempts++;
                if (attempts == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }

                req.Headers.Authorization?.Parameter.Should().Be("new-fresh-token");
                req.Headers.Contains(TokenRefreshingHandler.RetryHeaderName).Should().BeTrue();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("Success!")
                };
            }
        };

        var handler = new TokenRefreshingHandler(
            accessorMock.Object,
            configMock.Object,
            factoryMock.Object,
            NullLogger<TokenRefreshingHandler>.Instance)
        {
            InnerHandler = backendHandler
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5019/api/agent/tickets");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "expired-token");

        var response = await invoker.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attempts.Should().Be(2);
        httpContext.Session.GetString("AccessToken").Should().Be("new-fresh-token");
    }

    [Fact]
    public async Task SendAsync_On401_PreservesRequestBody_OnRetry()
    {
        var httpContext = new DefaultHttpContext();
        var session = new TestSession();
        httpContext.Session = session;
        httpContext.Session.SetString("AccessToken", "expired-token");
        httpContext.Request.Headers.Cookie = "helpdesk_refresh=valid-refresh";

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["ApiSettings:BaseUrl"]).Returns("http://localhost:5019");

        var refreshHandler = new MockHttpMessageHandler
        {
            Handler = req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { accessToken = "token-v2" }), Encoding.UTF8, "application/json")
            }
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(TokenRefreshingHandler.RefreshClientName))
            .Returns(new HttpClient(refreshHandler));

        string? receivedBodyOnRetry = null;
        var attempts = 0;
        var backendHandler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                attempts++;
                if (attempts == 1) return new HttpResponseMessage(HttpStatusCode.Unauthorized);

                receivedBodyOnRetry = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        };

        var handler = new TokenRefreshingHandler(
            accessorMock.Object,
            configMock.Object,
            factoryMock.Object,
            NullLogger<TokenRefreshingHandler>.Instance)
        {
            InnerHandler = backendHandler
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5019/api/chat/1/messages")
        {
            Content = new StringContent("{\"content\":\"Important message\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "expired-token");

        var response = await invoker.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        attempts.Should().Be(2);
        receivedBodyOnRetry.Should().Be("{\"content\":\"Important message\"}");
    }

    [Fact]
    public async Task SendAsync_OnFailedRefresh_ClearsSession_AndDoesNotLoop()
    {
        var httpContext = new DefaultHttpContext();
        var session = new TestSession();
        httpContext.Session = session;
        httpContext.Session.SetString("AccessToken", "bad-token");
        httpContext.Request.Headers.Cookie = "helpdesk_refresh=invalid-refresh";

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(a => a.HttpContext).Returns(httpContext);

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["ApiSettings:BaseUrl"]).Returns("http://localhost:5019");

        var refreshAttempts = 0;
        var refreshHandler = new MockHttpMessageHandler
        {
            Handler = _ =>
            {
                refreshAttempts++;
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(TokenRefreshingHandler.RefreshClientName))
            .Returns(new HttpClient(refreshHandler));

        var attempts = 0;
        var backendHandler = new MockHttpMessageHandler
        {
            Handler = _ =>
            {
                attempts++;
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }
        };

        var handler = new TokenRefreshingHandler(
            accessorMock.Object,
            configMock.Object,
            factoryMock.Object,
            NullLogger<TokenRefreshingHandler>.Instance)
        {
            InnerHandler = backendHandler
        };

        var invoker = new HttpMessageInvoker(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5019/api/tickets");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "bad-token");

        var response = await invoker.SendAsync(request, CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        attempts.Should().Be(1);
        refreshAttempts.Should().Be(1);
        httpContext.Session.GetString("AccessToken").Should().BeNull();
    }
}
