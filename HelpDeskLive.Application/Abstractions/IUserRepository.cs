
using HelpDeskLive.Domain.Entities;

namespace HelpDeskLive.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken);
}
