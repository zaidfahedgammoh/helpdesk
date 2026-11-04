using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.Infrastructure.Repositories;

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly HelpDeskDbContext _dbContext;

    public DepartmentRepository(HelpDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(ct);
    }

    public Task<Department?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return _dbContext.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
    {
        return _dbContext.Departments.AnyAsync(d => d.Id == id, ct);
    }
}
