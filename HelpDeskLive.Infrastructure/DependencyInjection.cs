
using Microsoft.Extensions.DependencyInjection;

namespace HelpDeskLive.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        // SQL Server and DbContext will be registered in the next step.

        return services;
    }
}
