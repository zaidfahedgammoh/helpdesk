using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.Infrastructure.Data.Seeding;

public static class ReferenceDataSeeder
{
    public static async Task SeedAsync(
        HelpDeskDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var departmentNames = new[] { "IT Support", "Accounts", "Facilities" };

        foreach (var name in departmentNames)
        {
            var exists = await dbContext.Departments
                .AnyAsync(department => department.Name == name, cancellationToken);

            if (!exists)
                await dbContext.Departments.AddAsync(new Department(name), cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var issueTypes = new[]
        {
            new { Name = "Password or Account Access", Description = "Problems signing in, resetting passwords, or accessing an account.", DepartmentName = "IT Support", Priority = TicketPriority.High },
            new { Name = "Software Problem", Description = "An application is not working, installing, or updating correctly.", DepartmentName = "IT Support", Priority = TicketPriority.Medium },
            new { Name = "Computer or Hardware Problem", Description = "Problems with a computer, monitor, keyboard, printer, or other equipment.", DepartmentName = "IT Support", Priority = TicketPriority.Medium },
            new { Name = "Network or Wi-Fi Problem", Description = "Problems connecting to the internet, office network, or Wi-Fi.", DepartmentName = "IT Support", Priority = TicketPriority.High },
            new { Name = "Accounts and Billing Question", Description = "Questions about account charges, invoices, or billing records.", DepartmentName = "Accounts", Priority = TicketPriority.Medium },
            new { Name = "Facilities or Office Problem", Description = "Problems with office space, furniture, lighting, or building facilities.", DepartmentName = "Facilities", Priority = TicketPriority.Low }
        };

        foreach (var item in issueTypes)
        {
            var exists = await dbContext.IssueTypes
                .AnyAsync(issueType => issueType.Name == item.Name, cancellationToken);

            if (exists)
                continue;

            var department = await dbContext.Departments
                .FirstAsync(department => department.Name == item.DepartmentName, cancellationToken);

            await dbContext.IssueTypes.AddAsync(
                new IssueType(item.Name, item.Description, department.Id, item.Priority),
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
