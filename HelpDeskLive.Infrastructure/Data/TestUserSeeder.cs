
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HelpDeskLive.Infrastructure.Data.Seeding;

public static class TestUserSeeder
{
    public static async Task SeedAsync(
        HelpDeskDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var defaultDepartmentId = await dbContext.Departments.Select(d => (int?)d.Id).FirstOrDefaultAsync(cancellationToken);
        if (defaultDepartmentId is null)
            throw new InvalidOperationException("Reference departments must be seeded before users.");

        var existingUsers = await dbContext.Users.ToListAsync(cancellationToken);
        foreach (var agent in existingUsers.Where(u => u.Role == UserRole.Agent && u.DepartmentId is null))
            agent.ChangeDepartment(defaultDepartmentId.Value);
        if (existingUsers.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var testPassword = configuration["SeedTestUsers:Password"];
        if (string.IsNullOrWhiteSpace(testPassword) || testPassword.Length < 12)
            throw new InvalidOperationException("Set SeedTestUsers__Password (at least 12 characters) before enabling test-user seeding.");

        var passwordHasher = new PasswordHasher<User>();

        var users = new[]
        {
            CreateUser(
                passwordHasher,
                "Test Admin",
                "admin@helpdesklive.local",
                UserRole.Admin,
                null,
                testPassword),

            CreateUser(
                passwordHasher,
                "Test Agent",
                "agent@helpdesklive.local",
                UserRole.Agent,
                defaultDepartmentId,
                testPassword),

            CreateUser(
                passwordHasher,
                "Test Customer",
                "customer@helpdesklive.local",
                UserRole.Customer,
                null,
                testPassword)
        };

        await dbContext.Users.AddRangeAsync(users, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static User CreateUser(
        PasswordHasher<User> passwordHasher,
        string name,
        string email,
        UserRole role,
        int? departmentId,
        string testPassword)
    {
        var user = new User(
            name,
            email,
            "temporary-hash",
            role,
            PreferredLanguage.English,
            departmentId);

        var passwordHash = passwordHasher.HashPassword(
            user,
            testPassword);

        return new User(
            name,
            email,
            passwordHash,
            role,
            PreferredLanguage.English,
            departmentId);
    }
}
