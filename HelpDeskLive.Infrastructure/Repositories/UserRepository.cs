
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly HelpDeskDbContext _dbContext;

    public UserRepository(HelpDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return _dbContext.Users.SingleOrDefaultAsync(
            user => user.Email == normalizedEmail,
            cancellationToken);
    }

    public async Task<int> CountByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Users
            .CountAsync(
                user => user.Role == role,
                cancellationToken);
    }
}
