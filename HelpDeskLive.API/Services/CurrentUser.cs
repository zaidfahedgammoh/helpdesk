
using System.Security.Claims;
using HelpDeskLive.Application.Abstractions;

namespace HelpDeskLive.API.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?
                .User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userId, out var parsedUserId))
            {
                return parsedUserId;
            }

            return null;
        }
    }
}
