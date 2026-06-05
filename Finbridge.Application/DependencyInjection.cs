using Finbridge.Application.Events;
using Finbridge.Application.Services;
using Finbridge.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Finbridge.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует Application-слой: сервисы и диспетчер доменных событий.
    /// Доменные политики (<see cref="BalanceSettings"/>) биндятся вызывающим
    /// кодом через <c>services.Configure&lt;BalanceSettings&gt;(...)</c>.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBalanceService, BalanceService>();
        return services;
    }
}
