
using HelpDeskLive.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskLive.Infrastructure.Data;

public class HelpDeskDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(HelpDeskDbContext).Assembly);
    }

    public HelpDeskDbContext(
        DbContextOptions<HelpDeskDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Ticket> Tickets => Set<Ticket>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
}
