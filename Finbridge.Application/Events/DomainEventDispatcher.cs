using Finbridge.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Finbridge.Application.Events;

/// <summary>
/// Application-уровень диспетчера. Маршрутизирует доменные события
/// к зарегистрированным in-process обработчикам. Конкретные обработчики
/// (Kafka, логи, метрики) поставляются внешними слоями.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = _serviceProvider.GetServices(handlerType);

        if (!handlers.Any())
        {
            _logger.LogDebug(
                "No handlers registered for domain event {EventType}.",
                domainEvent.GetType().Name);
            return;
        }

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod("HandleAsync")!;
            var invocation = method.Invoke(handler, new object[] { domainEvent, cancellationToken });
            if (invocation is Task task)
            {
                await task;
            }
        }
    }
}
