using HelpDeskLive.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.API.Controllers;

[ApiController]
[Route("api/admin/departments")]
[Authorize(Roles = "Admin")]
public sealed class AdminDepartmentsController : ControllerBase
{
    private readonly HelpDeskDbContext _dbContext;

    public AdminDepartmentsController(HelpDeskDbContext dbContext)
        => _dbContext = dbContext;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var departments = await _dbContext.Departments
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .Select(department => new DepartmentResponse(
                department.Id,
                department.Name))
            .ToListAsync(cancellationToken);

        return Ok(departments);
    }

    public sealed record DepartmentResponse(int Id, string Name);
}
