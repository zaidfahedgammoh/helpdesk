using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;

namespace HelpDeskLive.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<int> CountByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
