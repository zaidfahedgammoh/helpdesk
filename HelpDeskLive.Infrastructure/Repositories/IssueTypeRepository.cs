
using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Application.Interfaces;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.Infrastructure.Repositories;

public class IssueTypeRepository : IIssueTypeRepository
{
    private readonly HelpDeskDbContext _context;

    public IssueTypeRepository(HelpDeskDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<IssueType>> GetAllActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.IssueTypes
            .AsNoTracking()
            .Where(i => i.IsActive)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IssueType?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.IssueTypes
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task AddAsync(
        IssueType issueType,
        CancellationToken cancellationToken = default)
    {
        await _context.IssueTypes.AddAsync(issueType, cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
