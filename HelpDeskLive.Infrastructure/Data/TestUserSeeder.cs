
using HelpDeskLive.Domain.Entities;
using HelpDeskLive.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.Infrastructure.Data.Seeding;

public static class TestUserSeeder
{
    public static async Task SeedAsync(
        HelpDeskDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var passwordHasher = new PasswordHasher<User>();
        var defaultDepartmentId = await dbContext.Departments.Select(d => (int?)d.Id).FirstOrDefaultAsync(cancellationToken) ?? 1;

        var users = new[]
        {
            CreateUser(
                passwordHasher,
                "Test Admin",
                "admin@helpdesklive.local",
                UserRole.Admin),

            CreateUser(
                passwordHasher,
                "Test Agent",
                "agent@helpdesklive.local",
                UserRole.Agent,
                defaultDepartmentId),

            CreateUser(
                passwordHasher,
                "Test Customer",
                "customer@helpdesklive.local",
                UserRole.Customer)
        };

        await dbContext.Users.AddRangeAsync(users, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static User CreateUser(
        PasswordHasher<User> passwordHasher,
        string name,
        string email,
        UserRole role,
        int? departmentId = null)
    {
        const string testPassword = "HelpDeskTest!2026";

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
