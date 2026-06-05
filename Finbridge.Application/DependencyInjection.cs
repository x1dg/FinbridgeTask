using Finbridge.Application.Services;
using Finbridge.Domain.Users.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Finbridge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBalanceService, BalanceService>();
        return services;
    }
}
