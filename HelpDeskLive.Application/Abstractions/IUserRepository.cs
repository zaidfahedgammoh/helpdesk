
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;

namespace HelpDeskLive.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken);

    Task<int> CountByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken);
}
