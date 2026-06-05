using Finbridge.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Finbridge.Application.Events;

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
                "Нет обработчиков для доменного события {EventType}.",
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
