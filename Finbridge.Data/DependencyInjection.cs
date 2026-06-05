using Finbridge.Data.Repositories;
using Finbridge.Domain.Users.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Finbridge.Data;

public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует инфраструктурный слой: EF Core DbContext и репозитории.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<FinbridgeDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }
}
