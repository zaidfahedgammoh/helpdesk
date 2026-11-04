using HelpDeskLive.Application.Abstractions;
using HelpDeskLive.Infrastructure.Data;
using HelpDeskLive.Infrastructure.Repositories;
using HelpDeskLive.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HelpDeskLive.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IIssueTypeRepository, IssueTypeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IAdminReportingService, AdminReportingService>();
        services.AddHttpClient<ITicketNotificationService, TicketNotificationService>();

        if (configuration.GetValue<bool>("Database:UseInMemoryDatabase"))
        {
            var inMemoryDbName = configuration["Database:InMemoryDbName"] ?? "HelpDeskTestDb";
            services.AddDbContext<HelpDeskDbContext>(options =>
                options.UseInMemoryDatabase(inMemoryDbName));
        }
        else
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' was not found.");
            }

            services.AddDbContext<HelpDeskDbContext>(options =>
                options.UseSqlServer(connectionString));
        }

        return services;
    }
}
