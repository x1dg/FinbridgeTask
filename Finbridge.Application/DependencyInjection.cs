using Finbridge.Application.Events;
using Finbridge.Application.Services;
using Finbridge.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Finbridge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBalanceService, BalanceService>();
        return services;
    }
}
