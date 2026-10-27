
namespace HelpDeskLive.Application.Abstractions;

public interface ICurrentUser
{
    int? UserId { get; }

    string? Role { get; }
}
