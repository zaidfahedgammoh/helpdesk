using HelpDeskLive.Domain.Entities;

namespace HelpDeskLive.Application.Abstractions;

public interface IIssueTypeRepository
{
    Task<IReadOnlyList<IssueType>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IssueType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(IssueType issueType, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}